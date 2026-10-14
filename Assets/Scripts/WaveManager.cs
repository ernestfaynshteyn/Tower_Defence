using System.Collections;
using System;
using UnityEngine;
using TMPro;

public class WaveManager : MonoBehaviour
{
    private const int BossEnemiesPerWave = 1;

    public static WaveManager Instance;
    public event Action<int> WaveStarted;

    [Header("Wave State")]
    public int currentWave = 1;
    public int enemyNeeded;
    public int enemySpawned;
    public int enemyleft;

    [Header("Timing")]
    public float timeBetweenWaves = 5f;
    [Min(0)] public int baseWaveReward = 200;

    [Header("Enemy Growth")]
    [Min(1.01f)] public float enemyGrowthMultiplier = 1.2f;
    [Min(1)] public int maxEnemiesPerWave = 200;

    [Header("Enemy Stat Growth")]
    [Tooltip("Enemy health is multiplied by this once for every completed wave.")]
    [Min(1f)] public float enemyHealthGrowthPerWave = 1.08f;
    [Tooltip("Enemy speed gains this percentage of its base speed per completed wave.")]
    [Min(0f)] public float enemySpeedGrowthPerWave = 0.015f;
    [Tooltip("Prevents endless speed growth from making enemies impossible to read or target.")]
    [Min(1f)] public float maximumEnemySpeedMultiplier = 1.75f;
    [Tooltip("Slow, stun, and burn resistance gained at each resistance interval.")]
    [Range(0f, 1f)] public float resistanceGrowthPerInterval = 0.05f;
    [Min(1)] public int resistanceGrowthInterval = 10;
    [Range(0f, 1f)] public float maximumScaledResistance = 0.75f;

    [Header("Boss Waves")]
    public int bossWaveInterval = 5; // every X waves
    public bool IsBossWave => currentWave % Mathf.Max(1, bossWaveInterval) == 0;

    [Header("UI")]
    public GameObject waveTitle;
    private TextMeshProUGUI waveTitleTMP;

    private bool isWaveActive;
    private bool waitingForNextWave;
    private bool rewardGiven;
    private int baseEnemyNeeded;
    private BossHealthBarUI bossHealthBarUI;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // A duplicate manager must not delete unrelated components that
            // happen to share its scene object.
            Destroy(this);
            return;
        }
        else
            Instance = this;

        bossHealthBarUI = new BossHealthBarUI(transform);

        // Keep the milestone feature usable in both the original and Armory
        // gameplay scenes without requiring duplicate scene wiring.
        if (GetComponent<MilestoneManager>() == null)
            gameObject.AddComponent<MilestoneManager>();
    }

    private void OnDestroy()
    {
        GameRecords.Save();
        bossHealthBarUI?.Dispose();

        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        currentWave = Mathf.Max(1, currentWave);
        GameRecords.StartRun();
        ApplyDifficulty();
        PrepareCurrentWaveEnemyCount();
        StartWave();
    }

    void Update()
    {
        if (isWaveActive &&
            enemySpawned >= enemyNeeded &&
            enemyleft <= 0 &&
            !waitingForNextWave)
        {
            waitingForNextWave = true;
            GiveWaveReward();
            StartCoroutine(NextWaveRoutine());
        }
    }

    void ApplyDifficulty()
    {
        bossWaveInterval = GlobalData.GetBossWaveInterval();

        switch (GlobalData.ActiveDifficulty)
        {
            case Difficulty.Easy:
                enemyNeeded = 8;
                timeBetweenWaves = 6f;
                break;

            case Difficulty.Normal:
                enemyNeeded = 12;
                timeBetweenWaves = 5f;
                break;

            case Difficulty.Hard:
                enemyNeeded = 18;
                timeBetweenWaves = 4f;
                break;

            case Difficulty.Extreme:
                enemyNeeded = 25;
                timeBetweenWaves = 3f;
                break;
        }

        enemyleft = enemyNeeded;
        enemySpawned = 0;
        baseEnemyNeeded = enemyNeeded;
    }

    IEnumerator NextWaveRoutine()
    {
        isWaveActive = false;
        BackgroundMusic.WaveEnded();

        yield return new WaitForSeconds(timeBetweenWaves);

        // Wave 1 is the starting wave. Every following wave advances once,
        // immediately before its enemies and label are prepared.
        currentWave++;

        PrepareCurrentWaveEnemyCount();

        rewardGiven = false;
        waitingForNextWave = false;

        StartWave();
    }

    private void GiveWaveReward()
    {
        if (rewardGiven)
            return;

        rewardGiven = true;
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.AddMoney(GetWaveReward());

        if (!IsBossWave)
            BackgroundMusic.WaveCompleted();

        GameRecords.Save();
    }

    private int GetNormalWaveEnemyCount()
    {
        int completedWaveCount = Mathf.Max(0, currentWave - 1);
        float growthMultiplier = Mathf.Max(1.01f, enemyGrowthMultiplier);
        float enemyCount = baseEnemyNeeded * Mathf.Pow(growthMultiplier, completedWaveCount);
        int cap = Mathf.Max(1, maxEnemiesPerWave);
        return Mathf.Clamp(Mathf.CeilToInt(Mathf.Min(enemyCount, cap)), 1, cap);
    }

    public float GetEnemyHealthMultiplierForCurrentWave()
    {
        int completedWaves = Mathf.Max(0, currentWave - 1);
        return Mathf.Pow(Mathf.Max(1f, enemyHealthGrowthPerWave), completedWaves);
    }

    public float GetEnemySpeedMultiplierForCurrentWave()
    {
        int completedWaves = Mathf.Max(0, currentWave - 1);
        float multiplier = 1f + completedWaves * Mathf.Max(0f, enemySpeedGrowthPerWave);
        return Mathf.Min(Mathf.Max(1f, maximumEnemySpeedMultiplier), multiplier);
    }

    public float GetEnemyResistanceForCurrentWave(float authoredResistance)
    {
        float original = Mathf.Clamp01(authoredResistance);
        int interval = Mathf.Max(1, resistanceGrowthInterval);
        int completedIntervals = Mathf.Max(0, currentWave / interval);
        float scaled = original + completedIntervals * Mathf.Clamp01(resistanceGrowthPerInterval);

        // Never reduce a prefab's authored specialty resistance. The cap only
        // limits resistance added by wave progression.
        return Mathf.Max(original, Mathf.Min(Mathf.Clamp01(maximumScaledResistance), scaled));
    }

    private void PrepareCurrentWaveEnemyCount()
    {
        enemyNeeded = IsBossWave
            ? BossEnemiesPerWave
            : GetNormalWaveEnemyCount();
        enemySpawned = 0;
        enemyleft = enemyNeeded;
    }

    public void RegisterBoss(EnemyHealth boss)
    {
        if (boss == null || !boss.isBoss)
            return;

        if (bossHealthBarUI == null)
            bossHealthBarUI = new BossHealthBarUI(transform);

        bossHealthBarUI.Show(boss);
    }

    int GetWaveReward()
    {
        return Mathf.Max(0, baseWaveReward);
    }

    void StartWave()
    {
        GameRecords.RecordWaveReached(currentWave);

        if (!IsBossWave)
            bossHealthBarUI?.Hide();

        BackgroundMusic.WaveStarted(IsBossWave);
        if (waveTitle == null)
        {
            Debug.LogWarning("WaveManager has no wave title assigned.", this);
        }
        else
        {
            if (waveTitleTMP == null)
                waveTitleTMP = waveTitle.GetComponent<TextMeshProUGUI>();

            if (waveTitleTMP != null)
            {
                waveTitleTMP.text = IsBossWave
                    ? "Wave: " + currentWave + " (BOSS)"
                    : "Wave: " + currentWave;
            }
            else
            {
                Debug.LogWarning("Wave title needs a TextMeshProUGUI component.", waveTitle);
            }

            waveTitle.SetActive(true);
        }

        WaveStarted?.Invoke(currentWave);
        isWaveActive = true;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            GameRecords.Save();
    }

    private void OnApplicationQuit()
    {
        GameRecords.Save();
    }
}

internal sealed class BossHealthBarUI
{
    private static readonly Color PanelColor = new Color(0.055f, 0.045f, 0.065f, 0.94f);
    private static readonly Color TrackColor = new Color(0.12f, 0.09f, 0.12f, 1f);
    private static readonly Color FillColor = new Color(0.86f, 0.08f, 0.18f, 1f);

    private readonly Transform owner;
    private EnemyHealth trackedBoss;
    private GameObject canvasObject;
    private UnityEngine.UI.Slider healthSlider;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI valueLabel;

    public BossHealthBarUI(Transform owner)
    {
        this.owner = owner;
    }

    public void Show(EnemyHealth boss)
    {
        if (boss == null)
            return;

        EnsureUI();
        StopTrackingBoss();

        trackedBoss = boss;
        trackedBoss.Damaged += HandleBossDamaged;
        trackedBoss.Died += HandleBossDied;

        healthSlider.minValue = 0f;
        healthSlider.maxValue = Mathf.Max(1f, trackedBoss.MaxHealth);
        titleLabel.text = $"BOSS - {GetDisplayName(trackedBoss.gameObject.name)}";
        RefreshValues();
        canvasObject.SetActive(true);
    }

    public void Hide()
    {
        StopTrackingBoss();
        if (canvasObject != null)
            canvasObject.SetActive(false);
    }

    public void Dispose()
    {
        StopTrackingBoss();
    }

    private void HandleBossDamaged(EnemyHealth boss, float damage)
    {
        if (boss == trackedBoss)
            RefreshValues();
    }

    private void HandleBossDied(EnemyHealth boss)
    {
        if (boss == trackedBoss)
            Hide();
    }

    private void RefreshValues()
    {
        if (trackedBoss == null || healthSlider == null || valueLabel == null)
            return;

        float currentHealth = trackedBoss.CurrentHealth;
        float maxHealth = Mathf.Max(1f, trackedBoss.MaxHealth);
        healthSlider.maxValue = maxHealth;
        healthSlider.value = Mathf.Clamp(currentHealth, 0f, maxHealth);
        valueLabel.SetText("{0:0}/{1:0}", currentHealth, maxHealth);
    }

    private void StopTrackingBoss()
    {
        if (trackedBoss == null)
            return;

        trackedBoss.Damaged -= HandleBossDamaged;
        trackedBoss.Died -= HandleBossDied;
        trackedBoss = null;
    }

    private void EnsureUI()
    {
        if (canvasObject != null)
            return;

        canvasObject = new GameObject(
            "Boss HUD Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler),
            typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.transform.SetParent(owner, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = CreateUIObject("Boss Health Bar", canvasObject.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -28f);
        panelRect.sizeDelta = new Vector2(920f, 100f);

        UnityEngine.UI.Image panelImage = panel.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = PanelColor;
        panelImage.raycastTarget = false;

        titleLabel = CreateLabel("Boss Name", panel.transform, TextAlignmentOptions.MidlineLeft);
        SetStretch(titleLabel.rectTransform, new Vector2(24f, 53f), new Vector2(-260f, -7f));
        titleLabel.fontSize = 28f;
        titleLabel.fontStyle = FontStyles.Bold;

        valueLabel = CreateLabel("Boss Health Value", panel.transform, TextAlignmentOptions.MidlineRight);
        SetStretch(valueLabel.rectTransform, new Vector2(650f, 53f), new Vector2(-24f, -7f));
        valueLabel.fontSize = 27f;
        valueLabel.fontStyle = FontStyles.Bold;

        GameObject sliderObject = CreateUIObject("Health Slider", panel.transform);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        SetStretch(sliderRect, new Vector2(24f, 16f), new Vector2(-24f, -52f));

        UnityEngine.UI.Image trackImage = sliderObject.AddComponent<UnityEngine.UI.Image>();
        trackImage.color = TrackColor;
        trackImage.raycastTarget = false;

        healthSlider = sliderObject.AddComponent<UnityEngine.UI.Slider>();
        healthSlider.transition = UnityEngine.UI.Selectable.Transition.None;
        healthSlider.interactable = false;
        healthSlider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;

        GameObject fillArea = CreateUIObject("Fill Area", sliderObject.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        SetStretch(fillAreaRect, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        GameObject fill = CreateUIObject("Fill", fillArea.transform);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        SetStretch(fillRect, Vector2.zero, Vector2.zero);

        UnityEngine.UI.Image fillImage = fill.AddComponent<UnityEngine.UI.Image>();
        fillImage.color = FillColor;
        fillImage.raycastTarget = false;

        healthSlider.fillRect = fillRect;
        healthSlider.targetGraphic = fillImage;
        canvasObject.SetActive(false);
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
        TextAlignmentOptions alignment)
    {
        GameObject labelObject = CreateUIObject(objectName, parent);
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.alignment = alignment;
        label.color = Color.white;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
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

    private static string GetDisplayName(string objectName)
    {
        return objectName.Replace("(Clone)", string.Empty).Trim().ToUpperInvariant();
    }
}
