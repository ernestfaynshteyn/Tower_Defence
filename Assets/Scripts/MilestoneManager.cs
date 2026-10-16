using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A choice that can appear in the wave milestone menu. Future systems can
/// register mutation cores or other feature rewards without changing this UI.
/// </summary>
public sealed class MilestoneRewardOption
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public Color AccentColor { get; }
    public Action Apply { get; }

    public MilestoneRewardOption(
        string id,
        string title,
        string description,
        Color accentColor,
        Action apply)
    {
        Id = id;
        Title = title;
        Description = description;
        AccentColor = accentColor;
        Apply = apply;
    }
}

/// <summary>
/// Pauses the run every few waves and offers three rewards. WaveManager adds
/// this component automatically so both gameplay scenes receive milestones.
/// </summary>
[DisallowMultipleComponent]
public sealed class MilestoneManager : MonoBehaviour
{
    private const int ChoicesPerMilestone = 3;

    public static MilestoneManager Instance { get; private set; }

    [Header("Milestones")]
    [SerializeField, Min(1)] private int milestoneInterval = 10;

    private readonly List<MilestoneRewardOption> rewardPool = new List<MilestoneRewardOption>();
    private readonly List<MilestoneRewardOption> choiceBuffer = new List<MilestoneRewardOption>();
    private readonly List<int> availableIndices = new List<int>();

    private WaveManager waveManager;
    private GameObject canvasObject;
    private RectTransform choicesContainer;
    private TextMeshProUGUI titleLabel;
    private float timeScaleBeforeMenu = 1f;
    private int lastPresentedWave;
    private int pendingWave;
    private bool subscribed;
    private bool menuOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        BuildDefaultRewardPool();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        CloseMenu(true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Adds a choice to future milestone rolls. Intended for later systems such
    /// as mutation cores, temporary powers, or difficulty-specific rewards.
    /// </summary>
    public bool RegisterReward(MilestoneRewardOption reward)
    {
        if (reward == null || string.IsNullOrWhiteSpace(reward.Id) || reward.Apply == null)
            return false;

        for (int i = 0; i < rewardPool.Count; i++)
        {
            if (rewardPool[i].Id == reward.Id)
                return false;
        }

        rewardPool.Add(reward);
        return true;
    }

    public bool UnregisterReward(string rewardId)
    {
        if (string.IsNullOrWhiteSpace(rewardId))
            return false;

        for (int i = 0; i < rewardPool.Count; i++)
        {
            if (rewardPool[i].Id != rewardId)
                continue;

            rewardPool.RemoveAt(i);
            return true;
        }

        return false;
    }

    private void TrySubscribe()
    {
        if (subscribed)
            return;

        waveManager = GetComponent<WaveManager>();
        if (waveManager == null)
            waveManager = WaveManager.Instance;
        if (waveManager == null)
            return;

        waveManager.WaveStarted += HandleWaveStarted;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || waveManager == null)
            return;

        waveManager.WaveStarted -= HandleWaveStarted;
        subscribed = false;
    }

    private void HandleWaveStarted(int wave)
    {
        int interval = Mathf.Max(1, milestoneInterval);
        if (wave <= 0 || wave % interval != 0 || wave == lastPresentedWave)
            return;

        lastPresentedWave = wave;
        pendingWave = wave;
        ShowMilestoneChoices();
    }

    private void ShowMilestoneChoices()
    {
        BuildChoiceBuffer();
        if (choiceBuffer.Count == 0)
        {
            Debug.LogWarning("[Milestones] No valid rewards are registered.", this);
            return;
        }

        EnsureUI();
        RebuildChoiceCards();

        titleLabel.text = $"WAVE {pendingWave} MILESTONE";
        timeScaleBeforeMenu = Time.timeScale;
        Time.timeScale = 0f;
        menuOpen = true;
        canvasObject.SetActive(true);
    }

    private void BuildChoiceBuffer()
    {
        choiceBuffer.Clear();
        availableIndices.Clear();

        for (int i = 0; i < rewardPool.Count; i++)
        {
            MilestoneRewardOption reward = rewardPool[i];
            if (reward != null && reward.Apply != null)
                availableIndices.Add(i);
        }

        int choiceCount = Mathf.Min(ChoicesPerMilestone, availableIndices.Count);
        for (int i = 0; i < choiceCount; i++)
        {
            int randomSlot = UnityEngine.Random.Range(0, availableIndices.Count);
            int rewardIndex = availableIndices[randomSlot];
            availableIndices.RemoveAt(randomSlot);
            choiceBuffer.Add(rewardPool[rewardIndex]);
        }
    }

    private void SelectReward(MilestoneRewardOption reward)
    {
        if (!menuOpen || reward == null)
            return;

        try
        {
            reward.Apply?.Invoke();
            Debug.Log($"[Milestones] Selected '{reward.Title}' at wave {pendingWave}.");
        }
        finally
        {
            CloseMenu(true);
        }
    }

    private void CloseMenu(bool restoreTimeScale)
    {
        if (!menuOpen)
            return;

        menuOpen = false;
        if (canvasObject != null)
            canvasObject.SetActive(false);
        if (restoreTimeScale)
            Time.timeScale = timeScaleBeforeMenu;
    }

    private void BuildDefaultRewardPool()
    {
        RegisterPercentReward(
            "reinforced_tower",
            "Reinforced Tower",
            "+15% maximum tower health for this run.",
            new Color(0.3f, 0.95f, 0.45f),
            StatNames.Health,
            15f);
        RegisterPercentReward(
            "weapon_calibration",
            "Weapon Calibration",
            "+12% damage for every primary weapon this run.",
            new Color(1f, 0.35f, 0.2f),
            StatNames.AttackDamage,
            12f);
        RegisterPercentReward(
            "rapid_systems",
            "Rapid Systems",
            "+10% attack speed for this run.",
            new Color(1f, 0.8f, 0.2f),
            StatNames.AttackSpeed,
            10f);
        RegisterPercentReward(
            "expanded_targeting",
            "Expanded Targeting",
            "+10% weapon range for this run.",
            new Color(0.25f, 0.85f, 1f),
            StatNames.Range,
            10f);
        RegisterPercentReward(
            "shield_lattice",
            "Shield Lattice",
            "+15% defense capacity for this run.",
            new Color(0.5f, 0.65f, 1f),
            StatNames.Defence,
            15f);

        RegisterReward(new MilestoneRewardOption(
            "pay_raise",
            "Pay Raise",
            "Receive a large one-time money bonus.",
            new Color(0.55f, 1f, 0.25f),
            GrantMoneyBonus));
        RegisterReward(new MilestoneRewardOption(
            "emergency_repairs",
            "Emergency Repairs",
            "Immediately restore the tower to full health.",
            new Color(0.95f, 0.95f, 1f),
            RestoreTowerHealth));
    }

    private void RegisterPercentReward(
        string id,
        string title,
        string description,
        Color color,
        string statName,
        float percent)
    {
        RegisterReward(new MilestoneRewardOption(
            id,
            title,
            description,
            color,
            () => ApplyPercentStat(statName, percent)));
    }

    private void ApplyPercentStat(string statName, float percent)
    {
        if (PlayerStats.instance == null)
        {
            Debug.LogWarning($"[Milestones] Cannot apply '{statName}' because PlayerStats is missing.", this);
            return;
        }

        PlayerStats.instance.AddPercentModifier(statName, percent);
    }

    private void GrantMoneyBonus()
    {
        if (CurrencyManager.Instance == null)
        {
            Debug.LogWarning("[Milestones] Cannot grant money because CurrencyManager is missing.", this);
            return;
        }

        int milestoneNumber = Mathf.Max(1, pendingWave / Mathf.Max(1, milestoneInterval));
        CurrencyManager.Instance.AddMoney(400 + milestoneNumber * 100);
    }

    private void RestoreTowerHealth()
    {
        PlayerHealth player = FindFirstObjectByType<PlayerHealth>();
        if (player == null)
        {
            Debug.LogWarning("[Milestones] Cannot repair the tower because PlayerHealth is missing.", this);
            return;
        }

        player.Heal(player.health);
    }

    private void EnsureUI()
    {
        if (canvasObject != null)
            return;

        canvasObject = new GameObject(
            "Milestone Choice Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject dimmer = CreateUIObject("Dimmer", canvasObject.transform);
        SetStretch(dimmer.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        Image dimmerImage = dimmer.AddComponent<Image>();
        dimmerImage.color = new Color(0.01f, 0.015f, 0.025f, 0.88f);

        GameObject panel = CreateUIObject("Milestone Panel", dimmer.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(1450f, 680f);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.07f, 0.085f, 0.11f, 0.98f);

        titleLabel = CreateLabel("Milestone Title", panel.transform, 52f, FontStyles.Bold);
        RectTransform titleRect = titleLabel.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -42f);
        titleRect.sizeDelta = new Vector2(1250f, 72f);

        TextMeshProUGUI subtitle = CreateLabel("Milestone Subtitle", panel.transform, 27f, FontStyles.Normal);
        subtitle.text = "Choose one reward. Permanent bonuses last until this run ends.";
        subtitle.color = new Color(0.78f, 0.84f, 0.9f);
        RectTransform subtitleRect = subtitle.rectTransform;
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.anchoredPosition = new Vector2(0f, -112f);
        subtitleRect.sizeDelta = new Vector2(1250f, 52f);

        GameObject container = CreateUIObject("Reward Choices", panel.transform);
        choicesContainer = container.GetComponent<RectTransform>();
        choicesContainer.anchorMin = new Vector2(0.5f, 0.5f);
        choicesContainer.anchorMax = new Vector2(0.5f, 0.5f);
        choicesContainer.pivot = new Vector2(0.5f, 0.5f);
        choicesContainer.anchoredPosition = new Vector2(0f, -70f);
        choicesContainer.sizeDelta = new Vector2(1320f, 430f);

        canvasObject.SetActive(false);
    }

    private void RebuildChoiceCards()
    {
        for (int i = choicesContainer.childCount - 1; i >= 0; i--)
            Destroy(choicesContainer.GetChild(i).gameObject);

        float spacing = 440f;
        float startX = -spacing * (choiceBuffer.Count - 1) * 0.5f;
        for (int i = 0; i < choiceBuffer.Count; i++)
        {
            MilestoneRewardOption reward = choiceBuffer[i];
            GameObject card = CreateUIObject(reward.Title, choicesContainer);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(startX + spacing * i, 0f);
            cardRect.sizeDelta = new Vector2(400f, 390f);

            Image cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(
                reward.AccentColor.r * 0.22f,
                reward.AccentColor.g * 0.22f,
                reward.AccentColor.b * 0.22f,
                1f);

            Button button = card.AddComponent<Button>();
            button.targetGraphic = cardImage;
            MilestoneRewardOption capturedReward = reward;
            button.onClick.AddListener(() => SelectReward(capturedReward));

            GameObject accent = CreateUIObject("Accent", card.transform);
            RectTransform accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.offsetMin = new Vector2(0f, -12f);
            accentRect.offsetMax = Vector2.zero;
            Image accentImage = accent.AddComponent<Image>();
            accentImage.color = reward.AccentColor;
            accentImage.raycastTarget = false;

            TextMeshProUGUI rewardTitle = CreateLabel("Title", card.transform, 36f, FontStyles.Bold);
            rewardTitle.text = reward.Title;
            rewardTitle.color = reward.AccentColor;
            SetStretch(rewardTitle.rectTransform, new Vector2(26f, 245f), new Vector2(-26f, -42f));

            TextMeshProUGUI description = CreateLabel("Description", card.transform, 27f, FontStyles.Normal);
            description.text = reward.Description;
            description.color = Color.white;
            description.textWrappingMode = TextWrappingModes.Normal;
            SetStretch(description.rectTransform, new Vector2(32f, 78f), new Vector2(-32f, -142f));

            TextMeshProUGUI choose = CreateLabel("Choose", card.transform, 25f, FontStyles.Bold);
            choose.text = "SELECT";
            choose.color = new Color(0.9f, 0.94f, 1f);
            SetStretch(choose.rectTransform, new Vector2(26f, 22f), new Vector2(-26f, -320f));
        }
    }

    private static GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }

    private static TextMeshProUGUI CreateLabel(
        string objectName,
        Transform parent,
        float fontSize,
        FontStyles fontStyle)
    {
        GameObject labelObject = CreateUIObject(objectName, parent);
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
