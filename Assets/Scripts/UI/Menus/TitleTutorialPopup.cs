using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TitleTutorialPopup : MonoBehaviour
{
    private const string Instructions =
        "• Choose a weapon in the Armory, then start the game.\n" +
        "• Use the Armory, Upgrades, and Grenades tabs to prepare your loadout.\n" +
        "• Aim with the mouse and hold Left Mouse Button to fire. Press A to toggle auto-shoot.\n" +
        "• Press Q to equip or put away grenades. While equipped, press 1–4 to select Frag, Smoke, Flash, or Molotov, then click to throw.\n" +
        "• Unlocked abilities: Z = Sewer Pulse, X = EMP, C = Toxic Flood, N = Nuke.\n" +
        "• Survive each slime wave and protect your defence.";

    private Button tutorialButton;
    private TMP_FontAsset tutorialFont;
    private GameObject popupRoot;
    private bool isOpen;

    private void Awake()
    {
        tutorialButton = GetComponent<Button>();
        if (tutorialButton == null)
        {
            Debug.LogError("Title Tutorial Popup requires a Button component.", this);
            enabled = false;
            return;
        }

        TMP_Text label = GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            tutorialFont = label.font;

        // The scene's old Tutorial button callback pointed at QuitGame.
        tutorialButton.onClick = new Button.ButtonClickedEvent();
        tutorialButton.onClick.AddListener(OpenPopup);
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Space))
            ClosePopup();
    }

    private void OnDestroy()
    {
        if (tutorialButton != null)
            tutorialButton.onClick.RemoveListener(OpenPopup);
    }

    private void OpenPopup()
    {
        if (popupRoot == null && !CreatePopup())
            return;

        popupRoot.SetActive(true);
        popupRoot.transform.SetAsLastSibling();
        isOpen = true;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void ClosePopup()
    {
        isOpen = false;
        popupRoot.SetActive(false);
    }

    private bool CreatePopup()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Title Tutorial Popup could not find its parent Canvas.", this);
            return false;
        }

        Transform canvasTransform = canvas.rootCanvas.transform;
        popupRoot = new GameObject("Tutorial Popup Overlay", typeof(RectTransform), typeof(Image));
        popupRoot.transform.SetParent(canvasTransform, false);

        RectTransform overlayRect = popupRoot.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = popupRoot.GetComponent<Image>();
        overlayImage.color = new Color(0.04f, 0.09f, 0.05f, 0.78f);
        overlayImage.raycastTarget = true;

        GameObject panel = new GameObject("Tutorial Notes Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(popupRoot.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(700f, 500f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.88f, 0.97f, 0.79f, 1f);
        panelImage.raycastTarget = true;

        CreateText(panel.transform, "Tutorial Heading", "HOW TO PLAY",
            new Vector2(0f, -28f), new Vector2(630f, 48f), 32f,
            FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.13f, 0.31f, 0.14f));

        CreateText(panel.transform, "Tutorial Notes", Instructions,
            new Vector2(0f, -91f), new Vector2(620f, 330f), 22f,
            FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.12f, 0.16f, 0.12f));

        CreateText(panel.transform, "Tutorial Close Hint", "Click Space to close",
            new Vector2(0f, 27f), new Vector2(620f, 42f), 20f,
            FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.13f, 0.31f, 0.14f), true);

        popupRoot.SetActive(false);
        return true;
    }

    private void CreateText(Transform parent, string objectName, string value,
        Vector2 anchoredPosition, Vector2 size, float fontSize, FontStyles style,
        TextAlignmentOptions alignment, Color color, bool anchorToBottom = false)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = anchorToBottom ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 1f);
        textRect.anchorMax = textRect.anchorMin;
        textRect.pivot = anchorToBottom ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 1f);
        textRect.anchoredPosition = anchoredPosition;
        textRect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (tutorialFont != null)
            text.font = tutorialFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        text.margin = new Vector4(0f, 0f, 0f, 0f);
    }
}
