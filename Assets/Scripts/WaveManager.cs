using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }
    public int enemyleft = 10;
    public int enemyNeeded = 10;
    public int enemySpawned = 0;
    public int currentWave = 1;
    private bool isWaveActive = false;
    public float timeBetweenWaves = 5f;
    private bool waveRewardGiven = false;

    public GameObject waveTitle;

    //public int[] enemiesPerWave = { 5, 8, 10 };
    private void Awake()
    {
        // Check if an instance already exists
        if (Instance != null && Instance != this)
        {
            // If another instance exists, destroy this one;
            Destroy(this.gameObject);
        }
        else
        {
            // If no instance exists, set this as the instance
            Instance = this;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isWaveActive && enemySpawned >= enemyNeeded && enemyleft <= 0)
        {
            StartCoroutine(NextWaveCooldown());
        }

        if (enemyleft <= 0 && !waveRewardGiven)
        {
            currentWave++;
            waveTitle.SetActive(true);
            CurrencyManager.Instance.AddMoney(200);
            waveRewardGiven = true;
        }
    }

    IEnumerator NextWaveCooldown()
    {
        isWaveActive = false;
        yield return new WaitForSeconds(timeBetweenWaves);

        enemyNeeded++;
        enemySpawned = 0;
        enemyleft = enemyNeeded;
        waveRewardGiven = false; // reset here

        StartWave();
    }

    void StartWave()
    {
        isWaveActive = true;
    }
}