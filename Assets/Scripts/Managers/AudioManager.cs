using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// The AudioManager class is responsible for managing audio playback and recording within the application.
/// To use this class, create an empty GameObject in the Unity Editor, add an AudioSource component to it,
/// and then assign this script to the GameObject.
/// </summary>
public class AudioManager : MonoBehaviour
{
    static AudioManager audioManager;
    AudioSource audioSource;
    private AudioClip replayClip;

    void Awake()
    {
        if (audioManager != null)
        {
            Debug.LogError("Multiple AudioManagers");
            return;
        }
        audioSource = GetComponent<AudioSource>();
        audioManager = this;
    }

    public static AudioManager GetManager()
    {
        return audioManager;
    }

    public void StartRecording(int lengthSec) 
    {
        audioSource.clip = Microphone.Start(Microphone.devices[0], false, lengthSec, Const.FREQUENCY);
    }

    public void PlayAudioClip(AudioClip audioClip)
    {
        audioSource.clip = audioClip;
        audioSource.Play();
    }

    public void GetAudioAndPost(string transcript, GameObject textErrorGO, GameObject resultTextGO, GameObject resultPanelGO, GameObject debugTextGO)
    {
        Microphone.End("");        
        byte[] wavBuffer = SavWav.GetWav(audioSource.clip, out uint length, trim:true);
        SavWav.Save(Const.REPLAY_FILENAME, audioSource.clip, trim:true); // for debug purpose

        StartCoroutine(NetworkManager.GetManager().ServerPost(transcript, wavBuffer, textErrorGO, resultTextGO, resultPanelGO, debugTextGO));
    }

    public IEnumerator LoadAudioClip(string filename, GameObject replayButtonGO)
    {
        if(!string.IsNullOrEmpty(filename)) {
            string path = System.IO.Path.Combine(Application.persistentDataPath, filename.EndsWith(".wav") ? filename : filename + ".wav");
            
            // Need the file:// for GetAudioClip            
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.WAV))            
            {
                ((DownloadHandlerAudioClip)uwr.downloadHandler).streamAudio = true;
        
                yield return uwr.SendWebRequest();
        
                if (uwr.result==UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
                {   
                    Debug.LogError("Failed to reload replay audio clip");
                    Debug.LogError(uwr.result);
                    Debug.LogError(path);
                    replayClip = null;
                    if (replayButtonGO != null)
                    {
                        replayButtonGO.SetActive(false);
                    }
                    yield break;
                }
        
                DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip)uwr.downloadHandler;
        
                if (dlHandler.isDone)
                {
                    Debug.Log("Replay audio clip is loaded");
                    replayClip = dlHandler.audioClip;
                    if (replayButtonGO != null)
                    {
                        replayButtonGO.transform.GetComponent<Button>().onClick.AddListener(()=> AudioManager.GetManager().PlayAudioClip(replayClip));            
                        replayButtonGO.SetActive(true);
                    }   
                }
            }
            
            yield break;
        }
    }
}