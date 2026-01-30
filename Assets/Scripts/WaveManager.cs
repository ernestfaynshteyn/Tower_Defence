using System.Collections;
using UnityEngine;
using TMPro;
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }
    public int enemyleft = 10;
    public int enemyNeeded = 10;
    public int enemySpawned = 0;
    public int currentWave = 1;
    private bool isWaitingForNextWave = false;
    private bool isWaveActive = false;
    public float timeBetweenWaves = 5f;
    private bool waveRewardGiven = false;
    private TextMeshProUGUI waveTitleTMP;

    public GameObject waveTitle;

    //public int[] enemiesPerWave = { 5, 8, 10 };
    private void Awake()
    {
        // Check if an instance already exists
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            // If no instance exists, set this as the instance
            Instance = this;
        }
    }
    void Start()
    {
        StartWave();
    }

    // Update is called once per frame
    void Update()
    {
        if (isWaveActive && enemySpawned >= enemyNeeded && enemyleft <= 0 && !isWaitingForNextWave)
        {
            isWaitingForNextWave = true;
            StartCoroutine(NextWaveCooldown());
        }

        if (enemyleft <= 0 && !waveRewardGiven)
        {
            currentWave++;
            waveTitle.SetActive(true);
            CurrencyManager.Instance.AddMoney(200);
            waveRewardGiven = true;
            Debug.Log("current wave " + currentWave);
        }
    }

    IEnumerator NextWaveCooldown()
    {
        isWaveActive = false;

        yield return new WaitForSeconds(timeBetweenWaves);

        int extraEnemies = currentWave + Random.Range(
            1,
            4);
        enemyNeeded += extraEnemies;

        enemySpawned = 0;
        enemyleft = enemyNeeded;
        waveRewardGiven = false;
        isWaitingForNextWave = false;

        StartWave();
    }




    void StartWave()
    {
        waveTitleTMP = waveTitle.GetComponent<TextMeshProUGUI>();
        waveTitleTMP.text = "Wave: " + currentWave.ToString();
        isWaveActive = true;
    }
}