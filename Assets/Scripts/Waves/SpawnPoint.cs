using UnityEngine;
public class Spawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float minSpawnTime = 1f;
    public float maxSpawnTime = 3f;

    [Header("Prefabs")]
    [Tooltip("Ordered unlock list. Element 0 is available from wave 1; later elements unlock every few waves.")]
    public GameObject[] normalEnemies;
    public GameObject bossEnemy;

    [Header("Enemy Progression")]
    [SerializeField, Min(1)] private int minWavesBetweenUnlocks = 3;
    [SerializeField, Min(1)] private int maxWavesBetweenUnlocks = 5;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    private float spawnTimer;
    private float currentSpawnTime;
    private bool warnedAboutInvalidBossPrefab;
    private int[] enemyUnlockWaves = System.Array.Empty<int>();

    void Start()
    {
        BuildEnemyUnlockSchedule();
        ApplyDifficulty();
        ResetSpawnTime();
    }

    void Update()
    {
        if (WaveManager.Instance == null)
            return;

        if (WaveManager.Instance.enemySpawned >= WaveManager.Instance.enemyNeeded)
            return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= currentSpawnTime)
        {
            SpawnEnemy();
            spawnTimer = 0f;
            ResetSpawnTime();
        }
    }
    void ApplyDifficulty()
    {
        switch (GlobalData.ActiveDifficulty)
        {
            case Difficulty.Easy:
                minSpawnTime = 1f;
                maxSpawnTime = 3f;
                break;
            case Difficulty.Normal:
                minSpawnTime = 0.5f;
                maxSpawnTime = 2.5f;
                break;
            case Difficulty.Hard:
                minSpawnTime = 0.3f;
                maxSpawnTime = 2f;
                break;
            case Difficulty.Extreme:
                minSpawnTime = 0.1f;
                maxSpawnTime = 1f;
                break;
        }
    }
    void SpawnEnemy()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Spawner has no spawn points assigned; disabling it to avoid repeated errors.", this);
            enabled = false;
            return;
        }

        GameObject enemyPrefab = GetEnemyPrefabForCurrentWave();
        if (enemyPrefab == null)
        {
            Debug.LogError("Spawner has no valid enemy prefab with EnemyHealth; disabling it to avoid a stuck wave.", this);
            enabled = false;
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);

        WaveManager.Instance.enemySpawned++;
    }

    private GameObject GetEnemyPrefabForCurrentWave()
    {
        if (WaveManager.Instance.IsBossWave && HasEnemyHealth(bossEnemy))
            return bossEnemy;

        if (WaveManager.Instance.IsBossWave && !warnedAboutInvalidBossPrefab)
        {
            warnedAboutInvalidBossPrefab = true;
            Debug.LogWarning("Boss wave prefab has no EnemyHealth. Spawning a normal enemy instead so the wave can finish. Assign a real boss prefab when one is ready.", this);
        }

        if (normalEnemies == null || normalEnemies.Length == 0)
            return null;

        int unlockedEnemyCount = GetUnlockedEnemyCount(WaveManager.Instance.currentWave);
        if (unlockedEnemyCount <= 0)
            return null;

        int startIndex = Random.Range(0, unlockedEnemyCount);
        for (int i = 0; i < unlockedEnemyCount; i++)
        {
            GameObject candidate = normalEnemies[(startIndex + i) % unlockedEnemyCount];
            if (HasEnemyHealth(candidate))
                return candidate;
        }

        return null;
    }

    private void BuildEnemyUnlockSchedule()
    {
        int enemyCount = normalEnemies != null ? normalEnemies.Length : 0;
        enemyUnlockWaves = new int[enemyCount];
        if (enemyCount == 0)
            return;

        enemyUnlockWaves[0] = 1;
        int minimumInterval = Mathf.Max(1, minWavesBetweenUnlocks);
        int maximumInterval = Mathf.Max(minimumInterval, maxWavesBetweenUnlocks);
        int nextUnlockWave = Random.Range(minimumInterval, maximumInterval + 1);

        for (int i = 1; i < enemyCount; i++)
        {
            enemyUnlockWaves[i] = nextUnlockWave;
            nextUnlockWave += Random.Range(minimumInterval, maximumInterval + 1);
        }
    }

    private int GetUnlockedEnemyCount(int wave)
    {
        if (enemyUnlockWaves == null || enemyUnlockWaves.Length != (normalEnemies?.Length ?? 0))
            BuildEnemyUnlockSchedule();

        int unlockedCount = 0;
        for (int i = 0; i < enemyUnlockWaves.Length; i++)
        {
            if (wave < enemyUnlockWaves[i])
                break;

            unlockedCount++;
        }

        return unlockedCount;
    }

    private bool HasEnemyHealth(GameObject prefab)
    {
        return prefab != null && prefab.GetComponent<EnemyHealth>() != null;
    }

    void ResetSpawnTime()
    {
        currentSpawnTime = Random.Range(minSpawnTime, maxSpawnTime);
    }
}
