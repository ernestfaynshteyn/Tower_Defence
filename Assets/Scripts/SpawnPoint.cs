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

    void Start()
    {
        ApplyDifficulty();
        ResetSpawnTime();
    }

    void Update()
    {
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
        switch (GlobalData.Instance.currentDifficulty)
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
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        if (WaveManager.Instance.IsBossWave)
        {
            Instantiate(bossEnemy, spawnPoint.position, Quaternion.identity);
        }
        else
        {
            GameObject enemy =
                normalEnemies[Random.Range(0, normalEnemies.Length)];

            Instantiate(enemy, spawnPoint.position, Quaternion.identity);
        }

        WaveManager.Instance.enemySpawned++;
    }

    void ResetSpawnTime()
    {
        currentSpawnTime = Random.Range(minSpawnTime, maxSpawnTime);
    }
}