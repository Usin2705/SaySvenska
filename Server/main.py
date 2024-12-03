import mimetypes
from transformers import Wav2Vec2Processor, Wav2Vec2ForCTC, AutoFeatureExtractor
from flask import Flask, jsonify, request, Response
import torch
import torchaudio
import os
import logging
import Levenshtein
import tempfile

import utils.myutils as myutils

print("App start!")

app = Flask(__name__)

path_model = "./models/nhan_wav2vec2-xls-r-300m-svensk-ent-010_3_to_25"
SAMPLING_RATE = 16000

processor = Wav2Vec2Processor.from_pretrained(path_model)
model = Wav2Vec2ForCTC.from_pretrained(path_model)

@app.route("/api/app/test_post", methods=['POST'])
def test_post():
    print("test connected")
    return {"prediction":"success", "score":[0.5, 0.9, 0.2, 0.6, 0.8, 0.7, 0.1]}

@app.route("/test")
def test_func():
    
    return "Success"    


@app.route("/wav2vec2/models/score", methods=['POST'])
def pronunc_eval_unity():
    try:
        file = request.files['file']    
        # file.save(os.path.join("/tmp/", FILE_NAME + ".wav"))
        # print(f'file info: {file}')

        transcript = request.form['transcript'].lower()
        
        transcript = myutils.textSanitize(transcript)  

        vocab = processor.tokenizer.get_vocab()    

        with torch.inference_mode():
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

            waveform, sr = torchaudio.load(file)
            
            waveform = torchaudio.transforms.Resample(sr, SAMPLING_RATE)(waveform)  
                    
        # Because the server is low capacity, it can't handle long audio request
        # Server must refuse to load the model if audio length is longer than 8 seconds
        # audio length is waveform length / SAMPLING_RATE 
        # The default waveform shape is [channel, number of frames]
        # So we can measure audio_length. It should be note that the waveform shape
        # is depend on the configuration.
        audio_length = waveform.shape[1] / SAMPLING_RATE

        if audio_length > 8.2:
            return jsonify({"error": "Audio is too long. Please provide an audio file that is less than 8 seconds."}), 400

        # Since the recordings are often stereo with 2 channels,
        # while wav2vec2 input expect 1 channel. We can extract only 1 channel for prediction,
        # or use transpose to match dimension with Wav2Vec2 processor
        # input_audio = torch.transpose(waveform, 1, 0)

        # indexing to get only 1 channel (incase the recording are from 2 channels)
        # input_audio = input_audio[:, 0]                       
        
        # We can also you mean to use mean to get the mean signal as input audio for the model
        input_audio = waveform.mean(dim=0)

        input_values = processor(input_audio, sampling_rate=SAMPLING_RATE, return_tensors="pt").input_values

        with torch.no_grad():
            logits = model(input_values).logits

        predicted_ids = torch.argmax(logits, dim=-1)
        prediction = processor.batch_decode(predicted_ids)[0]
        tokens = [vocab[c] for c in transcript]     

        log_softmax = torch.log_softmax(logits, dim=-1)
        emission = log_softmax[0].cpu().detach()   

        trellis = myutils.get_trellis(emission, tokens, blank_id=model.config.pad_token_id)     
        path = myutils.backtrack(trellis, emission, tokens, blank_id=model.config.pad_token_id)    
        segments = myutils.merge_repeats(transcript, path)       

        fa_score = []

        for seg in segments:
            fa_score.append(seg.score)

        #logging.warning(f"Prediction {prediction}, Score {fa_score}")

        normal_trans = transcript.replace('|', ' ')
        ops = Levenshtein.editops(normal_trans, prediction)

        print(f'Using model: {path_model}')
        print(f'Transcript: {normal_trans}')
        print(f'Prediction: {prediction}')
        print(f'Score: {fa_score}')        
        print(f'OPS: {ops}')

        # Convert back to easier list for Unity
        ops_list = []
        for item in ops:
            ops_list.append({"ops":item[0], "tran_index":item[1], "pred_index":item[2]})

        # Success response with status code 200
        return jsonify({"levenshtein": ops_list, "prediction": prediction, "score": fa_score}), 200  
    
    except ValueError as e:
        # Invalid client request
        return jsonify({"error": str(e)}), 400
    
    except Exception as e:
        # Catch all other unexpected exceptions and return a 500 response
        return jsonify(error=f"An unexpected error occurred: {str(e)}"), 500

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