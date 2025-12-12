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
        if (spawnPoints.Length>0) {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                SpawnData data = spawnPoints[i];
                data.spawnTimer += Time.deltaTime;
                if (data.spawnTimer > data.spawnTime)
                {
                    //Spawn Enemy
                    int randEnemy = Random.Range(0, data.enemyList.Length);

                    GameObject enemy = Instantiate(data.enemyList[randEnemy], data.spawnTransform.position,Quaternion.identity);

                    //Reset the timer
                    data.spawnTimer = 0;
                    data.spawnTime = Random.Range(data.minSpawnTime, data.maxSpawnTime);
                }
            }
        }
    }
}
