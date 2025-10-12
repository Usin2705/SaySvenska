from dataclasses import dataclass, field
import torch
import re
import torch
import Levenshtein


# ──────────────────────────────────────────────────────────────
#  Probability calibration helpers
# ──────────────────────────────────────────────────────────────
def temperature_scaling(logits: torch.Tensor, temperature: float) -> torch.Tensor:
    """
    Divide logits by a temperature (>0). Higher T  ➜  softer distribution.
    """
    if temperature <= 0:
        raise ValueError("temperature must be > 0")
    return logits / temperature

def topk_normalize(probabilities, topk = 3):
    """
    Normalize the top-k probabilities in a tensor and leave the rest untouched.

    :param probabilities: The input tensor.
    :param topk: The number of top elements to consider.
    :return: A tensor of the same size as the input, but with only the top-k elements 
    normalized to sum to 1, and the rest left as they were.
    """
    # Compute the top-k values and their indices
    # Only use 1 instead of selecting topk
    top_values, _ = torch.topk(probabilities, 1)    
    #sum_prob = torch.sum(top_values, dim=-1, keepdims=True)
    
    top_values_norm, top_indices_norm = torch.topk(probabilities, topk)
    bottom_values, bottom_indice = torch.topk(probabilities, probabilities.shape[-1] - topk, largest=False)

    #top_prob_normalized = top_values_norm / sum_prob 
    top_prob_normalized = top_values_norm / top_values 

    # Generate the new probability tensor where only topk_norm items are normalized
    new_probabilities = torch.zeros_like(probabilities)

    # Scatter the normalized top-k values into the result tensor
    new_probabilities.scatter_(-1, top_indices_norm, top_prob_normalized)

    # Scatter the untouched bottom probabilities
    new_probabilities.scatter_(-1, bottom_indice, bottom_values)    
    
    return new_probabilities


# ──────────────────────────────────────────────────────────────
#  Letter-score post-processing
# ──────────────────────────────────────────────────────────────
def word_level_min_scores(segments, transcript: str):
    """
    Give every character the SAME score: the lowest segment.score found
    anywhere inside its word.

    segments   : list from merge_repeats(); len == len(transcript)
    transcript : text used for alignment (contains '|' as delimiter)

    returns list[float] aligned 1-to-1 with `segments`
    """
    if len(segments) != len(transcript):
        raise ValueError("segments and transcript must be the same length")

    out_scores   = []
    word_scores  = []          # temp buffer for current word’s segment scores

    for seg, ch in zip(segments, transcript):
        if ch == '|':          # word boundary  →  flush buffer
            if word_scores:                       # assign word-level min
                w_min = min(word_scores)
                out_scores.extend([w_min] * len(word_scores))
                word_scores.clear()
            out_scores.append(seg.score)          # keep delimiter’s own score
        else:
            word_scores.append(seg.score)

    # flush last word (no trailing '|')
    if word_scores:
        w_min = min(word_scores)
        out_scores.extend([w_min] * len(word_scores))

    return out_scores

# ──────────────────────────────────────────────────────────────
#  Tiny wrapper so main file can stay clean
# ──────────────────────────────────────────────────────────────
def edit_ops(ref: str, hyp: str):
    """
    Returns Levenshtein.editops in a JSON-friendly structure.
    """
    ops = Levenshtein.editops(ref, hyp)
    return [{"ops": op[0], "tran_index": op[1], "pred_index": op[2]} for op in ops]



def textSanitize(transcript, addPad = True):
    # This is done in Unity instead    
    if addPad:
        transcript = transcript.replace(' ', '|')        

    return transcript

def get_trellis(emission, tokens, blank_id=0):
    """
    Adding 'audio' in form of float numpy array to batch dataset. 
    Used together with map_to_w2v2_prediction for batch train/evaluation
    
    Args:
        batch:
            batch file containing at least 'path' which store the dicrectory of audio file
        sampling_rate:
            Target sampling rate for librosa to resampling
        path_column:
            Store the dicrectory of audio file
    """
    
    num_frame = emission.size(0)
    num_tokens = len(tokens)   

    # Trellis has extra dimensions for both time axis and tokens.
    # The extra dim for tokens represents <SoS> (start-of-sentence)
    # The extra dim for time axis is for simplification of the code.
    trellis = torch.full((num_frame + 1, num_tokens + 1), -float("inf"))
    trellis[:, 0] = 0
    
    for t in range(num_frame):
        trellis[t + 1, 1:] = torch.maximum(
            # Score for staying at the same token
            # Score = P(timeframe T) * P(timeframe T | PADDING)
            trellis[t, 1:] + emission[t, blank_id],
            # Score for changing to the next token
            # Score = P(timeframe T) * P(timeframe T | TOKENS)
            # We do not include the last token since it is the result of changing the token
            # the last token P is equal to P(token-1)*P(change token)
            trellis[t, :-1] + emission[t, tokens],
        )
    return trellis 

@dataclass
class Point:
    token_index: int
    time_index: int
    all_score: float
    token_score: float

def backtrack(trellis, emission, tokens, extra_frame=2, blank_id=0, space_id=0):
    # Note:
    # j and t are indices for trellis, which has extra dimensions
    # for time and tokens at the beginning.
    # When referring to time frame index `T` in trellis,
    # the corresponding index in emission is `T-1`.
    # Similarly, when referring to token index `J` in trellis,
    # the corresponding index in transcript is `J-1`.
    j = trellis.size(1) - 1
    t_start = torch.argmax(trellis[:, j]).item()
    
    # Extend the path to 2 more extra_frame at the end
    t_start += extra_frame
    
    # If the last phone is also at the end of the sound frame, there's no extra padding at the end
    #to avoid index out of bounds
    if t_start>=trellis.size(0):
        t_start = trellis.size(0) - 1
    
    count_down = extra_frame
    start_cd = False

    path = []
    for t in range(t_start, 0, -1):
        # 1. Figure out if the current position was stay or change
        # Note (again):
        # `emission[J-1]` is the emission at time frame `J` of trellis dimension.
        # Score for token staying the same from time frame J-1 to T (which mean they are padding).
        padding_score = trellis[t - 1, j] + emission[t - 1, blank_id]        
        # Score for token changing from C-1 at T-1 to J at T.
        # If model failed to put token into 1 framelength, then token_score could 
        # have several frame length
        token_score = trellis[t - 1, j - 1] + emission[t - 1, tokens[j - 1]]
        #print(f'index {t-1}, token {j} padding_score {padding_score}, token_score {token_score}, prob {emission[t - 1, tokens[j - 1]].exp()}')
        
        # 2. Store the path with frame-wise probability.
        all_prob = (emission[t - 1, tokens[j - 1] if token_score > padding_score else blank_id]).exp().item()
        token_prob = emission[t - 1, tokens[j - 1]].exp().item()
        
        # Return token index and time index in non-trellis coordinate.
        path.append(Point(j - 1, t - 1, all_prob, token_prob))

        # 3. Update the token
        if token_score > padding_score:
            
            # Extend the path to include two more frames at the beginning
            if (~start_cd) and (j == 1): 
                start_cd = True
                
            else:
                j -= 1
                
        # No longer meed this check since when j==1 and j-=1 mean it will break as soon as it reach j = 0
        # And since we extend two more frames --> it will break when cd end at 0
        #    if j == 0:
        #        break
        
        # This is to make sure j reach 0 
        # Because we insert count down
        # With this, we no longer need j == 0 check (since start cd --> j will reach 0 in 2 additional steps)
        if start_cd:
            count_down -=1
        if count_down == 0:
            break
    else:
        print("Failed to align")
        return path[::-1]
        #raise ValueError("Failed to align")
    return path[::-1]

@dataclass
class Segment:
    label: str
    start: int
    end: int
    score: float

    def __repr__(self):
        return f"{self.label}\t({self.score:4.2f}): [{self.start:5d}, {self.end:5d})"

    @property
    def length(self):
        return self.end - self.start

def merge_repeats(transcript, path, ignore_pad=True):
    # Merge the score, calculate scoring based on path    
    i1, i2 = 0, 0
    segments = []
    while i1 < len(path):
        while i2 < len(path) and path[i1].token_index == path[i2].token_index:
            i2 += 1
        
        if ignore_pad:
            # Since padding score is ignore, token score is calculated as maximum value of token:            
            score = max(path[k].token_score for k in range(i1, i2)) 
        else:            
            # If padding is included, score is calculated as average highest score 
            # of both padding and token accross the path
            score = sum(path[k].all_score for k in range(i1, i2)) / (i2 - i1)
           
        segments.append(Segment(transcript[path[i1].token_index],
                                path[i1].time_index, 
                                path[i2-1].time_index + 1, 
                                score))
        i1 = i2
    return segments     