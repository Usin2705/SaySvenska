using UnityEngine;

public class MainTab : MonoBehaviour
{
    [SerializeField] GameObject topPanelGO;
    [SerializeField] GameObject dropShadowLGO;
    [SerializeField] GameObject dropShadowBGO;
    [SerializeField] GameObject dropShadowRGO;
    [SerializeField] GameObject textInputPanelGO;

    [SerializeField] TMPro.TMP_InputField inputText;

    [SerializeField] GameObject startButtonGO;
    [SerializeField] GameObject againButtonGO;
    [SerializeField] GameObject replayButtonGO;

    [SerializeField] GameObject recordButtonGO;


    /// <summary>
    /// Awake is called when the script instance is being loaded. This is used to initialize any variables or game state 
    /// before the game starts. Awake is called only once during the lifetime of the script instance.
    /// 
    /// In this method, we set up listeners for the input field's onSelect and onDeselect events. These listeners will 
    /// trigger the OnInputTextFocus and OnInputTextUnfocus methods respectively when the input field gains or loses focus.
    /// 
    /// Awake is called before any Start methods, making it a good place to set up references and initialize variables 
    /// that other scripts might depend on during their Start method.
    /// </summary>
    private void Awake() 
    {
        SetUpStartUI();
        inputText.onSelect.AddListener(delegate { OnInputTextFocus(inputText); });
        // inputText.onDeselect.AddListener(delegate { OnInputTextUnfocus(inputText); });
        inputText.onValueChanged.AddListener(delegate { OnInputTextChange(inputText); });
        
        // This is not working, as it trigger even when the text is unfocused
        // inputText.onEndEdit.AddListener(delegate { OnInputTextFinish(inputText); });
        startButtonGO.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(delegate { OnStartButtonClick(); });
        recordButtonGO.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(delegate { OnRecordButtonClick(); });
        againButtonGO.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(delegate { OnAgainButtonClick(); });
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Set the start UI to be active
        SetUpStartUI();

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnInputTextFocus(TMPro.TMP_InputField inputField)
    {
        Debug.Log(inputField.name + " is focused.");
        SetupFocusTextUI();
    }

    private void OnInputTextUnfocus(TMPro.TMP_InputField inputField)
    {
        Debug.Log(inputField.name + " is unfocused.");
    }

    /// <summary>
    /// Handles the event when the text in the input field changes.
    /// </summary>
    /// <param name="inputField">The input field whose text has changed.</param>
    /// <remarks>
    /// This method lenables or disables the Start button based on whether the input field contains any text.
    /// If the input field's text length is greater than 0, the Start button is made interactable.
    /// Otherwise, the Start button is made non-interactable.
    /// </remarks>
    private void OnInputTextChange(TMPro.TMP_InputField inputField)
    {
        // If the input field contains any text, enable the Start button
        if (inputField.text.Length > 0)
        {
            startButtonGO.GetComponent<UnityEngine.UI.Button>().interactable = true;
        }        
        else
        {
            startButtonGO.GetComponent<UnityEngine.UI.Button>().interactable = false;
        }

        // If the input field is longer than max char, do not allow to enter more
        if (inputField.text.Length > Const.MAX_CHAR_ALLOWED)
        {
            inputField.text = inputField.text.Substring(0, Const.MAX_CHAR_ALLOWED);
            // Debug.Log(inputField.name + " restricted to : " + inputField.text);
        }
    }

    /// <summary>
    /// Handles two type of event depend on the scenario: 
    /// 1. The user focus on the EditText when the EditText is empty -> change to EditText focus layout
    /// 2. The input field's text is finished and the SayIt button is clicked. -> change to new layout
    /// </summary>
    private void OnStartButtonClick()
    {
        Debug.Log("Start button clicked to focus on EditText.");
        if (inputText.text.Length > 0)
        {
            Debug.Log(inputText.name + " text finished: " + inputText.text);
            OnStartButtonClickDoneText();
        } else {
            inputText.Select();
            Debug.Log(inputText.name + " text finished but there is nothing: " + inputText.text);
        }
    }

    private void OnStartButtonClickDoneText()
    {
        Debug.Log("Start button clicked to finish EditText.");
        SetUpReadAloudUI();
    }

    private void OnRecordButtonClick() 
    {
        Debug.Log("Record button clicked.");
        
    }

    private void OnAgainButtonClick()
    {
        Debug.Log("Again button clicked.");
        SetupFocusTextUI();

        // Refocus on the input text
        inputText.Select();
    }

    public void SetUpStartUI()
    {
        // Set up the default image for the top panel. This image is a sprite that is loaded from the Resources folder.
        topPanelGO.GetComponent<UnityEngine.UI.Image>().sprite = Resources.Load<Sprite>("app_icons/top_bar");

        // Move the top panel to the top of the screen
        topPanelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -280);
        // Set the default size for the top panel
        topPanelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 560);

        // Change the text prompt of PromptText
        textInputPanelGO.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Did you find a new \nSwedish word?";

        textInputPanelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -904);

        dropShadowLGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-384, -904);
        dropShadowBGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -1188);
        dropShadowRGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(384, -904);
        
        // Enable the startButtonGO
        startButtonGO.SetActive(true);
        // Enable the start button button, not GO        
        startButtonGO.GetComponent<UnityEngine.UI.Button>().interactable = true;
        
        // Disable the ReadAloudTextGO
        textInputPanelGO.transform.Find("ReadAloudText").gameObject.SetActive(false);

        // Disable the recording button
        recordButtonGO.SetActive(false);
        againButtonGO.SetActive(false);
        replayButtonGO.SetActive(false);
    }

    public void SetupFocusTextUI()
    {
        // Set up the smaller image for the top panel. This image is a sprite that is loaded from the Resources folder.
        topPanelGO.GetComponent<UnityEngine.UI.Image>().sprite = Resources.Load<Sprite>("app_icons/top_bar_noicon");

        // Move the top panel to the top of the screen
        topPanelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -125);
        // Set the default size for the top panel
        topPanelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 250);

        // Change the text prompt of PromptText
        textInputPanelGO.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Type in Swedish, and \nlet's practice!";

        textInputPanelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -692);

        dropShadowLGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-384, -692);
        dropShadowBGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -976);
        dropShadowRGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(384, -692);

        // Enable the inputText
        inputText.gameObject.SetActive(true);

        // Disable the text prompt of PromptText
        textInputPanelGO.transform.Find("ReadAloudText").gameObject.SetActive(false);

        // Disable the start button button, not GO
        // Disable the button will change its color to gray and make it unclickable (set in the Editor)

        // Only disable the start button if there is no text in the input field
        if (inputText.text.Length == 0)
            startButtonGO.GetComponent<UnityEngine.UI.Button>().interactable = false;
        else {
            startButtonGO.GetComponent<UnityEngine.UI.Button>().interactable = true;
        }

        // Enable the startButtonGO
        startButtonGO.SetActive(true);

        // Disable the recording button        
        recordButtonGO.SetActive(false);
        againButtonGO.SetActive(false);
    }

    public void SetUpReadAloudUI()
    {
        // Change the text prompt of PromptText
        textInputPanelGO.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Tap the record \nbutton and speak.";
        
        // Disable the inputText
        inputText.gameObject.SetActive(false);

        textInputPanelGO.transform.Find("ReadAloudText").gameObject.SetActive(true);
        textInputPanelGO.transform.Find("ReadAloudText").GetComponent<TMPro.TextMeshProUGUI>().text = inputText.text;
        
        // Clean the input text
        inputText.text = "";

        // Disable the startButtonGO
        startButtonGO.SetActive(false);

        // Enable the recording button
        recordButtonGO.SetActive(true);
        againButtonGO.SetActive(true);
    }
}
