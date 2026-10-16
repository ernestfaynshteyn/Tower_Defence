using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PrototypeArmoryBootstrap : MonoBehaviour
{
    [Header("Existing prototype-scene UI")]
    [SerializeField] private UnityEngine.UI.Toggle upgradesTab;
    [SerializeField] private UnityEngine.UI.Toggle grenadesTab;
    [SerializeField] private UnityEngine.UI.Toggle armoryTab;
    [SerializeField] private UnityEngine.UI.ToggleGroup tabGroup;
    [SerializeField] private RectTransform tabParent;
    [SerializeField] private GameObject upgradesPanel;
    [SerializeField] private GameObject grenadesPanel;

    private readonly Dictionary<PrototypePrimaryWeapon, UnityEngine.UI.Button> primaryButtons =
        new Dictionary<PrototypePrimaryWeapon, UnityEngine.UI.Button>();
    private readonly Dictionary<PrototypePrimaryWeapon, TextMeshProUGUI> primaryButtonLabels =
        new Dictionary<PrototypePrimaryWeapon, TextMeshProUGUI>();
    private readonly Dictionary<PrototypePrimaryWeapon, GameObject> primaryCards =
        new Dictionary<PrototypePrimaryWeapon, GameObject>();
    private readonly Dictionary<PrototypeAttachment, UnityEngine.UI.Button> attachmentButtons =
        new Dictionary<PrototypeAttachment, UnityEngine.UI.Button>();
    private readonly Dictionary<PrototypeAttachment, TextMeshProUGUI> attachmentButtonLabels =
        new Dictionary<PrototypeAttachment, TextMeshProUGUI>();
    private readonly Dictionary<PrototypeAttachment, TextMeshProUGUI> attachmentDescriptions =
        new Dictionary<PrototypeAttachment, TextMeshProUGUI>();
    private readonly Dictionary<PrototypeAttachment, string> attachmentBaseDescriptions =
        new Dictionary<PrototypeAttachment, string>();
    private readonly Dictionary<PrototypeWeaponUpgrade, UnityEngine.UI.Button> weaponUpgradeButtons =
        new Dictionary<PrototypeWeaponUpgrade, UnityEngine.UI.Button>();
    private readonly Dictionary<PrototypeWeaponUpgrade, TextMeshProUGUI> weaponUpgradeButtonLabels =
        new Dictionary<PrototypeWeaponUpgrade, TextMeshProUGUI>();
    private readonly Dictionary<PrototypeWeaponUpgrade, TextMeshProUGUI> weaponUpgradeTitles =
        new Dictionary<PrototypeWeaponUpgrade, TextMeshProUGUI>();
    private readonly Dictionary<PrototypeWeaponUpgrade, TextMeshProUGUI> weaponUpgradeDescriptions =
        new Dictionary<PrototypeWeaponUpgrade, TextMeshProUGUI>();

    private PrototypeArmoryCombat combat;
    private GameObject armoryPanel;
    private TextMeshProUGUI weaponUpgradeHeading;
    private TextMeshProUGUI weaponStatsLabel;
    private TextMeshProUGUI armoryFeedbackLabel;
    private TextMeshProUGUI resetButtonLabel;
    private UnityEngine.UI.ScrollRect armoryScrollRect;
    private UnityEngine.UI.Button resetButton;
    private Coroutine feedbackRoutine;
    private bool uiReady;
    private bool resetConfirmationPending;
    private float resetConfirmationUntil;
    private int configuredScreenWidth;
    private int configuredScreenHeight;

    private void Start()
    {
        PlayerHealth player = FindFirstObjectByType<PlayerHealth>();
        if (player == null)
        {
            Debug.LogError("The Armory Prototype could not find PlayerHealth in this scene.", this);
            enabled = false;
            return;
        }

        combat = player.gameObject.AddComponent<PrototypeArmoryCombat>();
        combat.Initialize(player);
        combat.StateChanged += RefreshButtons;

        StartCoroutine(InitializeUiAfterCanvasLayout());
    }

    private IEnumerator InitializeUiAfterCanvasLayout()
    {
        // A scene loaded from the selection screen does not have a valid Canvas
        // rectangle until the following frame. Waiting here keeps its layout
        // identical to launching the gameplay scene directly in the Editor.
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (!ValidateUiReferences())
            yield break;

        BuildArmoryPanel();
        BuildThirdTab();
        RefreshButtons();
        uiReady = true;
        RebuildShopLayout();
    }

    private void LateUpdate()
    {
        if (resetConfirmationPending && Time.unscaledTime > resetConfirmationUntil)
            CancelResetConfirmation();

        if (uiReady && (configuredScreenWidth != Screen.width || configuredScreenHeight != Screen.height))
            RebuildShopLayout();
    }

    private bool ValidateUiReferences()
    {
        if (upgradesTab != null && grenadesTab != null && armoryTab != null && tabGroup != null && tabParent != null &&
            upgradesPanel != null && grenadesPanel != null)
            return true;

        Debug.LogError("The Armory Prototype scene is missing one or more shop UI references.", this);
        return false;
    }

    private void BuildThirdTab()
    {
        tabGroup.allowSwitchOff = false;

        ConfigureTabToggle(upgradesTab, 0, "UPGRADES");
        ConfigureTabToggle(grenadesTab, 1, "GRENADES");
        ConfigureTabToggle(armoryTab, 2, "ARMORY");

        // Replace the legacy static callbacks. Those callbacks ran even when a
        // toggle was turning off, so two panels could reopen over each other.
        upgradesTab.onValueChanged = new UnityEngine.UI.Toggle.ToggleEvent();
        grenadesTab.onValueChanged = new UnityEngine.UI.Toggle.ToggleEvent();
        armoryTab.onValueChanged = new UnityEngine.UI.Toggle.ToggleEvent();

        upgradesTab.group = tabGroup;
        grenadesTab.group = tabGroup;
        armoryTab.group = tabGroup;

        upgradesTab.SetIsOnWithoutNotify(true);
        grenadesTab.SetIsOnWithoutNotify(false);
        armoryTab.SetIsOnWithoutNotify(false);

        upgradesTab.onValueChanged.AddListener(HandleUpgradesTab);
        grenadesTab.onValueChanged.AddListener(HandleGrenadesTab);
        armoryTab.onValueChanged.AddListener(HandleArmoryTab);
        ShowTab(ShopTab.Upgrades);
    }

    private enum ShopTab
    {
        Upgrades,
        Grenades,
        Armory
    }

    private void RebuildShopLayout()
    {
        ConfigureShopForCurrentResolution();
        ConfigureTabToggle(upgradesTab, 0, "UPGRADES");
        ConfigureTabToggle(grenadesTab, 1, "GRENADES");
        ConfigureTabToggle(armoryTab, 2, "ARMORY");
        SetTabVisual(upgradesTab, upgradesTab != null && upgradesTab.isOn);
        SetTabVisual(grenadesTab, grenadesTab != null && grenadesTab.isOn);
        SetTabVisual(armoryTab, armoryTab != null && armoryTab.isOn);
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(tabParent);
        configuredScreenWidth = Screen.width;
        configuredScreenHeight = Screen.height;
    }

    private void ConfigureShopForCurrentResolution()
    {
        RectTransform frame = tabParent != null ? tabParent.parent as RectTransform : null;
        RectTransform shopRoot = frame != null ? frame.parent as RectTransform : null;
        if (frame == null || shopRoot == null)
            return;

        // The copied scene was authored for 4K with a fixed +1083 X offset.
        // Anchor the shop to the screen instead so the third tab cannot sit off-camera.
        shopRoot.anchorMin = Vector2.zero;
        shopRoot.anchorMax = Vector2.one;
        shopRoot.pivot = new Vector2(0.5f, 0.5f);
        shopRoot.anchoredPosition = Vector2.zero;
        shopRoot.sizeDelta = Vector2.zero;
        shopRoot.localScale = Vector3.one;
        shopRoot.SetAsLastSibling();

        // Keep every shop tab and purchase panel above HUD numbers and the
        // throwable selector, even when those objects are later siblings.
        Canvas frontCanvas = shopRoot.GetComponent<Canvas>();
        if (frontCanvas == null)
            frontCanvas = shopRoot.gameObject.AddComponent<Canvas>();
        frontCanvas.overrideSorting = true;
        frontCanvas.sortingOrder = 250;
        if (shopRoot.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            shopRoot.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        Canvas.ForceUpdateCanvases();

        frame.anchorMin = new Vector2(1f, 0.5f);
        frame.anchorMax = new Vector2(1f, 0.5f);
        frame.pivot = new Vector2(1f, 0.5f);
        frame.anchoredPosition = new Vector2(-40f, 0f);

        float widthScale = (shopRoot.rect.width - 80f) / Mathf.Max(1f, frame.sizeDelta.x);
        float heightScale = (shopRoot.rect.height - 80f) / Mathf.Max(1f, frame.sizeDelta.y);
        float fitScale = Mathf.Clamp(Mathf.Min(widthScale, heightScale), 0.25f, 1f);
        frame.localScale = Vector3.one * fitScale;
    }

    private static void ConfigureTabToggle(UnityEngine.UI.Toggle toggle, int index, string labelText)
    {
        if (toggle == null)
            return;

        toggle.gameObject.SetActive(true);
        toggle.interactable = true;
        toggle.transition = UnityEngine.UI.Selectable.Transition.None;
        toggle.transform.SetSiblingIndex(index);
        if (toggle.GetComponent<ButtonAudioHover>() == null)
            toggle.gameObject.AddComponent<ButtonAudioHover>();

        RectTransform rect = toggle.transform as RectTransform;
        if (rect != null)
        {
            float leftAnchor = index / 3f;
            float rightAnchor = (index + 1f) / 3f;
            rect.anchorMin = new Vector2(leftAnchor, 0f);
            rect.anchorMax = new Vector2(rightAnchor, 1f);
            rect.offsetMin = new Vector2(index == 0 ? 4f : 8f, 6f);
            rect.offsetMax = new Vector2(index == 2 ? -4f : -8f, -6f);
            rect.localScale = Vector3.one;
        }

        if (toggle.targetGraphic is UnityEngine.UI.Image background)
        {
            background.raycastTarget = true;
            background.color = new Color(0.82f, 0.85f, 0.9f, 1f);
        }

        TextMeshProUGUI label = toggle.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.gameObject.SetActive(true);
            label.text = labelText;
            label.color = new Color(0.08f, 0.1f, 0.14f, 1f);
            label.fontSize = 36f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 15f;
            label.fontSizeMax = 36f;
            label.alignment = TextAlignmentOptions.Center;
            label.margin = Vector4.zero;
            label.raycastTarget = false;

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 8f);
            labelRect.offsetMax = new Vector2(-12f, -8f);
            labelRect.localScale = Vector3.one;
        }
    }

    private void BuildArmoryPanel()
    {
        RectTransform referenceRect = upgradesPanel.transform as RectTransform;
        armoryPanel = new GameObject("Armory Prototype Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        RectTransform panelRect = armoryPanel.GetComponent<RectTransform>();
        panelRect.SetParent(upgradesPanel.transform.parent, false);
        CopyRect(referenceRect, panelRect);

        UnityEngine.UI.Image panelImage = armoryPanel.GetComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0.045f, 0.055f, 0.075f, 0.985f);
        panelImage.raycastTarget = true;

        CreateHeader(panelRect);
        RectTransform content = CreateScrollView(panelRect);

        weaponUpgradeHeading = CreateSectionHeading(content, "ACTIVE WEAPON UPGRADES");
        weaponStatsLabel = CreateStatsCard(content);
        foreach (PrototypeWeaponUpgrade upgrade in Enum.GetValues(typeof(PrototypeWeaponUpgrade)))
            CreateWeaponUpgradeCard(content, upgrade);

        CreateSectionHeading(content, "PRIMARY WEAPONS — FIVE ALTERNATIVES");
        CreatePrimaryCard(content, PrototypePrimaryWeapon.RapidBullets,
            "Rapid Bullets", "High fire rate with low damage per shot.");
        CreatePrimaryCard(content, PrototypePrimaryWeapon.PiercingLaser,
            "Piercing Laser", "Hold fire for a continuous beam that pierces multiple slimes.");
        CreatePrimaryCard(content, PrototypePrimaryWeapon.ShotgunBurst,
            "Shotgun Burst", "Wide seven-pellet spread; strongest at close range.");
        CreatePrimaryCard(content, PrototypePrimaryWeapon.Flamethrower,
            "Flamethrower", "Continuous cone damage that applies burning damage over time.");
        CreatePrimaryCard(content, PrototypePrimaryWeapon.LightningArc,
            "Lightning Arc", "Targets your aim and jumps through up to five nearby slimes.");
        CreatePrimaryCard(content, PrototypePrimaryWeapon.ToxicSpitter,
            "Toxic Spitter", "Lobs poison bombs with splash damage and a poison effect.");

        CreateSectionHeading(content, "SECONDARY ATTACHMENTS — STACKABLE");
        CreateAttachmentCard(content, PrototypeAttachment.MiniTurretSatellites,
            "Mini-Turret Satellites", "Adds an orbiting automatic shooter per level.");
        CreateAttachmentCard(content, PrototypeAttachment.SpikeBarrier,
            "Spike Barrier", "Damages slimes touching the tower every half second.");
        CreateAttachmentCard(content, PrototypeAttachment.OverchargeModule,
            "Overcharge Module", "Press R for a temporary fire-rate boost. Levels increase its strength and duration.");
        CreateAttachmentCard(content, PrototypeAttachment.FreezeBurst,
            "Freeze Burst", "Automatically freezes nearby slimes every 10 seconds.");
        CreateAttachmentCard(content, PrototypeAttachment.ReflectorDome,
            "Reflector Dome", "Adds a stacking chance for attacks to bounce to another slime.");

        armoryPanel.SetActive(false);
    }

    private void CreateHeader(RectTransform panel)
    {
        GameObject header = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        RectTransform rect = header.GetComponent<RectTransform>();
        rect.SetParent(panel, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 92f);
        header.GetComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.13f, 0.19f, 1f);

        TextMeshProUGUI title = CreateText(header.transform, "ARMORY", 28f, TextAlignmentOptions.Center, Color.white);
        title.fontStyle = FontStyles.Bold;
        Stretch(title.rectTransform, 220f, 150f, 28f, 5f);

        resetButton = CreateButton(header.transform, "RESET + REFUND", new Color(0.42f, 0.27f, 0.12f, 1f));
        RectTransform resetRect = resetButton.transform as RectTransform;
        resetRect.anchorMin = new Vector2(0f, 0.5f);
        resetRect.anchorMax = new Vector2(0f, 0.5f);
        resetRect.pivot = new Vector2(0f, 0.5f);
        resetRect.anchoredPosition = new Vector2(12f, 0f);
        resetRect.sizeDelta = new Vector2(196f, 50f);
        resetButtonLabel = resetButton.GetComponentInChildren<TextMeshProUGUI>();
        resetButton.onClick.AddListener(HandleResetClicked);

        UnityEngine.UI.Button close = CreateButton(header.transform, "CLOSE", new Color(0.65f, 0.16f, 0.18f, 1f));
        RectTransform closeRect = close.transform as RectTransform;
        closeRect.anchorMin = new Vector2(1f, 0.5f);
        closeRect.anchorMax = new Vector2(1f, 0.5f);
        closeRect.pivot = new Vector2(1f, 0.5f);
        closeRect.anchoredPosition = new Vector2(-12f, 0f);
        closeRect.sizeDelta = new Vector2(128f, 46f);
        close.onClick.AddListener(HandleCloseClicked);

        armoryFeedbackLabel = CreateText(header.transform, "Progress auto-saves", 15f,
            TextAlignmentOptions.Center, new Color(0.64f, 0.78f, 0.92f));
        armoryFeedbackLabel.enableAutoSizing = true;
        armoryFeedbackLabel.fontSizeMin = 11f;
        armoryFeedbackLabel.fontSizeMax = 15f;
        Stretch(armoryFeedbackLabel.rectTransform, 220f, 150f, 5f, 57f);
    }

    private RectTransform CreateScrollView(RectTransform panel)
    {
        GameObject scrollObject = new GameObject("Scroll View", typeof(RectTransform), typeof(UnityEngine.UI.ScrollRect));
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.SetParent(panel, false);
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(12f, 12f);
        scrollRectTransform.offsetMax = new Vector2(-12f, -102f);

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Mask));
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        viewport.SetParent(scrollRectTransform, false);
        Stretch(viewport, 0f, 0f, 0f, 0f);
        UnityEngine.UI.Image viewportImage = viewportObject.GetComponent<UnityEngine.UI.Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewportObject.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;

        GameObject contentObject = new GameObject("Content", typeof(RectTransform),
            typeof(UnityEngine.UI.VerticalLayoutGroup), typeof(UnityEngine.UI.ContentSizeFitter));
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        UnityEngine.UI.VerticalLayoutGroup layout = contentObject.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 18);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        UnityEngine.UI.ContentSizeFitter fitter = contentObject.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        armoryScrollRect = scrollObject.GetComponent<UnityEngine.UI.ScrollRect>();
        armoryScrollRect.content = content;
        armoryScrollRect.viewport = viewport;
        armoryScrollRect.horizontal = false;
        armoryScrollRect.vertical = true;
        armoryScrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        armoryScrollRect.scrollSensitivity = 42f;
        return content;
    }

    private TextMeshProUGUI CreateSectionHeading(Transform parent, string heading)
    {
        GameObject section = new GameObject(heading, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.LayoutElement));
        section.transform.SetParent(parent, false);
        section.GetComponent<UnityEngine.UI.Image>().color = new Color(0.13f, 0.2f, 0.3f, 1f);
        section.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 52f;
        TextMeshProUGUI label = CreateText(section.transform, heading, 22f, TextAlignmentOptions.Center, new Color(0.75f, 0.9f, 1f));
        Stretch(label.rectTransform, 8f, 8f, 6f, 6f);
        return label;
    }

    private TextMeshProUGUI CreateStatsCard(Transform parent)
    {
        GameObject card = new GameObject("Active Weapon Stats", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.LayoutElement));
        card.transform.SetParent(parent, false);
        card.GetComponent<UnityEngine.UI.Image>().color = new Color(0.07f, 0.14f, 0.2f, 1f);
        card.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 112f;
        TextMeshProUGUI label = CreateText(card.transform, string.Empty, 18f, TextAlignmentOptions.Center,
            new Color(0.82f, 0.94f, 1f));
        label.textWrappingMode = TextWrappingModes.Normal;
        Stretch(label.rectTransform, 16f, 16f, 8f, 8f);
        return label;
    }

    private void CreatePrimaryCard(Transform parent, PrototypePrimaryWeapon weapon, string title, string description)
    {
        UnityEngine.UI.Button button;
        TextMeshProUGUI buttonLabel;
        TextMeshProUGUI titleLabel;
        TextMeshProUGUI descriptionLabel;
        GameObject card = CreateCard(parent, title, description, out button, out buttonLabel,
            out titleLabel, out descriptionLabel);
        PrototypePrimaryWeapon captured = weapon;
        button.onClick.AddListener(() => HandlePrimaryClicked(captured));
        primaryCards[weapon] = card;
        primaryButtons[weapon] = button;
        primaryButtonLabels[weapon] = buttonLabel;
    }

    private void CreateWeaponUpgradeCard(Transform parent, PrototypeWeaponUpgrade upgrade)
    {
        UnityEngine.UI.Button button;
        TextMeshProUGUI buttonLabel;
        TextMeshProUGUI titleLabel;
        TextMeshProUGUI descriptionLabel;
        CreateCard(parent, upgrade.ToString(), string.Empty, out button, out buttonLabel,
            out titleLabel, out descriptionLabel);
        PrototypeWeaponUpgrade captured = upgrade;
        button.onClick.AddListener(() => HandleWeaponUpgradeClicked(captured));
        weaponUpgradeButtons[upgrade] = button;
        weaponUpgradeButtonLabels[upgrade] = buttonLabel;
        weaponUpgradeTitles[upgrade] = titleLabel;
        weaponUpgradeDescriptions[upgrade] = descriptionLabel;
    }

    private void CreateAttachmentCard(Transform parent, PrototypeAttachment attachment, string title, string description)
    {
        UnityEngine.UI.Button button;
        TextMeshProUGUI buttonLabel;
        TextMeshProUGUI titleLabel;
        TextMeshProUGUI descriptionLabel;
        CreateCard(parent, title, description, out button, out buttonLabel,
            out titleLabel, out descriptionLabel);
        PrototypeAttachment captured = attachment;
        button.onClick.AddListener(() => HandleAttachmentClicked(captured));
        attachmentButtons[attachment] = button;
        attachmentButtonLabels[attachment] = buttonLabel;
        attachmentDescriptions[attachment] = descriptionLabel;
        attachmentBaseDescriptions[attachment] = description;
    }

    private static GameObject CreateCard(Transform parent, string title, string description,
        out UnityEngine.UI.Button button, out TextMeshProUGUI buttonLabel,
        out TextMeshProUGUI titleText, out TextMeshProUGUI descriptionText)
    {
        GameObject card = new GameObject(title, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.LayoutElement), typeof(UnityEngine.UI.HorizontalLayoutGroup));
        card.transform.SetParent(parent, false);
        card.GetComponent<UnityEngine.UI.Image>().color = new Color(0.095f, 0.11f, 0.15f, 1f);
        card.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 148f;

        UnityEngine.UI.HorizontalLayoutGroup horizontal = card.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        horizontal.padding = new RectOffset(18, 14, 12, 12);
        horizontal.spacing = 16f;
        horizontal.childAlignment = TextAnchor.MiddleLeft;
        horizontal.childControlWidth = true;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = true;

        GameObject copy = new GameObject("Description", typeof(RectTransform), typeof(UnityEngine.UI.LayoutElement),
            typeof(UnityEngine.UI.VerticalLayoutGroup));
        copy.transform.SetParent(card.transform, false);
        UnityEngine.UI.LayoutElement copyLayout = copy.GetComponent<UnityEngine.UI.LayoutElement>();
        copyLayout.flexibleWidth = 1f;
        UnityEngine.UI.VerticalLayoutGroup vertical = copy.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vertical.spacing = 4f;
        vertical.childControlHeight = true;
        vertical.childControlWidth = true;
        vertical.childForceExpandHeight = false;
        vertical.childForceExpandWidth = true;

        titleText = CreateText(copy.transform, title, 23f, TextAlignmentOptions.Left, Color.white);
        titleText.fontStyle = FontStyles.Bold;
        titleText.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 38f;
        descriptionText = CreateText(copy.transform, description, 17f, TextAlignmentOptions.TopLeft,
            new Color(0.78f, 0.82f, 0.9f));
        descriptionText.textWrappingMode = TextWrappingModes.Normal;
        descriptionText.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 78f;

        button = CreateButton(card.transform, "BUY", new Color(0.12f, 0.48f, 0.38f, 1f));
        UnityEngine.UI.LayoutElement buttonLayout = button.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        buttonLayout.preferredWidth = 190f;
        buttonLayout.minWidth = 190f;
        buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>();
        return card;
    }

    private static UnityEngine.UI.Button CreateButton(Transform parent, string label, Color color)
    {
        GameObject buttonObject = new GameObject(label + " Button", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        buttonObject.transform.SetParent(parent, false);
        UnityEngine.UI.Image image = buttonObject.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        UnityEngine.UI.Button button = buttonObject.GetComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        buttonObject.AddComponent<ButtonAudioHover>().SuppressClickSound();

        TextMeshProUGUI text = CreateText(buttonObject.transform, label, 20f, TextAlignmentOptions.Center, Color.white);
        text.fontStyle = FontStyles.Bold;
        Stretch(text.rectTransform, 6f, 6f, 4f, 4f);
        return button;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string value, float fontSize,
        TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private void HandlePrimaryClicked(PrototypePrimaryWeapon weapon)
    {
        bool alreadyOwned = combat.OwnsPrimary(weapon);
        if (combat.BuyOrEquipPrimary(weapon))
        {
            string action = alreadyOwned ? "Equipped " : "Purchased and equipped ";
            ShowFeedback(action + PrototypeArmoryCombat.GetWeaponDisplayName(weapon) + ".", true);
        }
        else
        {
            ShowFeedback("Not enough money to buy " + PrototypeArmoryCombat.GetWeaponDisplayName(weapon) + ".", false);
        }
    }

    private void HandleWeaponUpgradeClicked(PrototypeWeaponUpgrade upgrade)
    {
        string upgradeName = PrototypeArmoryCombat.GetWeaponUpgradeName(combat.ActivePrimary, upgrade);
        if (combat.BuyWeaponUpgrade(upgrade))
            ShowFeedback(upgradeName + " upgraded.", true);
        else
            ShowFeedback("Not enough money for " + upgradeName + ".", false);
    }

    private void HandleAttachmentClicked(PrototypeAttachment attachment)
    {
        if (combat.BuyAttachment(attachment))
            ShowFeedback(FormatEnumName(attachment) + " upgraded.", true);
        else
            ShowFeedback("Not enough money for " + FormatEnumName(attachment) + ".", false);
    }

    private void HandleResetClicked()
    {
        if (!resetConfirmationPending || Time.unscaledTime > resetConfirmationUntil)
        {
            resetConfirmationPending = true;
            resetConfirmationUntil = Time.unscaledTime + 3f;
            if (resetButtonLabel != null)
                resetButtonLabel.text = "CONFIRM RESET";
            ShowFeedback("Click reset again within 3 seconds to clear and refund Armory progress.", false, 3f);
            BackgroundMusic.ButtonClick();
            return;
        }

        int refund = combat.ResetProgressAndRefund();
        CancelResetConfirmation();
        ShowFeedback("Armory reset. Refunded $" + refund + ".", true, 3.5f);
    }

    private void CancelResetConfirmation()
    {
        resetConfirmationPending = false;
        resetConfirmationUntil = 0f;
        if (resetButtonLabel != null)
            resetButtonLabel.text = "RESET + REFUND";
    }

    private void HandleCloseClicked()
    {
        BackgroundMusic.ButtonClick();
        CancelResetConfirmation();
        CloseArmory();
    }

    private void ShowFeedback(string message, bool success, float duration = 2.4f)
    {
        if (armoryFeedbackLabel == null)
            return;

        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);
        armoryFeedbackLabel.text = message;
        armoryFeedbackLabel.color = success
            ? new Color(0.42f, 1f, 0.68f)
            : new Color(1f, 0.58f, 0.5f);
        feedbackRoutine = StartCoroutine(ClearFeedbackAfterDelay(duration));
    }

    private IEnumerator ClearFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        feedbackRoutine = null;
        if (armoryFeedbackLabel != null)
        {
            armoryFeedbackLabel.text = "Progress auto-saves";
            armoryFeedbackLabel.color = new Color(0.64f, 0.78f, 0.92f);
        }
    }

    private static string FormatEnumName(PrototypeAttachment attachment)
    {
        switch (attachment)
        {
            case PrototypeAttachment.MiniTurretSatellites: return "Mini-Turret Satellites";
            case PrototypeAttachment.SpikeBarrier: return "Spike Barrier";
            case PrototypeAttachment.OverchargeModule: return "Overcharge Module";
            case PrototypeAttachment.FreezeBurst: return "Freeze Burst";
            default: return "Reflector Dome";
        }
    }

    private void RefreshButtons()
    {
        if (combat == null) return;

        foreach (PrototypePrimaryWeapon weapon in Enum.GetValues(typeof(PrototypePrimaryWeapon)))
        {
            if (!primaryButtonLabels.TryGetValue(weapon, out TextMeshProUGUI label)) continue;
            bool isActive = combat.ActivePrimary == weapon;
            if (primaryCards.TryGetValue(weapon, out GameObject card))
                card.SetActive(!isActive);

            if (combat.OwnsPrimary(weapon))
            {
                label.text = "OWNED\nEQUIP";
                primaryButtons[weapon].interactable = true;
                SetButtonColor(primaryButtons[weapon], new Color(0.12f, 0.34f, 0.62f, 1f));
            }
            else
            {
                label.text = "$" + combat.GetPrimaryCost(weapon);
                primaryButtons[weapon].interactable = true;
                SetButtonColor(primaryButtons[weapon], new Color(0.12f, 0.48f, 0.38f, 1f));
            }
        }

        PrototypePrimaryWeapon activeWeapon = combat.ActivePrimary;
        string activeName = PrototypeArmoryCombat.GetWeaponDisplayName(activeWeapon);
        if (weaponUpgradeHeading != null)
            weaponUpgradeHeading.text = activeName.ToUpperInvariant() + " — WEAPON UPGRADES";
        if (weaponStatsLabel != null)
            weaponStatsLabel.text = combat.GetActiveWeaponStatsText();

        foreach (PrototypeWeaponUpgrade upgrade in Enum.GetValues(typeof(PrototypeWeaponUpgrade)))
        {
            if (!weaponUpgradeButtonLabels.TryGetValue(upgrade, out TextMeshProUGUI buttonLabel))
                continue;

            int level = combat.GetWeaponUpgradeLevel(activeWeapon, upgrade);
            int max = PrototypeArmoryCombat.GetWeaponUpgradeMaxLevel(upgrade);
            weaponUpgradeTitles[upgrade].text = PrototypeArmoryCombat.GetWeaponUpgradeName(activeWeapon, upgrade);
            weaponUpgradeDescriptions[upgrade].text =
                PrototypeArmoryCombat.GetWeaponUpgradeDescription(activeWeapon, upgrade) +
                "\n<color=#6DE6A0><b>NEXT:</b> " + combat.GetWeaponUpgradePreviewText(activeWeapon, upgrade) + "</color>";

            if (level >= max)
            {
                buttonLabel.text = "MAX\nLVL " + level;
                weaponUpgradeButtons[upgrade].interactable = false;
                SetButtonColor(weaponUpgradeButtons[upgrade], new Color(0.28f, 0.31f, 0.36f, 1f));
            }
            else
            {
                int cost = PrototypeArmoryCombat.GetWeaponUpgradeCost(activeWeapon, upgrade, level);
                buttonLabel.text = "LVL " + level + "/" + max + "\n$" + cost;
                weaponUpgradeButtons[upgrade].interactable = true;
                SetButtonColor(weaponUpgradeButtons[upgrade], new Color(0.12f, 0.48f, 0.38f, 1f));
            }
        }

        foreach (PrototypeAttachment attachment in Enum.GetValues(typeof(PrototypeAttachment)))
        {
            if (!attachmentButtonLabels.TryGetValue(attachment, out TextMeshProUGUI label)) continue;
            int level = combat.GetAttachmentLevel(attachment);
            int max = PrototypeArmoryCombat.GetAttachmentMaxLevel(attachment);
            if (attachmentDescriptions.TryGetValue(attachment, out TextMeshProUGUI description) &&
                attachmentBaseDescriptions.TryGetValue(attachment, out string baseDescription))
            {
                description.text = baseDescription + "\n<color=#6DE6A0><b>NEXT:</b> " +
                                   combat.GetAttachmentUpgradePreviewText(attachment) + "</color>";
            }
            if (level >= max)
            {
                label.text = "MAX\nLVL " + level;
                attachmentButtons[attachment].interactable = false;
                SetButtonColor(attachmentButtons[attachment], new Color(0.28f, 0.31f, 0.36f, 1f));
            }
            else
            {
                label.text = "LVL " + level + "/" + max + "\n$" + combat.GetAttachmentCost(attachment, level);
                attachmentButtons[attachment].interactable = true;
                SetButtonColor(attachmentButtons[attachment], new Color(0.12f, 0.48f, 0.38f, 1f));
            }
        }
    }

    private static void SetButtonColor(UnityEngine.UI.Button button, Color color)
    {
        if (button != null && button.targetGraphic is UnityEngine.UI.Image image)
            image.color = color;
    }

    private void HandleArmoryTab(bool isOn)
    {
        if (isOn)
            ShowTab(ShopTab.Armory);
    }

    private void HandleUpgradesTab(bool isOn)
    {
        if (isOn)
            ShowTab(ShopTab.Upgrades);
    }

    private void HandleGrenadesTab(bool isOn)
    {
        if (isOn)
            ShowTab(ShopTab.Grenades);
    }

    private void ShowTab(ShopTab selectedTab)
    {
        if (upgradesPanel != null)
            upgradesPanel.SetActive(selectedTab == ShopTab.Upgrades);
        if (grenadesPanel != null)
            grenadesPanel.SetActive(selectedTab == ShopTab.Grenades);
        if (armoryPanel != null)
            armoryPanel.SetActive(selectedTab == ShopTab.Armory);

        SetTabVisual(upgradesTab, selectedTab == ShopTab.Upgrades);
        SetTabVisual(grenadesTab, selectedTab == ShopTab.Grenades);
        SetTabVisual(armoryTab, selectedTab == ShopTab.Armory);

        if (selectedTab == ShopTab.Armory)
        {
            // Permanent skill-tree purchases can change weapon stats while the
            // Armory tab is closed, so always rebuild its live values on entry.
            RefreshButtons();
            Canvas.ForceUpdateCanvases();
            if (armoryScrollRect != null)
            {
                armoryScrollRect.StopMovement();
                armoryScrollRect.verticalNormalizedPosition = 1f;
            }
        }
    }

    private static void SetTabVisual(UnityEngine.UI.Toggle toggle, bool selected)
    {
        if (toggle == null)
            return;

        if (toggle.targetGraphic is UnityEngine.UI.Image background)
            background.color = selected
                ? new Color(0.12f, 0.35f, 0.62f, 1f)
                : new Color(0.82f, 0.85f, 0.9f, 1f);

        TextMeshProUGUI label = toggle.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            label.color = selected ? Color.white : new Color(0.08f, 0.1f, 0.14f, 1f);
    }

    private void CloseArmory()
    {
        if (upgradesTab != null)
            upgradesTab.isOn = true;
    }

    private static void CopyRect(RectTransform source, RectTransform target)
    {
        if (source == null || target == null) return;
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localScale = source.localScale;
        target.localRotation = source.localRotation;
    }

    private static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private void OnDestroy()
    {
        if (combat != null) combat.StateChanged -= RefreshButtons;
        if (upgradesTab != null) upgradesTab.onValueChanged.RemoveListener(HandleUpgradesTab);
        if (grenadesTab != null) grenadesTab.onValueChanged.RemoveListener(HandleGrenadesTab);
        if (armoryTab != null) armoryTab.onValueChanged.RemoveListener(HandleArmoryTab);
    }
}
