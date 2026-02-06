using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public int damageAmount = 10;       // How much damage the enemy does
    public float damageRate = 1f;       // Time between hits

    private float nextDamageTimer  = 0f;

    public string enemyID;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ApplyDifficulty();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
   
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (nextDamageTimer >= damageRate)
            {
                nextDamageTimer = 0;
                playerHealth.TakeDamage(damageAmount, enemyID);
            }
            if (playerHealth.currentHealth <= 0)
            {
                GlobalData.Instance.sprite = GetComponent<SpriteRenderer>().sprite;
            }
            nextDamageTimer += Time.deltaTime;
        }
    }

    void ApplyDifficulty()
    {
        switch (GlobalData.Instance.currentDifficulty)
        {
            case Difficulty.Easy:
                damageAmount = 7;
                damageRate = 1.2f;
                break;

            case Difficulty.Normal:
                damageAmount = 10;
                damageRate = 1f;
                break;

            case Difficulty.Hard:
                damageAmount = 13;
                damageRate = 0.8f;
                break;

            case Difficulty.Extreme:
                damageAmount = 15;
                damageRate = 0.6f;
                break;
        }
    }
}

