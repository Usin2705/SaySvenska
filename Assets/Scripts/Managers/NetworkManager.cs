using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// The NetworkManager class is responsible for managing network operations related to ASR (Automatic Speech Recognition).
/// It handles sending audio data and transcripts to the ASR server and processes the server's response.
/// This class ensures that only one instance of NetworkManager exists at any time (singleton pattern).
/// </summary>
public class NetworkManager : MonoBehaviour
{
    
	// This variable is not used in the current version
	// [SerializeField] GameObject surveyPopUpPanelGO;    
    
    static NetworkManager netWorkManager;
	// This is the URL to the ASR server
	// AUDIO_URL should be in http and not https
	// Because it would make the connection faster???
	// You can set the URL in Secret.cs

	// public static class Secret
	// {
	// 	public const string AUDIO_URL = "http://YOUR SERVER ADDRESS HERE"; //fill in this one
	// }
	string asrURL = Secret.AUDIO_URL; 

    // However, other URL should be in https for encryption purpose
    public ASRResult asrResult {get; private set;}

    void Awake() {
		if (netWorkManager != null) {
			Debug.LogError("Multiple NetWorkManagers");
			Destroy(gameObject);
			return;
		}
		netWorkManager = this;
	}

	public static NetworkManager GetManager() {
		return netWorkManager;
	}

    public IEnumerator ServerPost(string transcript, byte[] wavBuffer, GameObject textErrorGO, GameObject resultTextGO,
								GameObject resultPanelGO, GameObject debugTextGO, System.Action OnServerDone = null)
    
	{
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavBuffer, fileName:Const.FILE_NAME_POST, mimeType: "audio/wav");
        form.AddField("transcript", transcript);
		form.AddField("model_code", "1");

		// Use a `using` statement for UnityWebRequest to handle resource cleanup
		// This is a good practice to avoid memory leaks
		using (UnityWebRequest uwr = UnityWebRequest.Post(asrURL, form))
		{
			uwr.timeout = Const.TIME_OUT_SECS;
			yield return uwr.SendWebRequest();

			Debug.Log(uwr.result);

			if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError) {
				Debug.Log(uwr.error);
				
				textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = string.IsNullOrEmpty(uwr.error) ? "Network error!" : "Server error!";		
				
				OnServerDone?.Invoke();
				throw new System.Exception(uwr.downloadHandler.text ?? uwr.error);

			} else {
				Debug.Log("Form upload complete!");

				Debug.Log(uwr.downloadHandler.text);

				if (uwr.downloadHandler.text == "invalid credentials") {
					Debug.Log("invalid credentials");				
					textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = "invalid credentials";
					
					OnServerDone?.Invoke();
					yield break;
				}

				if (uwr.downloadHandler.text == "this account uses auth0") {
					Debug.Log("this account uses auth0");
					textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = "this account uses auth0";
					
					OnServerDone?.Invoke();
					yield break;
				}
			}
			
			textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = "Here are your results:";
			asrResult = JsonUtility.FromJson<ASRResult>(uwr.downloadHandler.text);


			// Update text result
			// This part only update the TextResult text		
			// is updated (added onclick, show active) in their MainPanel (either MainPanel or ExercisePanel)

			// After TextResult text is updated,
			// it's safe to set onclick on result text on it's main panel
			// that's why we can set the Panel to active		
			string textResult = TextUtils.FormatTextResult(transcript, asrResult.score);		
			resultTextGO.GetComponent<TMPro.TextMeshProUGUI>().text = textResult;
			
			// Set resultTextGO to bold following design guideline
			resultTextGO.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;			
			
			// Update the debug text
			debugTextGO.SetActive(true);
			debugTextGO.GetComponent<TMPro.TextMeshProUGUI>().text = asrResult.prediction;		
			
			// This function is not active in the current version
			if (resultPanelGO != null) resultPanelGO.SetActive(true);

			//checkSurVey();			
		}
		OnServerDone?.Invoke();
    }	

	// public void checkSurVey() {
	// 	int recordNumber = 1;
		
	// 	// If this is not the first record, get the record number
	// 	if (PlayerPrefs.HasKey(Const.PREF_RECORD_NUMBER)) {
	// 		recordNumber = PlayerPrefs.GetInt(Const.PREF_RECORD_NUMBER) + 1;
	// 	}
	// 	//Debug.Log("Record number: " + recordNumber);
	// 	PlayerPrefs.SetInt(Const.PREF_RECORD_NUMBER, recordNumber);
	// 	PlayerPrefs.Save();

	// 	if (recordNumber % Const.SURVEY_TRIGGER == 0) {
	// 		// Only show survey if user has not has not done survey v1
	// 		// No longer have option to refuse survey
	// 		if (!PlayerPrefs.HasKey(Const.PREF_SURVEY_V1_DONE))  {
	// 			//Debug.Log("Show survey");
	// 			surveyPopUpPanelGO.SetActive(true);
	// 		}
	// 	}
	// }
}