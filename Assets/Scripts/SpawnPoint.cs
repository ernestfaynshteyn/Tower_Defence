using UnityEngine;

[System.Serializable]
public class SpawnData{
    public float minSpawnTime;
    public float maxSpawnTime;
    public float spawnTime;
    public float spawnTimer;
    public Transform spawnTransform;

    public GameObject[] enemyList;
}


public class SpawnPoint : MonoBehaviour
{
    public SpawnData[] spawnPoints;

    private float Timer;
    public float spawnTime;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        //check if there is at least one spawn point
        if (spawnPoints.Length>0 && WaveManager.Instance.enemySpawned <= WaveManager.Instance.enemyNeeded) {
            // looping though each spawn point
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                SpawnData data = spawnPoints[i];
                data.spawnTimer += Time.deltaTime;
                //check if it is time to spawn a new enemy
                if (data.spawnTimer > data.spawnTime)
                {
                    //pick random enemy
                    int randEnemy = Random.Range(0, data.enemyList.Length);
                    //spawn da enemy
                    GameObject enemy = Instantiate(data.enemyList[randEnemy], data.spawnTransform.position,Quaternion.identity);
                    //every time we spawn enemy, we increase da enemySpawned by 1
                        WaveManager.Instance.enemySpawned += 1;
                    //Reset the timer
                    data.spawnTimer = 0;
                    data.spawnTime = Random.Range(data.minSpawnTime, data.maxSpawnTime);
                }
            }
        }
    }
}
