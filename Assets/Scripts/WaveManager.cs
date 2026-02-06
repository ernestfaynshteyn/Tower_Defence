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

    [Header("Boss Waves")]
    public int bossWaveInterval = 5; // every X waves
    public bool IsBossWave => currentWave % bossWaveInterval == 0;

    [Header("UI")]
    public GameObject waveTitle;
    private TextMeshProUGUI waveTitleTMP;

    private bool isWaveActive;
    private bool waitingForNextWave;
    private bool rewardGiven;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    void Start()
    {
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
            StartCoroutine(NextWaveRoutine());
        }

        if (enemyleft <= 0 && !rewardGiven)
        {
            rewardGiven = true;
            CurrencyManager.Instance.AddMoney(GetWaveReward());
            currentWave++;
        }
    }

    void ApplyDifficulty()
    {
        switch (GlobalData.Instance.currentDifficulty)
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
    }

    IEnumerator NextWaveRoutine()
    {
        isWaveActive = false;

        yield return new WaitForSeconds(timeBetweenWaves);

        if (IsBossWave)
        {
            enemyNeeded = 1; // boss only
        }
        else
        {
            enemyNeeded += GetEndlessScaling();
        }

        enemySpawned = 0;
        enemyleft = enemyNeeded;

        rewardGiven = false;
        waitingForNextWave = false;

        StartWave();
    }

    int GetEndlessScaling()
    {
        float difficultyMultiplier = 1f;

        switch (GlobalData.Instance.currentDifficulty)
        {
            case Difficulty.Easy: difficultyMultiplier = 0.8f; break;
            case Difficulty.Normal: difficultyMultiplier = 1f; break;
            case Difficulty.Hard: difficultyMultiplier = 1.3f; break;
            case Difficulty.Extreme: difficultyMultiplier = 1.6f; break;
        }

        float curve = Mathf.Pow(currentWave, 1.15f);
        return Mathf.RoundToInt(curve * difficultyMultiplier);
    }

    int GetWaveReward()
    {
        switch (GlobalData.Instance.currentDifficulty)
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
        waveTitleTMP = waveTitle.GetComponent<TextMeshProUGUI>();

        waveTitleTMP.text = IsBossWave
            ? "BOSS WAVE"
            : "Wave: " + currentWave;

        waveTitle.SetActive(true);
        isWaveActive = true;
    }
}