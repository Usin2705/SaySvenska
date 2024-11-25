using UnityEngine;

public class MainTab : MonoBehaviour
{
    [SerializeField] GameObject TopPanelGO;
    [SerializeField] GameObject DropShadowLGO;
    [SerializeField] GameObject DropShadowBGO;
    [SerializeField] GameObject DropShadowRGO;
    [SerializeField] GameObject TextInputPanelGO;

    [SerializeField] TMPro.TMP_InputField InputText;

    [SerializeField] GameObject StartButtonGO;

    [SerializeField] GameObject RecordButtonGO;


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
        InputText.onSelect.AddListener(delegate { OnInputTextFocus(InputText); });
        InputText.onDeselect.AddListener(delegate { OnInputTextUnfocus(InputText); });
        InputText.onValueChanged.AddListener(delegate { OnInputTextChange(InputText); });
        InputText.onEndEdit.AddListener(delegate { OnInputTextFinish(InputText); });
        StartButtonGO.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(delegate { OnStartButtonClickFocusText(); });
        RecordButtonGO.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(delegate { OnRecordButtonClick(); });
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
            StartButtonGO.GetComponent<UnityEngine.UI.Button>().interactable = true;
        }        
        else
        {
            StartButtonGO.GetComponent<UnityEngine.UI.Button>().interactable = false;
        }

        // If the input field is longer than max char, do not allow to enter more
        if (inputField.text.Length > Const.MAX_CHAR_ALLOWED)
        {
            inputField.text = inputField.text.Substring(0, Const.MAX_CHAR_ALLOWED);
            // Debug.Log(inputField.name + " restricted to : " + inputField.text);
        }
    }

    private void OnInputTextFinish(TMPro.TMP_InputField inputField)
    {
        Debug.Log(inputField.name + " text finished: " + inputField.text);
        OnStartButtonClickDoneText();
    }

    private void OnStartButtonClickFocusText()
    {
        Debug.Log("Start button clicked to focus on EditText.");
        InputText.Select();
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

    public void SetUpStartUI()
    {
        // Set up the default image for the top panel. This image is a sprite that is loaded from the Resources folder.
        TopPanelGO.GetComponent<UnityEngine.UI.Image>().sprite = Resources.Load<Sprite>("app_icons/top_bar");

        // Move the top panel to the top of the screen
        TopPanelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -280);
        // Set the default size for the top panel
        TopPanelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 560);

        // Change the text prompt of PromptText
        TextInputPanelGO.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Did you find a new \nSwedish word?";

        TextInputPanelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -904);

        DropShadowLGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-384, -904);
        DropShadowBGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -1188);
        DropShadowRGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(384, -904);
        
        // Enable the StartButtonGO
        StartButtonGO.SetActive(true);
        // Enable the start button button, not GO        
        StartButtonGO.GetComponent<UnityEngine.UI.Button>().interactable = true;
        
        // Disable the ReadAloudTextGO
        TextInputPanelGO.transform.Find("ReadAloudText").gameObject.SetActive(false);

        // Disable the recording button
        RecordButtonGO.SetActive(false);
    }

    public void SetupFocusTextUI()
    {
        // Set up the smaller image for the top panel. This image is a sprite that is loaded from the Resources folder.
        TopPanelGO.GetComponent<UnityEngine.UI.Image>().sprite = Resources.Load<Sprite>("app_icons/top_bar_noicon");

        // Move the top panel to the top of the screen
        TopPanelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -125);
        // Set the default size for the top panel
        TopPanelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 250);

        // Change the text prompt of PromptText
        TextInputPanelGO.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Type in Swedish, and \nlet's practice!";

        TextInputPanelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -692);

        DropShadowLGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-384, -692);
        DropShadowBGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -976);
        DropShadowRGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(384, -692);

        // Disable the start button button, not GO
        // Disable the button will change its color to gray and make it unclickable (set in the Editor)
        StartButtonGO.GetComponent<UnityEngine.UI.Button>().interactable = false;
    }

    public void SetUpReadAloudUI()
    {
        // Change the text prompt of PromptText
        TextInputPanelGO.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Tap the record \nbutton and speak.";
        
        // Disable the InputTextGO
        InputText.gameObject.SetActive(false);

        TextInputPanelGO.transform.Find("ReadAloudText").gameObject.SetActive(true);
        TextInputPanelGO.transform.Find("ReadAloudText").GetComponent<TMPro.TextMeshProUGUI>().text = InputText.text;
        
        // Clean the input text
        InputText.text = "";

        // Disable the StartButtonGO
        StartButtonGO.SetActive(false);

        // Enable the recording button
        RecordButtonGO.SetActive(true);
    }
}
