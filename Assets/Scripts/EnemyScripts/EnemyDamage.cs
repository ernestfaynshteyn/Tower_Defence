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
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
   
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>().;
            if (nextDamageTimer >= damageRate)
            {
                nextDamageTimer = 0;
                playerHealth.TakeDamage(damageAmount, enemyID);
            }
            if (playerHealth.currentHealth <= 0)
            {
                GlobalData
            }
            nextDamageTimer += Time.deltaTime;
        }
    }
}

