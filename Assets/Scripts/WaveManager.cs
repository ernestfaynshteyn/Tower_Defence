using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }
    public int enemyleft = 10;
    public int enemyNeeded = 10;
    public int enemySpawned = 0;

    public GameObject waveTitle;
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
        if (enemyleft <= 0)
        {
            waveTitle.SetActive(true);
        }
    }
}
