using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class TitleCreditsPopup : MonoBehaviour
{
    private static readonly string[] Credits =
    {
        "Game Concept & Design — Ernest",
        "Programming & Systems — Ernest",
        "Gameplay, Weapons & Upgrades — Ernest",
        "Enemies, Waves & Balancing — Ernest",
        "UI & Menus — Ernest",
        "Audio Integration — Ernest",
        "Testing & Polish — Jin",
        "Everything Else — Jin"
    };

    private UnityEngine.UI.Button creditsButton;
    private TMP_FontAsset creditsFont;
    private GameObject popupRoot;
    private bool isOpen;

    private void Awake()
    {
        creditsButton = GetComponent<UnityEngine.UI.Button>();
        if (creditsButton == null)
        {
            Debug.LogError("Title Credits Popup requires a Button component.", this);
            enabled = false;
            return;
        }

        TMP_Text label = GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            creditsFont = label.font;

        creditsButton.onClick.AddListener(OpenPopup);
    }

    private void Update()
    {
        if (isOpen && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)))
            ClosePopup();
    }

    private void OnDestroy()
    {
        if (creditsButton != null)
            creditsButton.onClick.RemoveListener(OpenPopup);
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

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(creditsButton.gameObject);
    }

    private bool CreatePopup()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Title Credits Popup could not find its parent Canvas.", this);
            return false;
        }

        popupRoot = new GameObject("Credits Popup Overlay", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        popupRoot.transform.SetParent(canvas.rootCanvas.transform, false);

        RectTransform overlayRect = popupRoot.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        UnityEngine.UI.Image overlayImage = popupRoot.GetComponent<UnityEngine.UI.Image>();
        overlayImage.color = new Color(0.04f, 0.09f, 0.05f, 0.78f);
        overlayImage.raycastTarget = true;

        GameObject panel = new GameObject("Credits Panel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        panel.transform.SetParent(popupRoot.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(700f, 540f);

        UnityEngine.UI.Image panelImage = panel.GetComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0.88f, 0.97f, 0.79f, 1f);
        panelImage.raycastTarget = true;

        CreateText(panel.transform, "Credits Heading", "CREDITS",
            new Vector2(0f, -18f), new Vector2(630f, 48f), 32f,
            FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.13f, 0.31f, 0.14f), true);

        for (int i = 0; i < Credits.Length; i++)
        {
            CreateText(panel.transform, "Credit " + (i + 1), Credits[i],
                new Vector2(0f, 145f - i * 37f), new Vector2(620f, 32f), 19f,
                FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.12f, 0.16f, 0.12f), false);
        }

        CreateCloseButton(panel.transform);
        popupRoot.SetActive(false);
        return true;
    }

    private void CreateCloseButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("Close Credits Button",
            typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -215f);
        buttonRect.sizeDelta = new Vector2(190f, 42f);

        UnityEngine.UI.Image buttonImage = buttonObject.GetComponent<UnityEngine.UI.Image>();
        buttonImage.color = new Color(0.25f, 0.58f, 0.27f, 1f);

        UnityEngine.UI.Button button = buttonObject.GetComponent<UnityEngine.UI.Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(ClosePopup);

        CreateText(buttonObject.transform, "Close Credits Label", "CLOSE  •  SPACE",
            Vector2.zero, new Vector2(180f, 38f), 17f,
            FontStyles.Bold, TextAlignmentOptions.Center, Color.white, false);
    }

    private void CreateText(Transform parent, string objectName, string value,
        Vector2 position, Vector2 size, float fontSize, FontStyles style,
        TextAlignmentOptions alignment, Color color, bool anchorToTop)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        if (anchorToTop)
        {
            textRect.anchorMin = new Vector2(0.5f, 1f);
            textRect.anchorMax = new Vector2(0.5f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
        }
        else
        {
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
        }

        textRect.anchoredPosition = position;
        textRect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (creditsFont != null)
            text.font = creditsFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
    }
}
