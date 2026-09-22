using System.Collections;
using UnityEngine;
using TMPro;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    [Header("Wave State")]
    public int currentWave = 1;
    public int enemyNeeded;
    public int enemySpawned;
    public int enemyleft;

    [Header("Timing")]
    public float timeBetweenWaves = 5f;

    [Header("Enemy Growth")]
    [Min(1.01f)] public float enemyGrowthMultiplier = 2.1f;

    [Header("Boss Waves")]
    public int bossWaveInterval = 5; // every X waves
    public bool IsBossWave => currentWave % bossWaveInterval == 0;

    [Header("UI")]
    public GameObject waveTitle;
    private TextMeshProUGUI waveTitleTMP;

    private bool isWaveActive;
    private bool waitingForNextWave;
    private bool rewardGiven;
    private int baseEnemyNeeded;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    void Start()
    {
        currentWave = Mathf.Max(1, currentWave);
        ApplyDifficulty();
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

        yield return new WaitForSeconds(timeBetweenWaves);

        // Wave 1 is the starting wave. Every following wave advances once,
        // immediately before its enemies and label are prepared.
        currentWave++;

        if (IsBossWave)
        {
            enemyNeeded = 1; // boss only
        }
        else
        {
            enemyNeeded = GetNormalWaveEnemyCount();
        }

        enemySpawned = 0;
        enemyleft = enemyNeeded;

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
    }

    private int GetNormalWaveEnemyCount()
    {
        int completedWaveCount = Mathf.Max(0, currentWave - 1);
        float growthMultiplier = Mathf.Max(1.01f, enemyGrowthMultiplier);
        float enemyCount = baseEnemyNeeded * Mathf.Pow(growthMultiplier, completedWaveCount);
        return Mathf.Max(1, Mathf.CeilToInt(enemyCount));
    }

    int GetWaveReward()
    {
        switch (GlobalData.ActiveDifficulty)
        {
            case Difficulty.Easy: return 150;
            case Difficulty.Normal: return 200;
            case Difficulty.Hard: return 300;
            case Difficulty.Extreme: return 450;
        }
        return 200;
    }

    void StartWave()
    {
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

        isWaveActive = true;
    }
}
