using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameSettingsPopup : MonoBehaviour
{
    private const float PanelWidth = 740f;
    private const float PanelHeight = 570f;

    private Button optionsButton;
    private TMP_FontAsset menuFont;
    private GameObject popupRoot;
    private Toggle autoShootToggle;
    private TMP_Text autoShootStateText;
    private bool isOpen;

    private void Awake()
    {
        optionsButton = GetComponent<Button>();
        if (optionsButton == null)
        {
            Debug.LogError("Game Settings Popup requires a Button component.", this);
            enabled = false;
            return;
        }

        TMP_Text label = GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            menuFont = label.font;

        optionsButton.onClick = new Button.ButtonClickedEvent();
        optionsButton.onClick.AddListener(Open);
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    private void OnDestroy()
    {
        if (optionsButton != null)
            optionsButton.onClick.RemoveListener(Open);
        GameSettings.Save();
    }

    private void Open()
    {
        if (popupRoot == null && !CreatePopup())
            return;

        popupRoot.SetActive(true);
        popupRoot.transform.SetAsLastSibling();
        isOpen = true;
        if (autoShootToggle != null)
        {
            autoShootToggle.SetIsOnWithoutNotify(GameSettings.AutomaticShootingEnabled);
            autoShootStateText.text = autoShootToggle.isOn ? "ON" : "OFF";
        }

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void Close()
    {
        isOpen = false;
        GameSettings.Save();
        popupRoot.SetActive(false);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(optionsButton.gameObject);
    }

    private bool CreatePopup()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Game Settings Popup could not find its parent Canvas.", this);
            return false;
        }

        popupRoot = new GameObject("Game Settings Overlay", typeof(RectTransform), typeof(Image));
        popupRoot.transform.SetParent(canvas.rootCanvas.transform, false);
        RectTransform overlayRect = popupRoot.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = popupRoot.GetComponent<Image>();
        overlayImage.color = new Color(0.04f, 0.09f, 0.05f, 0.78f);
        overlayImage.raycastTarget = true;

        GameObject panel = new GameObject("Game Settings Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(popupRoot.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.88f, 0.97f, 0.79f, 1f);
        panelImage.raycastTarget = true;

        CreateText(panel.transform, "Settings Heading", "GAME SETTINGS",
            new Vector2(0f, -17f), new Vector2(680f, 42f), 28f,
            FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.13f, 0.31f, 0.14f));

        float rowY = -63f;
        AddSliderRow(panel.transform, "Master", rowY, GameSettings.MasterVolume, GameSettings.SetMasterVolume);
        rowY -= 33f;
        AddSliderRow(panel.transform, "Music Master", rowY, GameSettings.MusicMasterVolume, GameSettings.SetMusicMasterVolume);
        rowY -= 33f;
        AddSliderRow(panel.transform, "Background Music", rowY, GameSettings.BackgroundMusicVolume, GameSettings.SetBackgroundMusicVolume);
        rowY -= 33f;
        AddSliderRow(panel.transform, "Boss Music", rowY, GameSettings.BossMusicVolume, GameSettings.SetBossMusicVolume);
        rowY -= 33f;
        AddSliderRow(panel.transform, "Sound Effects Master", rowY, GameSettings.EffectsMasterVolume, GameSettings.SetEffectsMasterVolume);
        rowY -= 33f;
        AddSliderRow(panel.transform, "Shooting", rowY, GameSettings.ShootingVolume, GameSettings.SetShootingVolume);
        rowY -= 33f;
        AddSliderRow(panel.transform, "Explosions", rowY, GameSettings.ExplosionsVolume, GameSettings.SetExplosionsVolume);
        rowY -= 33f;
        AddSliderRow(panel.transform, "Abilities & Grenades", rowY, GameSettings.AbilitiesVolume, GameSettings.SetAbilitiesVolume);
        rowY -= 33f;
        AddSliderRow(panel.transform, "Slimes & Waves", rowY, GameSettings.WorldVolume, GameSettings.SetWorldVolume);
        rowY -= 33f;
        AddSliderRow(panel.transform, "UI Sounds", rowY, GameSettings.InterfaceVolume, GameSettings.SetInterfaceVolume);
        rowY -= 36f;
        AddSliderRow(panel.transform, "Screen Shake", rowY, GameSettings.ScreenShake, GameSettings.SetScreenShake);

        AddAutoShootToggle(panel.transform, -466f);
        AddCloseButton(panel.transform, -520f);

        popupRoot.SetActive(false);
        return true;
    }

    private void AddSliderRow(Transform parent, string label, float anchoredY,
        float initialValue, Action<float> setter)
    {
        CreateText(parent, label + " Label", label,
            new Vector2(-267f, anchoredY), new Vector2(190f, 28f), 16f,
            FontStyles.Normal, TextAlignmentOptions.MidlineLeft, new Color(0.12f, 0.16f, 0.12f));

        TextMeshProUGUI valueText = CreateText(parent, label + " Value", "",
            new Vector2(315f, anchoredY), new Vector2(80f, 28f), 16f,
            FontStyles.Bold, TextAlignmentOptions.MidlineRight, new Color(0.13f, 0.31f, 0.14f));

        Slider slider = CreateSlider(parent, label + " Slider", new Vector2(30f, anchoredY));
        slider.SetValueWithoutNotify(Mathf.RoundToInt(Mathf.Clamp01(initialValue) * 100f));
        UpdatePercentage(valueText, slider.value);
        slider.onValueChanged.AddListener(value =>
        {
            UpdatePercentage(valueText, value);
            setter(value / 100f);
        });
    }

    private Slider CreateSlider(Transform parent, string objectName, Vector2 anchoredPosition)
    {
        GameObject sliderObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 1f);
        sliderRect.anchorMax = new Vector2(0.5f, 1f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = anchoredPosition;
        sliderRect.sizeDelta = new Vector2(390f, 20f);

        Image background = sliderObject.GetComponent<Image>();
        background.color = new Color(0.45f, 0.56f, 0.4f, 1f);
        background.raycastTarget = true;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(8f, 0f);
        fillAreaRect.offsetMax = new Vector2(-8f, 0f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color(0.26f, 0.69f, 0.3f, 1f);
        fillImage.raycastTarget = false;

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(8f, 0f);
        handleAreaRect.offsetMax = new Vector2(-8f, 0f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(18f, 24f);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.color = new Color(0.15f, 0.42f, 0.18f, 1f);
        handleImage.raycastTarget = true;

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.wholeNumbers = true;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private void AddAutoShootToggle(Transform parent, float anchoredY)
    {
        CreateText(parent, "Auto Shoot Label", "Auto-shoot (A)",
            new Vector2(-267f, anchoredY), new Vector2(190f, 30f), 16f,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.12f, 0.16f, 0.12f));

        GameObject toggleObject = new GameObject("Auto Shoot Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
        toggleObject.transform.SetParent(parent, false);
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 1f);
        toggleRect.anchorMax = new Vector2(0f, 1f);
        toggleRect.pivot = new Vector2(0f, 1f);
        toggleRect.anchoredPosition = new Vector2(245f, anchoredY + 5f);
        toggleRect.sizeDelta = new Vector2(28f, 28f);

        Image toggleBackground = toggleObject.GetComponent<Image>();
        toggleBackground.color = new Color(0.45f, 0.56f, 0.4f, 1f);
        toggleBackground.raycastTarget = true;

        GameObject checkObject = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkObject.transform.SetParent(toggleObject.transform, false);
        RectTransform checkRect = checkObject.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.2f, 0.2f);
        checkRect.anchorMax = new Vector2(0.8f, 0.8f);
        checkRect.offsetMin = Vector2.zero;
        checkRect.offsetMax = Vector2.zero;
        Image checkImage = checkObject.GetComponent<Image>();
        checkImage.color = new Color(0.13f, 0.48f, 0.18f, 1f);
        checkImage.raycastTarget = false;

        autoShootStateText = CreateText(parent, "Auto Shoot State", "",
            new Vector2(292f, anchoredY), new Vector2(75f, 30f), 16f,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.13f, 0.31f, 0.14f));

        autoShootToggle = toggleObject.GetComponent<Toggle>();
        autoShootToggle.targetGraphic = toggleBackground;
        autoShootToggle.graphic = checkImage;
        autoShootToggle.SetIsOnWithoutNotify(GameSettings.AutomaticShootingEnabled);
        autoShootStateText.text = autoShootToggle.isOn ? "ON" : "OFF";
        autoShootToggle.onValueChanged.AddListener(isOn =>
        {
            GameSettings.SetAutomaticShootingEnabled(isOn);
            autoShootStateText.text = isOn ? "ON" : "OFF";
        });
    }

    private void AddCloseButton(Transform parent, float anchoredY)
    {
        GameObject buttonObject = new GameObject("Close Settings Button",
            typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 1f);
        buttonRect.anchorMax = new Vector2(0.5f, 1f);
        buttonRect.pivot = new Vector2(0.5f, 1f);
        buttonRect.anchoredPosition = new Vector2(0f, anchoredY);
        buttonRect.sizeDelta = new Vector2(190f, 38f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.25f, 0.58f, 0.27f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(Close);

        CreateText(buttonObject.transform, "Close Settings Label", "CLOSE",
            Vector2.zero, new Vector2(180f, 34f), 18f,
            FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
    }

    private TextMeshProUGUI CreateText(Transform parent, string objectName, string value,
        Vector2 anchoredPosition, Vector2 size, float fontSize, FontStyles style,
        TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = anchoredPosition;
        textRect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (menuFont != null)
            text.font = menuFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    private static void UpdatePercentage(TMP_Text text, float value)
    {
        text.text = Mathf.RoundToInt(value) + "%";
    }
}
