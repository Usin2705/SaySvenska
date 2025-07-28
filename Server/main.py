import mimetypes
import os
import tempfile
import logging

from flask import Flask, jsonify, request
import torch
import torchaudio
from transformers import Wav2Vec2Processor, Wav2Vec2ForCTC

import utils.myutils as myutils

print("App start!")

app = Flask(__name__)

PATH_MODEL = "./models/nhan_wav2vec2-xls-r-300m-svensk_ent-020_2to25"
SAMPLING_RATE = 16000

processor = Wav2Vec2Processor.from_pretrained(PATH_MODEL)
model = Wav2Vec2ForCTC.from_pretrained(PATH_MODEL)

@app.route("/api/app/test_post", methods=['POST'])
def test_post():
    print("test connected")
    return {"prediction":"success", "score":[0.5, 0.9, 0.2, 0.6, 0.8, 0.7, 0.1]}

@app.route("/test")
def test_func():
    
    return "Success2"    


@app.route("/wav2vec2/models/score", methods=['POST'])
def pronunc_eval_unity():
    try:
        # ------------------------------------------------------------------
        # 1. ── housekeeping
        # ------------------------------------------------------------------
        wav_file = request.files['file']    
        transcript_raw = request.form["transcript"].lower()
        transcript     = myutils.textSanitize(transcript_raw) 

        temperature = float(request.form.get("temperature", 10))
        topk        = int(request.form.get("topk", 3))

        # ------------------------------------------------------------------
        # 2. ── audio-to-logits
        # ------------------------------------------------------------------        
            # waveform, sr = torchaudio.load("/tmp/" + FILE_NAME + ".wav")
            # Loading directly require FFmpeg libraries (>=4.1, <4.4)            
            # We can also save it temporarity
            # Save the file temporarily and load it with torchaudio
            # with tempfile.NamedTemporaryFile(delete=False, suffix=".wav") as tmp_file:
            #     file.save(tmp_file.name)
            #     tmp_path = tmp_file.name

            # Then delete it after use
            # os.remove(tmp_path)
            # waveform, sr = torchaudio.load(tmp_path)    

        with torch.inference_mode():
            waveform, sr = torchaudio.load(wav_file)
            waveform     = torchaudio.transforms.Resample(sr, SAMPLING_RATE)(waveform)  
                    
        # Because the server is low capacity, it can't handle long audio request
        # Server must refuse to load the model if audio length is longer than 8 seconds
        # audio length is waveform length / SAMPLING_RATE 
        # The default waveform shape is [channel, number of frames]
        # So we can measure audio_length. It should be note that the waveform shape
        # is depend on the configuration.
        audio_length = waveform.shape[1] / SAMPLING_RATE

        if audio_length > 8.2:
            return jsonify({"error": "Audio is too long. Max length is 8 s."}), 400

        # Since the recordings are often stereo with 2 channels,
        # while wav2vec2 input expect 1 channel. We can extract only 1 channel for prediction,
        # or use transpose to match dimension with Wav2Vec2 processor
        # input_audio = torch.transpose(waveform, 1, 0)

        # indexing to get only 1 channel (incase the recording are from 2 channels)
        # input_audio = input_audio[:, 0]                       
        
        # We can also you mean to use mean to get the mean signal as input audio for the model
        input_audio  = waveform.mean(dim=0)   # mono
        input_values = processor(
            input_audio,
            sampling_rate=SAMPLING_RATE,
            return_tensors="pt",
        ).input_values

        with torch.no_grad():
            logits = model(input_values).logits                     # [1, T, vocab]

        # ------------------------------------------------------------------
        # 3. ── temperature scaling  ➜  top-k normalisation
        # ------------------------------------------------------------------
        logits_scaled   = myutils.temperature_scaling(logits, temperature)
        probs_scaled    = torch.softmax(logits_scaled, dim=-1)     # still [1, T, vocab]
        probs_topk_norm = myutils.topk_normalize(probs_scaled, topk=topk)
        emission        = torch.log(probs_topk_norm[0])            # [T, vocab]  ← used below


        # ------------------------------------------------------------------
        # 4. ── forced-alignment & scoring
        # ------------------------------------------------------------------
        vocab = processor.tokenizer.get_vocab()
        tokens   = [vocab[c] for c in transcript]

        trellis  = myutils.get_trellis(emission, tokens, blank_id=model.config.pad_token_id)
        path     = myutils.backtrack(trellis, emission, tokens, blank_id=model.config.pad_token_id)
        segments = myutils.merge_repeats(transcript, path)       

        
        # ------------------------------------------------------------------
        # 5. ── word-level calibration of letter scores
        #        (each letter takes the lowest score of that letter in
        #         its own word)
        # ------------------------------------------------------------------
        fa_score = myutils.word_level_min_scores(segments, transcript)


        # ------------------------------------------------------------------
        # 6. ── decode + Levenshtein
        # ------------------------------------------------------------------
        pred_ids  = torch.argmax(logits, dim=-1)
        prediction = processor.batch_decode(pred_ids)[0]

        normal_transcript = transcript.replace("|", " ")
        ops = myutils.edit_ops(normal_transcript, prediction)   # thin wrapper around Levenshtein

        # ------------------------------------------------------------------
        # 7. ── response
        # ------------------------------------------------------------------
        return jsonify({
            "levenshtein": ops,
            "prediction" : prediction,
            "score"      : fa_score,
        }), 200
    
    # ----------------------------------------------------------------------
    except ValueError as e:
        return jsonify({"error": str(e)}), 400
    except Exception as e:
        logging.exception(e)
        return jsonify({"error": f"Unexpected error: {e}"}), 500

if __name__ == '__main__':
    server_port = os.environ.get('PORT', '8080')
    app.run(debug=False, port=server_port, host='0.0.0.0')


#flask run --host=0.0.0.0 --port=52705
#gunicorn --bind :52705 main:app --workers 2 --threads 2

#Run in background
#gunicorn --bind :52705 --chdir /l/pop2talk-server/ main:app --workers 2 --threads 3 --daemon --access-logfile /l/pop2talk-server/logs/gunicorn-access.log --error-logfile /l/pop2talk-server/logs/gunicorn-error.log --capture-output --log-level debug --access-logformat "%(h)s %(l)s %(u)s %(t)s \"%(r)s\" %(s)s %(b)s \"%(f)s\" \"%(a)s\" %(L)s"
#gunicorn --bind :52705 --chdir /l/pop2talk-server/ main:app --workers 2 --threads 3 --access-logfile /l/pop2talk-server/logs/gunicorn-access.log --error-logfile /l/pop2talk-server/logs/gunicorn-error.log --capture-output --log-level debug --access-logformat "%(h)s %(l)s %(u)s %(t)s \"%(r)s\" %(s)s %(b)s \"%(f)s\" \"%(a)s\" %(L)s"


#gunicorn --bind :52705 main:app --workers 2 --threads 3 --daemon --access-logfile /l/pop2talk-server/logs/gunicorn-access.log --error-logfile /l/pop2talk-server/logs/gunicorn-error.log --capture-output --log-level debug --access-logformat "%(h)s %(l)s %(u)s %(t)s \"%(r)s\" %(s)s %(b)s \"%(f)s\" \"%(a)s\" %(L)s"
#gunicorn --bind :52705 main:app --workers 2 --threads 3 --access-logfile /l/pop2talk-server/logs/gunicorn-access.log --error-logfile /l/pop2talk-server/logs/gunicorn-error.log --capture-output --log-level debug --access-logformat "%(h)s %(l)s %(u)s %(t)s \"%(r)s\" %(s)s %(b)s \"%(f)s\" \"%(a)s\" %(L)s"

#netstat -aWn --programs | grep 52705
#ldconfig /usr/local/lib64/
#pkill -f gunicorn
#sudo kill -9 `sudo lsof -t -i:52705`