using UnityEngine;
public class Spawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float minSpawnTime = 1f;
    public float maxSpawnTime = 3f;

    [Header("Prefabs")]
    public GameObject[] normalEnemies;
    public GameObject bossEnemy;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    private float spawnTimer;
    private float currentSpawnTime;
    private bool warnedAboutInvalidBossPrefab;

    void Start()
    {
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

        if (normalEnemies == null)
            return null;

        int startIndex = Random.Range(0, normalEnemies.Length);
        for (int i = 0; i < normalEnemies.Length; i++)
        {
            GameObject candidate = normalEnemies[(startIndex + i) % normalEnemies.Length];
            if (HasEnemyHealth(candidate))
                return candidate;
        }

        return null;
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
