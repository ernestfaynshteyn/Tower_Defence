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
    [Tooltip("Scene-authored Armory window. Use Tools/Tower Defence/Create Missing Armory Panel if this is missing.")]
    [SerializeField] private GameObject armoryPanel;

    [Header("Armory Readability")]
    [SerializeField, Min(160f)] private float cardHeight = 220f;
    [SerializeField, Range(20f, 48f)] private float cardTitleFontSize = 32f;
    [SerializeField, Range(16f, 40f)] private float cardDescriptionFontSize = 24f;
    [SerializeField, Min(180f)] private float purchaseButtonWidth = 240f;
    [SerializeField, Range(18f, 40f)] private float purchaseButtonFontSize = 27f;

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
    private readonly Dictionary<PrototypeAttachment, GameObject> attachmentCards =
        new Dictionary<PrototypeAttachment, GameObject>();
    private readonly Dictionary<PrototypeWeaponUpgrade, UnityEngine.UI.Button> weaponUpgradeButtons =
        new Dictionary<PrototypeWeaponUpgrade, UnityEngine.UI.Button>();
    private readonly Dictionary<PrototypeWeaponUpgrade, TextMeshProUGUI> weaponUpgradeButtonLabels =
        new Dictionary<PrototypeWeaponUpgrade, TextMeshProUGUI>();
    private readonly Dictionary<PrototypeWeaponUpgrade, TextMeshProUGUI> weaponUpgradeTitles =
        new Dictionary<PrototypeWeaponUpgrade, TextMeshProUGUI>();
    private readonly Dictionary<PrototypeWeaponUpgrade, TextMeshProUGUI> weaponUpgradeDescriptions =
        new Dictionary<PrototypeWeaponUpgrade, TextMeshProUGUI>();
    private readonly Dictionary<PrototypeWeaponUpgrade, UnityEngine.UI.Image> weaponUpgradeCards =
        new Dictionary<PrototypeWeaponUpgrade, UnityEngine.UI.Image>();

    private const string ArmoryPanelName = "Armory Panel";
    private static readonly PrototypeWeaponUpgrade[] SignatureUpgrades =
    {
        PrototypeWeaponUpgrade.SpecialOne,
        PrototypeWeaponUpgrade.SpecialTwo,
        PrototypeWeaponUpgrade.SpecialThree
    };

    private PrototypeArmoryCombat combat;
    private TextMeshProUGUI weaponUpgradeHeading;
    private TextMeshProUGUI weaponStatsLabel;
    private UnityEngine.UI.Image weaponUpgradeHeadingBackground;
    private UnityEngine.UI.Image weaponStatsBackground;
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

        if (armoryPanel == null)
            BuildArmoryPanel();
        else if (!BindArmoryPanel())
            yield break;

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
        ConfigureTabBar(tabParent);

        ConfigureTabToggle(upgradesTab, 0, "UPGRADES");
        ConfigureTabToggle(grenadesTab, 1, "GRENADES");
        ConfigureTabToggle(armoryTab, 2, "ARMORY");
        ConfigureGrenadesPanel();
        ConfigureArmoryPanelPresentation();

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
        ConfigureTabBar(tabParent);
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

    private static void ConfigureTabBar(RectTransform tabs)
    {
        if (tabs == null)
            return;

        tabs.sizeDelta = new Vector2(1500f, 190f);
        tabs.anchoredPosition = new Vector2(tabs.anchoredPosition.x, -112f);

        UnityEngine.UI.Image barBackground = tabs.GetComponent<UnityEngine.UI.Image>();
        if (barBackground == null)
            barBackground = tabs.gameObject.AddComponent<UnityEngine.UI.Image>();
        barBackground.color = new Color(0.035f, 0.045f, 0.065f, 0.97f);
        barBackground.raycastTarget = false;
    }

    private static void ConfigureTabToggle(UnityEngine.UI.Toggle toggle, int index, string labelText)
    {
        if (toggle == null)
            return;

        toggle.gameObject.SetActive(true);
        toggle.interactable = true;
        toggle.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
        toggle.toggleTransition = UnityEngine.UI.Toggle.ToggleTransition.Fade;
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
            rect.offsetMin = new Vector2(index == 0 ? 12f : 8f, 12f);
            rect.offsetMax = new Vector2(index == 2 ? -12f : -8f, -12f);
            rect.localScale = Vector3.one;
        }

        if (toggle.targetGraphic is UnityEngine.UI.Image background)
        {
            background.raycastTarget = true;
            background.color = Color.white;
        }

        UnityEngine.UI.Image accent = EnsureTabSelectionAccent(toggle, index);
        toggle.graphic = accent;

        TextMeshProUGUI label = toggle.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.gameObject.SetActive(true);
            label.text = labelText;
            label.color = new Color(0.67f, 0.72f, 0.8f, 1f);
            label.fontSize = 32f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 32f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.margin = Vector4.zero;
            label.raycastTarget = false;

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(18f, 16f);
            labelRect.offsetMax = new Vector2(-18f, -16f);
            labelRect.localScale = Vector3.one;
        }

        SetTabVisual(toggle, toggle.isOn);
    }

    private static UnityEngine.UI.Image EnsureTabSelectionAccent(UnityEngine.UI.Toggle toggle, int index)
    {
        Transform existing = toggle.transform.Find("Selection Accent");
        UnityEngine.UI.Image accent;
        if (existing == null)
        {
            GameObject accentObject = new GameObject("Selection Accent", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            accentObject.transform.SetParent(toggle.transform, false);
            accent = accentObject.GetComponent<UnityEngine.UI.Image>();
        }
        else
        {
            accent = existing.GetComponent<UnityEngine.UI.Image>();
            if (accent == null)
                accent = existing.gameObject.AddComponent<UnityEngine.UI.Image>();
        }

        RectTransform accentRect = accent.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(1f, 0f);
        accentRect.pivot = new Vector2(0.5f, 0f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 10f);
        accent.color = GetTabAccent(index);
        accent.raycastTarget = false;
        accent.transform.SetAsLastSibling();
        return accent;
    }

    private static Color GetTabAccent(int index)
    {
        switch (index)
        {
            case 0: return new Color(0.24f, 0.58f, 1f, 1f);
            case 1: return new Color(1f, 0.48f, 0.16f, 1f);
            default: return new Color(0.18f, 0.84f, 0.82f, 1f);
        }
    }

    private void ConfigureGrenadesPanel()
    {
        if (grenadesPanel == null)
            return;

        RectTransform grenadePanelRect = grenadesPanel.transform as RectTransform;
        RectTransform referenceRect = armoryPanel != null
            ? armoryPanel.transform as RectTransform
            : upgradesPanel != null ? upgradesPanel.transform as RectTransform : null;
        if (grenadePanelRect != null && referenceRect != null)
            CopyRect(referenceRect, grenadePanelRect);

        UnityEngine.UI.ScrollRect legacyScroll = grenadesPanel.GetComponent<UnityEngine.UI.ScrollRect>();
        foreach (ZoomInOut zoom in grenadesPanel.GetComponentsInChildren<ZoomInOut>(true))
            zoom.enabled = false;
        if (legacyScroll != null)
        {
            legacyScroll.horizontal = false;
            legacyScroll.vertical = true;
            legacyScroll.scrollSensitivity = 42f;
            legacyScroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            legacyScroll.StopMovement();
        }

        Transform shopWindow = grenadesPanel.transform.Find("shop window");
        if (shopWindow == null)
            return;

        RectTransform windowRect = shopWindow as RectTransform;
        if (windowRect != null)
        {
            windowRect.anchorMin = new Vector2(0f, 1f);
            windowRect.anchorMax = Vector2.one;
            windowRect.pivot = new Vector2(0.5f, 1f);
            windowRect.anchoredPosition = Vector2.zero;
            windowRect.sizeDelta = new Vector2(0f, Mathf.Max(1800f,
                grenadePanelRect != null ? grenadePanelRect.rect.height : 1800f));
            windowRect.localScale = Vector3.one;
            windowRect.localRotation = Quaternion.identity;
            if (legacyScroll != null)
            {
                legacyScroll.content = windowRect;
                legacyScroll.viewport = grenadePanelRect;
            }
        }

        if (grenadesPanel.GetComponent<UnityEngine.UI.RectMask2D>() == null)
            grenadesPanel.AddComponent<UnityEngine.UI.RectMask2D>();

        UnityEngine.UI.Image windowImage = shopWindow.GetComponent<UnityEngine.UI.Image>();
        if (windowImage != null)
        {
            windowImage.color = new Color(0.045f, 0.055f, 0.075f, 0.985f);
            windowImage.raycastTarget = true;
        }

        TextMeshProUGUI header = EnsureGrenadeText(shopWindow, "Grenade Header");
        header.text = "THROWABLE SUPPLIES\n<size=55%><color=#9BAFC8>BUY ONE PER CLICK  |  Q TO EQUIP  |  1-4 TO SELECT</color></size>";
        header.fontSize = 42f;
        header.fontStyle = FontStyles.Bold;
        header.alignment = TextAlignmentOptions.Center;
        header.color = new Color(0.94f, 0.97f, 1f, 1f);
        header.enableAutoSizing = true;
        header.fontSizeMin = 24f;
        header.fontSizeMax = 42f;
        SetAnchoredRect(header.rectTransform, new Vector2(0.08f, 0.87f), new Vector2(0.92f, 0.98f));
        header.transform.SetAsLastSibling();

        string[] objectNames = { "frag", "Smoke", "flash", "Molotov" };
        string[] titles = { "FRAG GRENADE", "SMOKE GRENADE", "FLASHBANG", "MOLOTOV" };
        string[] descriptions =
        {
            "High-damage blast in a wide area.",
            "Creates a cloud that slows nearby enemies.",
            "Damages and briefly stuns caught enemies.",
            "Ignites an area for damage over time."
        };
        Color[] accents =
        {
            new Color(0.45f, 0.78f, 0.32f, 1f),
            new Color(0.42f, 0.72f, 0.82f, 1f),
            new Color(1f, 0.78f, 0.2f, 1f),
            new Color(1f, 0.38f, 0.12f, 1f)
        };

        for (int index = 0; index < objectNames.Length; index++)
        {
            Transform item = shopWindow.Find(objectNames[index]);
            if (item == null)
                continue;

            ConfigureGrenadeCard(item, index, titles[index], descriptions[index], accents[index]);
        }
    }

    private static void ConfigureGrenadeCard(
        Transform item,
        int index,
        string title,
        string description,
        Color accent)
    {
        string[] iconNames = { "Frag", "Smoke", "Flashbang", "Molotov" };
        RectTransform cardRect = item as RectTransform;
        if (cardRect == null)
            return;

        float anchorX = index % 2 == 0 ? 0.27f : 0.73f;
        float anchorY = index < 2 ? 0.64f : 0.28f;
        cardRect.anchorMin = new Vector2(anchorX, anchorY);
        cardRect.anchorMax = new Vector2(anchorX, anchorY);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(570f, 520f);
        cardRect.localScale = Vector3.one;
        cardRect.localRotation = Quaternion.identity;

        UnityEngine.UI.Image cardImage = item.GetComponent<UnityEngine.UI.Image>();
        if (cardImage == null)
            cardImage = item.gameObject.AddComponent<UnityEngine.UI.Image>();

        Transform iconTransform = item.Find("Icon");
        UnityEngine.UI.Image iconImage;
        if (iconTransform == null)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(UnityEngine.UI.Image));
            iconObject.layer = item.gameObject.layer;
            iconObject.transform.SetParent(item, false);
            iconTransform = iconObject.transform;
            iconImage = iconObject.GetComponent<UnityEngine.UI.Image>();
            iconImage.sprite = cardImage.sprite;
            iconImage.material = cardImage.material;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage = iconTransform.GetComponent<UnityEngine.UI.Image>();
            if (iconImage == null)
                iconImage = iconTransform.gameObject.AddComponent<UnityEngine.UI.Image>();
        }

        Sprite throwableIcon = Resources.Load<Sprite>("ThrowableIcons/" + iconNames[index]);
        if (throwableIcon != null)
            iconImage.sprite = throwableIcon;
        else if (iconImage.sprite == null && cardImage.sprite != null)
            iconImage.sprite = cardImage.sprite;
        iconImage.color = Color.white;
        iconImage.material = null;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        SetAnchoredRect(iconImage.rectTransform, new Vector2(0.14f, 0.43f), new Vector2(0.86f, 0.93f));
        iconImage.rectTransform.localScale = Vector3.one;
        iconTransform.SetAsFirstSibling();

        cardImage.sprite = null;
        cardImage.material = null;
        cardImage.preserveAspect = false;
        cardImage.color = Color.Lerp(new Color(0.075f, 0.09f, 0.125f, 1f), accent, 0.12f);
        cardImage.raycastTarget = false;

        TextMeshProUGUI titleLabel = EnsureGrenadeText(item, "Item Title");
        titleLabel.text = title;
        titleLabel.fontSize = 30f;
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.alignment = TextAlignmentOptions.Center;
        titleLabel.color = Color.Lerp(Color.white, accent, 0.38f);
        titleLabel.enableAutoSizing = true;
        titleLabel.fontSizeMin = 20f;
        titleLabel.fontSizeMax = 30f;
        SetAnchoredRect(titleLabel.rectTransform, new Vector2(0.07f, 0.31f), new Vector2(0.93f, 0.43f));

        TextMeshProUGUI descriptionLabel = EnsureGrenadeText(item, "Item Description");
        descriptionLabel.text = description;
        descriptionLabel.fontSize = 21f;
        descriptionLabel.alignment = TextAlignmentOptions.Center;
        descriptionLabel.color = new Color(0.7f, 0.76f, 0.84f, 1f);
        descriptionLabel.enableAutoSizing = true;
        descriptionLabel.fontSizeMin = 15f;
        descriptionLabel.fontSizeMax = 21f;
        descriptionLabel.textWrappingMode = TextWrappingModes.Normal;
        SetAnchoredRect(descriptionLabel.rectTransform, new Vector2(0.08f, 0.17f), new Vector2(0.92f, 0.31f));

        UnityEngine.UI.Button buyButton = item.GetComponentInChildren<UnityEngine.UI.Button>(true);
        if (buyButton == null)
            return;

        RectTransform buttonRect = buyButton.transform as RectTransform;
        if (buttonRect != null)
            SetAnchoredRect(buttonRect, new Vector2(0.25f, 0.035f), new Vector2(0.75f, 0.15f));

        if (buyButton.GetComponent<ButtonAudioHover>() == null)
            buyButton.gameObject.AddComponent<ButtonAudioHover>().SuppressClickSound();

        BuyGrenadeButton purchase = buyButton.GetComponent<BuyGrenadeButton>();
        TextMeshProUGUI buttonLabel = buyButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (buttonLabel != null)
        {
            int baseCost = purchase != null ? Mathf.Max(0, purchase.cost) : 0;
            int shownCost = Application.isPlaying ? GlobalData.ScaleUpgradePrice(baseCost) : baseCost;
            buttonLabel.text = "BUY\n$" + shownCost;
            buttonLabel.fontSize = 25f;
            buttonLabel.fontStyle = FontStyles.Bold;
            buttonLabel.alignment = TextAlignmentOptions.Center;
            buttonLabel.color = Color.white;
            buttonLabel.enableAutoSizing = true;
            buttonLabel.fontSizeMin = 17f;
            buttonLabel.fontSizeMax = 25f;
            buttonLabel.raycastTarget = false;
        }

        buyButton.interactable = true;
        SetButtonColor(buyButton, Color.Lerp(new Color(0.08f, 0.3f, 0.28f, 1f), accent, 0.46f));
    }

    private static TextMeshProUGUI EnsureGrenadeText(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        TextMeshProUGUI text;
        if (existing == null)
        {
            GameObject textObject = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer));
            textObject.layer = parent.gameObject.layer;
            textObject.transform.SetParent(parent, false);
            text = textObject.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            text = existing.GetComponent<TextMeshProUGUI>();
            if (text == null)
                text = existing.gameObject.AddComponent<TextMeshProUGUI>();
        }

        text.raycastTarget = false;
        return text;
    }

    private static void SetAnchoredRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (rect == null)
            return;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private void ConfigureArmoryPanelPresentation()
    {
        if (armoryPanel == null)
            return;

        UnityEngine.UI.Image panelImage = armoryPanel.GetComponent<UnityEngine.UI.Image>();
        if (panelImage != null)
            panelImage.color = new Color(0.045f, 0.055f, 0.075f, 0.985f);

        Transform header = armoryPanel.transform.Find("Header");
        if (header != null)
        {
            UnityEngine.UI.Image headerImage = header.GetComponent<UnityEngine.UI.Image>();
            if (headerImage != null)
                headerImage.color = new Color(0.065f, 0.085f, 0.125f, 1f);

            TextMeshProUGUI title = FindText(header, "Title");
            if (title != null)
            {
                title.text = "ARMORY  |  LOADOUT";
                title.fontSize = 32f;
                title.fontStyle = FontStyles.Bold;
                title.color = new Color(0.88f, 0.96f, 1f, 1f);
            }

            UnityEngine.UI.Button reset = FindButton(header, "RESET + REFUND Button");
            if (reset != null)
                SetButtonColor(reset, new Color(0.48f, 0.3f, 0.12f, 1f));
            UnityEngine.UI.Button close = FindButton(header, "CLOSE Button");
            if (close != null)
                SetButtonColor(close, new Color(0.58f, 0.14f, 0.18f, 1f));
        }

        Transform content = armoryPanel.transform.Find("Scroll View/Viewport/Content");
        if (content == null)
            return;

        UnityEngine.UI.VerticalLayoutGroup layout = content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.padding = new RectOffset(14, 14, 14, 22);
            layout.spacing = 14f;
        }

        foreach (Transform child in content)
        {
            if (child.name.StartsWith("PRIMARY WEAPONS", StringComparison.OrdinalIgnoreCase))
            {
                StyleArmorySectionHeading(child, new Color(0.22f, 0.58f, 1f, 1f));
            }
            else if (child.name.StartsWith("SECONDARY ATTACHMENTS", StringComparison.OrdinalIgnoreCase))
            {
                StyleArmorySectionHeading(child, new Color(0.68f, 0.38f, 1f, 1f));
            }
        }
    }

    private static void StyleArmorySectionHeading(Transform section, Color accent)
    {
        UnityEngine.UI.Image background = section.GetComponent<UnityEngine.UI.Image>();
        if (background != null)
            background.color = Color.Lerp(new Color(0.055f, 0.07f, 0.105f, 1f), accent, 0.2f);

        TextMeshProUGUI label = FindText(section, "Heading Label");
        if (label != null)
        {
            label.color = Color.Lerp(Color.white, accent, 0.55f);
            label.fontStyle = FontStyles.Bold;
        }
    }

    /// <summary>
    /// Used by the Editor installer to turn the formerly runtime-only Armory
    /// into ordinary scene-authored uGUI that can be edited in the Inspector.
    /// </summary>
    public bool EnsureSceneAuthoredArmoryPanel()
    {
        if (armoryPanel != null)
        {
            bool changed = MatchArmoryPanelLayer();
            changed |= EnsureSignatureUpgradeCards();
            changed |= EnsureSceneAuthoredShopPresentation();
            return changed;
        }

        if (upgradesPanel == null || upgradesPanel.transform.parent == null)
        {
            Debug.LogError("The Armory panel needs the existing SkillTree panel and its window parent.", this);
            return false;
        }

        Transform existing = upgradesPanel.transform.parent.Find(ArmoryPanelName);
        if (existing == null)
            existing = upgradesPanel.transform.parent.Find("Armory Prototype Panel");

        if (existing != null)
        {
            existing.name = ArmoryPanelName;
            armoryPanel = existing.gameObject;
            MatchArmoryPanelLayer();
            EnsureSignatureUpgradeCards();
            EnsureSceneAuthoredShopPresentation();
            return true;
        }

        BuildArmoryPanel();
        EnsureSceneAuthoredShopPresentation();
        return armoryPanel != null;
    }

    private bool EnsureSceneAuthoredShopPresentation()
    {
        if (tabParent == null || upgradesTab == null || grenadesTab == null || armoryTab == null ||
            grenadesPanel == null || armoryPanel == null)
            return false;

        ConfigureTabBar(tabParent);
        ConfigureTabToggle(upgradesTab, 0, "UPGRADES");
        ConfigureTabToggle(grenadesTab, 1, "GRENADES");
        ConfigureTabToggle(armoryTab, 2, "ARMORY");
        ConfigureGrenadesPanel();
        ConfigureArmoryPanelPresentation();
        MatchArmoryPanelLayer();
        return true;
    }

    private bool EnsureSignatureUpgradeCards()
    {
        if (armoryPanel == null)
            return false;

        Transform content = armoryPanel.transform.Find("Scroll View/Viewport/Content");
        if (content == null)
            return false;

        bool changed = false;
        Transform legacySpecial = content.Find("Special");
        Transform firstSpecial = content.Find(PrototypeWeaponUpgrade.SpecialOne.ToString());
        if (firstSpecial == null && legacySpecial != null)
        {
            legacySpecial.name = PrototypeWeaponUpgrade.SpecialOne.ToString();
            firstSpecial = legacySpecial;
            changed = true;
        }

        Transform template = firstSpecial != null ? firstSpecial : content.Find(PrototypeWeaponUpgrade.Cooling.ToString());
        if (template == null)
            return changed;

        Transform cooling = content.Find(PrototypeWeaponUpgrade.Cooling.ToString());
        int firstSibling = cooling != null ? cooling.GetSiblingIndex() + 1 : template.GetSiblingIndex();
        for (int i = 0; i < SignatureUpgrades.Length; i++)
        {
            PrototypeWeaponUpgrade upgrade = SignatureUpgrades[i];
            Transform card = content.Find(upgrade.ToString());
            if (card == null)
            {
                GameObject clone = Instantiate(template.gameObject, content, false);
                clone.name = upgrade.ToString();
                card = clone.transform;
                changed = true;
            }

            int desiredSibling = firstSibling + i;
            if (card.GetSiblingIndex() != desiredSibling)
            {
                card.SetSiblingIndex(desiredSibling);
                changed = true;
            }

            TextMeshProUGUI title = FindText(card.Find("Description"), "Title");
            TextMeshProUGUI details = FindText(card.Find("Description"), "Details");
            UnityEngine.UI.Button button = card.GetComponentInChildren<UnityEngine.UI.Button>(true);
            TextMeshProUGUI label = button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            string defaultTitle = PrototypeArmoryCombat.GetWeaponUpgradeName(
                PrototypePrimaryWeapon.RapidBullets, upgrade);
            string defaultDetails = PrototypeArmoryCombat.GetWeaponUpgradeDescription(
                PrototypePrimaryWeapon.RapidBullets, upgrade);
            if (title != null && title.text != defaultTitle)
            {
                title.text = defaultTitle;
                changed = true;
            }
            if (details != null && details.text != defaultDetails)
            {
                details.text = defaultDetails;
                changed = true;
            }
            if (label != null && label.text != "BUY ONCE")
            {
                label.text = "BUY ONCE";
                changed = true;
            }
        }

        PrototypeWeaponUpgrade[] basicWeaponUpgrades =
        {
            PrototypeWeaponUpgrade.Damage,
            PrototypeWeaponUpgrade.Range,
            PrototypeWeaponUpgrade.FireRate,
            PrototypeWeaponUpgrade.Cooling
        };
        foreach (PrototypeWeaponUpgrade basicUpgrade in basicWeaponUpgrades)
        {
            Transform basicCard = content.Find(basicUpgrade.ToString());
            if (basicCard == null)
                continue;

            DestroyImmediate(basicCard.gameObject);
            changed = true;
        }

        return changed;
    }

    private void BuildArmoryPanel()
    {
        if (armoryPanel != null)
            return;

        RectTransform referenceRect = upgradesPanel.transform as RectTransform;
        armoryPanel = new GameObject(ArmoryPanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        RectTransform panelRect = armoryPanel.GetComponent<RectTransform>();
        panelRect.SetParent(upgradesPanel.transform.parent, false);
        CopyRect(referenceRect, panelRect);

        UnityEngine.UI.Image panelImage = armoryPanel.GetComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0.045f, 0.055f, 0.075f, 0.985f);
        panelImage.raycastTarget = true;

        CreateHeader(panelRect);
        RectTransform content = CreateScrollView(panelRect);

        weaponUpgradeHeading = CreateSectionHeading(content, "ACTIVE WEAPON UPGRADES");
        weaponUpgradeHeadingBackground = weaponUpgradeHeading.transform.parent.GetComponent<UnityEngine.UI.Image>();
        weaponStatsLabel = CreateStatsCard(content);
        weaponStatsBackground = weaponStatsLabel.transform.parent.GetComponent<UnityEngine.UI.Image>();
        foreach (PrototypeWeaponUpgrade upgrade in SignatureUpgrades)
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

        MatchArmoryPanelLayer();
        armoryPanel.SetActive(false);
    }

    private bool MatchArmoryPanelLayer()
    {
        if (armoryPanel == null || upgradesPanel == null)
            return false;

        bool changed = false;
        int uiLayer = upgradesPanel.layer;
        Transform[] hierarchy = armoryPanel.GetComponentsInChildren<Transform>(true);
        foreach (Transform item in hierarchy)
        {
            if (item.gameObject.layer == uiLayer)
                continue;

            item.gameObject.layer = uiLayer;
            changed = true;
        }

        return changed;
    }

    private bool BindArmoryPanel()
    {
        primaryButtons.Clear();
        primaryButtonLabels.Clear();
        primaryCards.Clear();
        attachmentButtons.Clear();
        attachmentButtonLabels.Clear();
        attachmentDescriptions.Clear();
        attachmentBaseDescriptions.Clear();
        attachmentCards.Clear();
        weaponUpgradeButtons.Clear();
        weaponUpgradeButtonLabels.Clear();
        weaponUpgradeTitles.Clear();
        weaponUpgradeDescriptions.Clear();
        weaponUpgradeCards.Clear();

        Transform header = armoryPanel.transform.Find("Header");
        Transform scrollView = armoryPanel.transform.Find("Scroll View");
        Transform content = armoryPanel.transform.Find("Scroll View/Viewport/Content");
        if (header == null || scrollView == null || content == null)
        {
            Debug.LogError("The scene-authored Armory panel is missing Header or Scroll View content.", armoryPanel);
            return false;
        }

        resetButton = FindButton(header, "RESET + REFUND Button");
        UnityEngine.UI.Button closeButton = FindButton(header, "CLOSE Button");
        armoryFeedbackLabel = FindText(header, "Feedback");
        resetButtonLabel = resetButton != null ? resetButton.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        armoryScrollRect = scrollView.GetComponent<UnityEngine.UI.ScrollRect>();
        Transform weaponUpgradeSection = content.Find("ACTIVE WEAPON UPGRADES");
        Transform weaponStatsCard = content.Find("Active Weapon Stats");
        weaponUpgradeHeading = FindText(weaponUpgradeSection, "Heading Label");
        weaponStatsLabel = FindText(weaponStatsCard, "Stats Text");
        weaponUpgradeHeadingBackground = weaponUpgradeSection != null
            ? weaponUpgradeSection.GetComponent<UnityEngine.UI.Image>()
            : null;
        weaponStatsBackground = weaponStatsCard != null
            ? weaponStatsCard.GetComponent<UnityEngine.UI.Image>()
            : null;

        if (resetButton == null || closeButton == null || armoryFeedbackLabel == null ||
            armoryScrollRect == null || weaponUpgradeHeading == null || weaponStatsLabel == null)
        {
            Debug.LogError("The scene-authored Armory panel has missing header or summary references.", armoryPanel);
            return false;
        }

        resetButton.onClick.AddListener(HandleResetClicked);
        closeButton.onClick.AddListener(HandleCloseClicked);

        foreach (PrototypeWeaponUpgrade upgrade in SignatureUpgrades)
        {
            if (!TryBindCard(content, upgrade.ToString(), out GameObject card, out UnityEngine.UI.Button button,
                    out TextMeshProUGUI buttonLabel, out TextMeshProUGUI title, out TextMeshProUGUI description))
                return false;

            PrototypeWeaponUpgrade captured = upgrade;
            button.onClick.AddListener(() => HandleWeaponUpgradeClicked(captured));
            weaponUpgradeButtons[upgrade] = button;
            weaponUpgradeButtonLabels[upgrade] = buttonLabel;
            weaponUpgradeTitles[upgrade] = title;
            weaponUpgradeDescriptions[upgrade] = description;
            weaponUpgradeCards[upgrade] = card.GetComponent<UnityEngine.UI.Image>();
        }

        foreach (PrototypePrimaryWeapon weapon in Enum.GetValues(typeof(PrototypePrimaryWeapon)))
        {
            string cardName = PrototypeArmoryCombat.GetWeaponDisplayName(weapon);
            if (!TryBindCard(content, cardName, out GameObject card, out UnityEngine.UI.Button button,
                    out TextMeshProUGUI buttonLabel, out _, out _))
                return false;

            PrototypePrimaryWeapon captured = weapon;
            button.onClick.AddListener(() => HandlePrimaryClicked(captured));
            primaryCards[weapon] = card;
            primaryButtons[weapon] = button;
            primaryButtonLabels[weapon] = buttonLabel;
        }

        foreach (PrototypeAttachment attachment in Enum.GetValues(typeof(PrototypeAttachment)))
        {
            string cardName = FormatEnumName(attachment);
            if (!TryBindCard(content, cardName, out GameObject card, out UnityEngine.UI.Button button,
                    out TextMeshProUGUI buttonLabel, out _, out TextMeshProUGUI description))
                return false;

            PrototypeAttachment captured = attachment;
            button.onClick.AddListener(() => HandleAttachmentClicked(captured));
            attachmentButtons[attachment] = button;
            attachmentButtonLabels[attachment] = buttonLabel;
            attachmentDescriptions[attachment] = description;
            attachmentBaseDescriptions[attachment] = GetAttachmentBaseDescription(attachment);
            attachmentCards[attachment] = card;
        }

        return true;
    }

    private static bool TryBindCard(
        Transform content,
        string cardName,
        out GameObject card,
        out UnityEngine.UI.Button button,
        out TextMeshProUGUI buttonLabel,
        out TextMeshProUGUI title,
        out TextMeshProUGUI description)
    {
        Transform cardTransform = content != null ? content.Find(cardName) : null;
        Transform copy = cardTransform != null ? cardTransform.Find("Description") : null;
        card = cardTransform != null ? cardTransform.gameObject : null;
        button = cardTransform != null ? cardTransform.GetComponentInChildren<UnityEngine.UI.Button>(true) : null;
        buttonLabel = button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        title = FindText(copy, "Title");
        description = FindText(copy, "Details");

        if (card != null && button != null && buttonLabel != null && title != null && description != null)
            return true;

        Debug.LogError("Armory card '" + cardName + "' is missing one or more editable UI children.", card);
        return false;
    }

    private static UnityEngine.UI.Button FindButton(Transform parent, string childName)
    {
        Transform child = parent != null ? parent.Find(childName) : null;
        return child != null ? child.GetComponent<UnityEngine.UI.Button>() : null;
    }

    private static TextMeshProUGUI FindText(Transform parent, string childName)
    {
        Transform child = parent != null ? parent.Find(childName) : null;
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
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
        title.gameObject.name = "Title";
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
        armoryFeedbackLabel.gameObject.name = "Feedback";
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
        label.gameObject.name = "Heading Label";
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
        label.gameObject.name = "Stats Text";
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
        GameObject card = CreateCard(parent, upgrade.ToString(), string.Empty, out button, out buttonLabel,
            out titleLabel, out descriptionLabel);
        PrototypeWeaponUpgrade captured = upgrade;
        button.onClick.AddListener(() => HandleWeaponUpgradeClicked(captured));
        weaponUpgradeButtons[upgrade] = button;
        weaponUpgradeButtonLabels[upgrade] = buttonLabel;
        weaponUpgradeTitles[upgrade] = titleLabel;
        weaponUpgradeDescriptions[upgrade] = descriptionLabel;
        weaponUpgradeCards[upgrade] = card.GetComponent<UnityEngine.UI.Image>();
    }

    private void CreateAttachmentCard(Transform parent, PrototypeAttachment attachment, string title, string description)
    {
        UnityEngine.UI.Button button;
        TextMeshProUGUI buttonLabel;
        TextMeshProUGUI titleLabel;
        TextMeshProUGUI descriptionLabel;
        GameObject card = CreateCard(parent, title, description, out button, out buttonLabel,
            out titleLabel, out descriptionLabel);
        PrototypeAttachment captured = attachment;
        button.onClick.AddListener(() => HandleAttachmentClicked(captured));
        attachmentButtons[attachment] = button;
        attachmentButtonLabels[attachment] = buttonLabel;
        attachmentDescriptions[attachment] = descriptionLabel;
        attachmentBaseDescriptions[attachment] = description;
        attachmentCards[attachment] = card;
    }

    private GameObject CreateCard(Transform parent, string title, string description,
        out UnityEngine.UI.Button button, out TextMeshProUGUI buttonLabel,
        out TextMeshProUGUI titleText, out TextMeshProUGUI descriptionText)
    {
        GameObject card = new GameObject(title, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.LayoutElement), typeof(UnityEngine.UI.HorizontalLayoutGroup));
        card.transform.SetParent(parent, false);
        card.GetComponent<UnityEngine.UI.Image>().color = new Color(0.095f, 0.11f, 0.15f, 1f);
        card.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = cardHeight;

        UnityEngine.UI.HorizontalLayoutGroup horizontal = card.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        horizontal.padding = new RectOffset(24, 20, 16, 16);
        horizontal.spacing = 22f;
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
        vertical.spacing = 8f;
        vertical.childControlHeight = true;
        vertical.childControlWidth = true;
        vertical.childForceExpandHeight = false;
        vertical.childForceExpandWidth = true;

        titleText = CreateText(copy.transform, title, cardTitleFontSize, TextAlignmentOptions.Left, Color.white);
        titleText.gameObject.name = "Title";
        titleText.fontStyle = FontStyles.Bold;
        titleText.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 52f;
        descriptionText = CreateText(copy.transform, description, cardDescriptionFontSize, TextAlignmentOptions.TopLeft,
            new Color(0.78f, 0.82f, 0.9f));
        descriptionText.gameObject.name = "Details";
        descriptionText.textWrappingMode = TextWrappingModes.Normal;
        descriptionText.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 128f;

        button = CreateButton(card.transform, "BUY", new Color(0.12f, 0.48f, 0.38f, 1f), purchaseButtonFontSize);
        UnityEngine.UI.LayoutElement buttonLayout = button.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        buttonLayout.preferredWidth = purchaseButtonWidth;
        buttonLayout.minWidth = purchaseButtonWidth;
        buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>();
        return card;
    }

    private static UnityEngine.UI.Button CreateButton(Transform parent, string label, Color color, float fontSize = 20f)
    {
        GameObject buttonObject = new GameObject(label + " Button", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        buttonObject.transform.SetParent(parent, false);
        UnityEngine.UI.Image image = buttonObject.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        UnityEngine.UI.Button button = buttonObject.GetComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        buttonObject.AddComponent<ButtonAudioHover>().SuppressClickSound();

        TextMeshProUGUI text = CreateText(buttonObject.transform, label, fontSize, TextAlignmentOptions.Center, Color.white);
        text.gameObject.name = "Label";
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
        {
            string action = PrototypeArmoryCombat.GetWeaponUpgradeMaxLevel(upgrade) == 1
                ? " unlocked."
                : " upgraded.";
            ShowFeedback(upgradeName + action, true);
        }
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

    private static string GetAttachmentBaseDescription(PrototypeAttachment attachment)
    {
        switch (attachment)
        {
            case PrototypeAttachment.MiniTurretSatellites:
                return "Adds an orbiting automatic shooter per level.";
            case PrototypeAttachment.SpikeBarrier:
                return "Damages slimes touching the tower every half second.";
            case PrototypeAttachment.OverchargeModule:
                return "Press R for a temporary fire-rate boost. Levels increase its strength and duration.";
            case PrototypeAttachment.FreezeBurst:
                return "Automatically freezes nearby slimes every 10 seconds.";
            default:
                return "Adds a stacking chance for attacks to bounce to another slime.";
        }
    }

    private void RefreshButtons()
    {
        if (combat == null) return;

        foreach (PrototypePrimaryWeapon weapon in Enum.GetValues(typeof(PrototypePrimaryWeapon)))
        {
            if (!primaryButtonLabels.TryGetValue(weapon, out TextMeshProUGUI label)) continue;
            bool isActive = combat.ActivePrimary == weapon;
            Color accent = GetWeaponAccent(weapon);
            if (primaryCards.TryGetValue(weapon, out GameObject card))
            {
                card.SetActive(!isActive);
                UnityEngine.UI.Image cardImage = card.GetComponent<UnityEngine.UI.Image>();
                if (cardImage != null)
                    cardImage.color = Color.Lerp(new Color(0.07f, 0.08f, 0.105f, 1f), accent, 0.1f);
                TextMeshProUGUI cardTitle = FindText(card.transform.Find("Description"), "Title");
                if (cardTitle != null)
                    cardTitle.color = Color.Lerp(Color.white, accent, 0.38f);
            }

            if (combat.OwnsPrimary(weapon))
            {
                label.text = "OWNED\nEQUIP";
                primaryButtons[weapon].interactable = true;
                SetButtonColor(primaryButtons[weapon], Color.Lerp(new Color(0.1f, 0.28f, 0.5f, 1f), accent, 0.36f));
            }
            else
            {
                label.text = "$" + combat.GetPrimaryCost(weapon);
                primaryButtons[weapon].interactable = true;
                SetButtonColor(primaryButtons[weapon], Color.Lerp(new Color(0.08f, 0.32f, 0.3f, 1f), accent, 0.34f));
            }
        }

        PrototypePrimaryWeapon activeWeapon = combat.ActivePrimary;
        string activeName = PrototypeArmoryCombat.GetWeaponDisplayName(activeWeapon);
        Color weaponAccent = GetWeaponAccent(activeWeapon);
        int unlockedSignatureCount = 0;
        foreach (PrototypeWeaponUpgrade signatureUpgrade in SignatureUpgrades)
        {
            if (combat.GetWeaponUpgradeLevel(activeWeapon, signatureUpgrade) >=
                PrototypeArmoryCombat.GetWeaponUpgradeMaxLevel(signatureUpgrade))
                unlockedSignatureCount++;
        }

        if (weaponUpgradeHeading != null)
        {
            weaponUpgradeHeading.text = activeName.ToUpperInvariant() + "  |  SIGNATURE UPGRADES  " +
                                        unlockedSignatureCount + "/" + SignatureUpgrades.Length;
            weaponUpgradeHeading.color = Color.Lerp(Color.white, weaponAccent, 0.72f);
        }
        if (weaponUpgradeHeadingBackground != null)
            weaponUpgradeHeadingBackground.color = Color.Lerp(new Color(0.055f, 0.075f, 0.11f, 1f), weaponAccent, 0.2f);
        if (weaponStatsLabel != null)
        {
            weaponStatsLabel.text = combat.GetActiveWeaponStatsText();
            weaponStatsLabel.color = Color.Lerp(new Color(0.82f, 0.9f, 1f, 1f), weaponAccent, 0.28f);
        }
        if (weaponStatsBackground != null)
            weaponStatsBackground.color = Color.Lerp(new Color(0.05f, 0.075f, 0.105f, 1f), weaponAccent, 0.12f);

        for (int signatureIndex = 0; signatureIndex < SignatureUpgrades.Length; signatureIndex++)
        {
            PrototypeWeaponUpgrade upgrade = SignatureUpgrades[signatureIndex];
            if (!weaponUpgradeButtonLabels.TryGetValue(upgrade, out TextMeshProUGUI buttonLabel))
                continue;

            int level = combat.GetWeaponUpgradeLevel(activeWeapon, upgrade);
            int max = PrototypeArmoryCombat.GetWeaponUpgradeMaxLevel(upgrade);
            bool isUnlocked = level >= max;
            string accentHex = ColorUtility.ToHtmlStringRGB(weaponAccent);
            string signatureNumber = signatureIndex == 0 ? "I" : signatureIndex == 1 ? "II" : "III";
            weaponUpgradeTitles[upgrade].text = "SIGNATURE " + signatureNumber + "  |  " +
                                                PrototypeArmoryCombat.GetWeaponUpgradeName(activeWeapon, upgrade);
            weaponUpgradeTitles[upgrade].color = Color.Lerp(Color.white, weaponAccent, 0.44f);
            weaponUpgradeDescriptions[upgrade].text =
                PrototypeArmoryCombat.GetWeaponUpgradeDescription(activeWeapon, upgrade) +
                (isUnlocked
                    ? "\n<color=#" + accentHex + "><b>UNLOCKED:</b> Active for " + activeName + ".</color>"
                    : "\n<color=#" + accentHex + "><b>UPGRADE:</b> " +
                      combat.GetWeaponUpgradePreviewText(activeWeapon, upgrade) + "</color>");

            if (weaponUpgradeCards.TryGetValue(upgrade, out UnityEngine.UI.Image cardImage) && cardImage != null)
            {
                float slotTint = 0.1f + signatureIndex * 0.025f;
                cardImage.color = isUnlocked
                    ? Color.Lerp(new Color(0.055f, 0.075f, 0.09f, 1f), weaponAccent, 0.26f)
                    : Color.Lerp(new Color(0.07f, 0.08f, 0.105f, 1f), weaponAccent, slotTint);
            }

            if (isUnlocked)
            {
                buttonLabel.text = max == 1 ? "UNLOCKED" : "MAX\nLVL " + level;
                weaponUpgradeButtons[upgrade].interactable = false;
                SetButtonColor(weaponUpgradeButtons[upgrade],
                    Color.Lerp(new Color(0.18f, 0.22f, 0.27f, 1f), weaponAccent, 0.42f));
            }
            else
            {
                int cost = PrototypeArmoryCombat.GetWeaponUpgradeCost(activeWeapon, upgrade, level);
                buttonLabel.text = max == 1
                    ? "UNLOCK\n$" + cost
                    : "LVL " + level + "/" + max + "\n$" + cost;
                weaponUpgradeButtons[upgrade].interactable = true;
                SetButtonColor(weaponUpgradeButtons[upgrade],
                    Color.Lerp(new Color(0.08f, 0.32f, 0.3f, 1f), weaponAccent, 0.48f));
            }
        }

        foreach (PrototypeAttachment attachment in Enum.GetValues(typeof(PrototypeAttachment)))
        {
            if (!attachmentButtonLabels.TryGetValue(attachment, out TextMeshProUGUI label)) continue;
            int level = combat.GetAttachmentLevel(attachment);
            int max = PrototypeArmoryCombat.GetAttachmentMaxLevel(attachment);
            Color accent = GetAttachmentAccent(attachment);
            if (attachmentCards.TryGetValue(attachment, out GameObject card) && card != null)
            {
                UnityEngine.UI.Image cardImage = card.GetComponent<UnityEngine.UI.Image>();
                if (cardImage != null)
                    cardImage.color = Color.Lerp(new Color(0.07f, 0.08f, 0.105f, 1f), accent, 0.09f);
                TextMeshProUGUI cardTitle = FindText(card.transform.Find("Description"), "Title");
                if (cardTitle != null)
                    cardTitle.color = Color.Lerp(Color.white, accent, 0.34f);
            }
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
                SetButtonColor(attachmentButtons[attachment],
                    Color.Lerp(new Color(0.2f, 0.23f, 0.28f, 1f), accent, 0.24f));
            }
            else
            {
                label.text = "LVL " + level + "/" + max + "\n$" + combat.GetAttachmentCost(attachment, level);
                attachmentButtons[attachment].interactable = true;
                SetButtonColor(attachmentButtons[attachment],
                    Color.Lerp(new Color(0.08f, 0.32f, 0.3f, 1f), accent, 0.38f));
            }
        }
    }

    private static void SetButtonColor(UnityEngine.UI.Button button, Color color)
    {
        if (button == null)
            return;

        if (button.targetGraphic is UnityEngine.UI.Image image)
            image.color = Color.white;

        UnityEngine.UI.ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = color;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private static Color GetWeaponAccent(PrototypePrimaryWeapon weapon)
    {
        switch (weapon)
        {
            case PrototypePrimaryWeapon.RapidBullets:
                return new Color(1f, 0.72f, 0.2f, 1f);
            case PrototypePrimaryWeapon.PiercingLaser:
                return new Color(0.18f, 0.86f, 1f, 1f);
            case PrototypePrimaryWeapon.ShotgunBurst:
                return new Color(1f, 0.46f, 0.16f, 1f);
            case PrototypePrimaryWeapon.Flamethrower:
                return new Color(1f, 0.22f, 0.08f, 1f);
            case PrototypePrimaryWeapon.LightningArc:
                return new Color(1f, 0.88f, 0.12f, 1f);
            default:
                return new Color(0.35f, 0.92f, 0.28f, 1f);
        }
    }

    private static Color GetAttachmentAccent(PrototypeAttachment attachment)
    {
        switch (attachment)
        {
            case PrototypeAttachment.MiniTurretSatellites:
                return new Color(0.3f, 0.7f, 1f, 1f);
            case PrototypeAttachment.SpikeBarrier:
                return new Color(1f, 0.32f, 0.26f, 1f);
            case PrototypeAttachment.OverchargeModule:
                return new Color(1f, 0.58f, 0.16f, 1f);
            case PrototypeAttachment.FreezeBurst:
                return new Color(0.3f, 0.9f, 1f, 1f);
            default:
                return new Color(0.72f, 0.42f, 1f, 1f);
        }
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
        {
            foreach (UnityEngine.UI.ScrollRect treeScroll in
                     upgradesPanel.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true))
                treeScroll.scrollSensitivity = 0f;
        }
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

        int index = Mathf.Clamp(toggle.transform.GetSiblingIndex(), 0, 2);
        Color accent = GetTabAccent(index);
        Color normal = selected
            ? Color.Lerp(new Color(0.08f, 0.105f, 0.15f, 1f), accent, 0.28f)
            : new Color(0.075f, 0.09f, 0.125f, 1f);

        if (toggle.targetGraphic is UnityEngine.UI.Image background)
            background.color = Color.white;

        UnityEngine.UI.ColorBlock colors = toggle.colors;
        colors.normalColor = normal;
        colors.highlightedColor = Color.Lerp(normal, accent, selected ? 0.22f : 0.16f);
        colors.pressedColor = Color.Lerp(normal, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.055f, 0.06f, 0.075f, 0.72f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        toggle.colors = colors;

        UnityEngine.UI.Image selectionAccent = toggle.graphic as UnityEngine.UI.Image;
        if (selectionAccent != null)
            selectionAccent.color = accent;

        TextMeshProUGUI label = toggle.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            label.color = selected
                ? Color.white
                : new Color(0.67f, 0.72f, 0.8f, 1f);
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
