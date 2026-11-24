// AI assistance: Codex helped revise menu behavior and layouts. Ernest directed the design.
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameSettingsPopup : MonoBehaviour
{
    private const float MaxPanelWidth = 760f;
    private const float MaxPanelHeight = 620f;

    private Button optionsButton;
    private TMP_FontAsset menuFont;
    private GameObject popupRoot;
    private Toggle autoShootToggle;
    private TMP_Text autoShootStateText;
    private ScrollRect settingsScroll;
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
        Canvas.ForceUpdateCanvases();
        if (settingsScroll != null)
            settingsScroll.verticalNormalizedPosition = 1f;
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
        Rect canvasRect = canvas.rootCanvas.GetComponent<RectTransform>().rect;
        float panelWidth = Mathf.Min(MaxPanelWidth, canvasRect.width - 32f);
        float panelHeight = Mathf.Min(MaxPanelHeight, canvasRect.height - 32f);
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.88f, 0.97f, 0.79f, 1f);
        panelImage.raycastTarget = true;

        CreateText(panel.transform, "Settings Heading", "GAME SETTINGS",
            new Vector2(0f, -15f), new Vector2(panelWidth - 48f, 40f), 28f,
            FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.13f, 0.31f, 0.14f));

        Transform content = CreateScrollArea(panel.transform, panelWidth, panelHeight);
        float rowWidth = panelWidth - 104f;
        bool compact = panelWidth < 610f;
        AddSectionHeader(content, "GENERAL", rowWidth);
        AddSliderRow(content, "Master", rowWidth, compact, GameSettings.MasterVolume, GameSettings.SetMasterVolume);
        AddSectionHeader(content, "MUSIC", rowWidth);
        AddSliderRow(content, "Music Master", rowWidth, compact, GameSettings.MusicMasterVolume, GameSettings.SetMusicMasterVolume);
        AddSliderRow(content, "Background Music", rowWidth, compact, GameSettings.BackgroundMusicVolume, GameSettings.SetBackgroundMusicVolume);
        AddSliderRow(content, "Boss Music", rowWidth, compact, GameSettings.BossMusicVolume, GameSettings.SetBossMusicVolume);
        AddSectionHeader(content, "SOUND EFFECTS", rowWidth);
        AddSliderRow(content, "Sound Effects Master", rowWidth, compact, GameSettings.EffectsMasterVolume, GameSettings.SetEffectsMasterVolume);
        AddSliderRow(content, "Shooting", rowWidth, compact, GameSettings.ShootingVolume, GameSettings.SetShootingVolume);
        AddSliderRow(content, "Explosions", rowWidth, compact, GameSettings.ExplosionsVolume, GameSettings.SetExplosionsVolume);
        AddSliderRow(content, "Abilities & Grenades", rowWidth, compact, GameSettings.AbilitiesVolume, GameSettings.SetAbilitiesVolume);
        AddSliderRow(content, "Slimes & Waves", rowWidth, compact, GameSettings.WorldVolume, GameSettings.SetWorldVolume);
        AddSliderRow(content, "UI Sounds", rowWidth, compact, GameSettings.InterfaceVolume, GameSettings.SetInterfaceVolume);
        AddSectionHeader(content, "GAMEPLAY", rowWidth);
        AddSliderRow(content, "Screen Shake", rowWidth, compact, GameSettings.ScreenShake, GameSettings.SetScreenShake);
        AddAutoShootToggle(content, rowWidth);

        AddCloseButton(panel.transform, -panelHeight + 54f);

        popupRoot.SetActive(false);
        return true;
    }

    private Transform CreateScrollArea(Transform parent, float panelWidth, float panelHeight)
    {
        GameObject scrollObject = new GameObject("Settings Scroll Area", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(parent, false);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.5f, 1f);
        scrollRect.anchorMax = new Vector2(0.5f, 1f);
        scrollRect.pivot = new Vector2(0.5f, 1f);
        scrollRect.anchoredPosition = new Vector2(0f, -62f);
        scrollRect.sizeDelta = new Vector2(panelWidth - 44f, panelHeight - 136f);

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = new Vector2(-20f, 0f);
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = Color.white;
        viewportObject.GetComponent<Mask>().showMaskGraphic = false;

        GameObject contentObject = new GameObject("Settings Content", typeof(RectTransform),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 12);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject scrollbarObject = new GameObject("Scroll Bar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(scrollObject.transform, false);
        RectTransform barRect = scrollbarObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(1f, 0f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot = new Vector2(1f, 0.5f);
        barRect.anchoredPosition = new Vector2(-2f, 0f);
        barRect.sizeDelta = new Vector2(12f, -6f);
        scrollbarObject.GetComponent<Image>().color = new Color(0.45f, 0.56f, 0.4f, 1f);

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(scrollbarObject.transform, false);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;
        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = new Color(0.15f, 0.42f, 0.18f, 1f);
        Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        settingsScroll = scrollObject.GetComponent<ScrollRect>();
        settingsScroll.viewport = viewportRect;
        settingsScroll.content = contentRect;
        settingsScroll.horizontal = false;
        settingsScroll.vertical = true;
        settingsScroll.movementType = ScrollRect.MovementType.Clamped;
        settingsScroll.scrollSensitivity = 36f;
        settingsScroll.verticalScrollbar = scrollbar;
        return contentObject.transform;
    }

    private Transform CreateLayoutRow(Transform parent, string name, float height)
    {
        GameObject rowObject = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
        rowObject.transform.SetParent(parent, false);
        rowObject.GetComponent<LayoutElement>().preferredHeight = height;
        return rowObject.transform;
    }

    private void AddSectionHeader(Transform parent, string heading, float rowWidth)
    {
        Transform row = CreateLayoutRow(parent, heading + " Header", 38f);
        CreateText(row, heading + " Text", heading,
            new Vector2(0f, -8f), new Vector2(rowWidth, 28f), 19f,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.13f, 0.31f, 0.14f));
    }

    private void AddSliderRow(Transform parent, string label, float rowWidth, bool compact,
        float initialValue, Action<float> setter)
    {
        Transform row = CreateLayoutRow(parent, label + " Row", compact ? 72f : 50f);
        float labelWidth = compact ? rowWidth - 80f : Mathf.Min(190f, rowWidth * 0.32f);
        float labelX = -rowWidth * 0.5f + labelWidth * 0.5f;
        float labelY = compact ? -7f : -11f;
        CreateText(row, label + " Label", label,
            new Vector2(labelX, labelY), new Vector2(labelWidth, 28f), 16f,
            FontStyles.Normal, TextAlignmentOptions.MidlineLeft, new Color(0.12f, 0.16f, 0.12f));

        TextMeshProUGUI valueText = CreateText(row, label + " Value", "",
            new Vector2(rowWidth * 0.5f - 36f, labelY), new Vector2(72f, 28f), 16f,
            FontStyles.Bold, TextAlignmentOptions.MidlineRight, new Color(0.13f, 0.31f, 0.14f));

        float sliderWidth = compact ? rowWidth - 20f : rowWidth - labelWidth - 88f;
        float sliderX = compact ? 0f : -rowWidth * 0.5f + labelWidth + 8f + sliderWidth * 0.5f;
        Slider slider = CreateSlider(row, label + " Slider",
            new Vector2(sliderX, compact ? -51f : -25f), sliderWidth);
        slider.SetValueWithoutNotify(Mathf.RoundToInt(Mathf.Clamp01(initialValue) * 100f));
        UpdatePercentage(valueText, slider.value);
        slider.onValueChanged.AddListener(value =>
        {
            UpdatePercentage(valueText, value);
            setter(value / 100f);
        });
    }

    private Slider CreateSlider(Transform parent, string objectName, Vector2 anchoredPosition, float width)
    {
        GameObject sliderObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 1f);
        sliderRect.anchorMax = new Vector2(0.5f, 1f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = anchoredPosition;
        sliderRect.sizeDelta = new Vector2(width, 20f);

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

    private void AddAutoShootToggle(Transform parent, float rowWidth)
    {
        Transform row = CreateLayoutRow(parent, "Auto Shoot Row", 54f);
        CreateText(row, "Auto Shoot Label", "Auto-shoot (A)",
            new Vector2(-rowWidth * 0.5f + 95f, -11f), new Vector2(190f, 30f), 16f,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.12f, 0.16f, 0.12f));

        GameObject toggleObject = new GameObject("Auto Shoot Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
        toggleObject.transform.SetParent(row, false);
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0.5f, 1f);
        toggleRect.anchorMax = new Vector2(0.5f, 1f);
        toggleRect.pivot = new Vector2(0.5f, 0.5f);
        toggleRect.anchoredPosition = new Vector2(rowWidth * 0.5f - 105f, -26f);
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

        autoShootStateText = CreateText(row, "Auto Shoot State", "",
            new Vector2(rowWidth * 0.5f - 37f, -11f), new Vector2(70f, 30f), 16f,
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
        SlimePixelUiTheme.ApplyButtonSkin(button);
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
