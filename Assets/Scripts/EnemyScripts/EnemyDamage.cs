using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public int damageAmount = 10;       // How much damage the enemy does
    public float damageRate = 1f;       // Time between hits

    private float nextDamageTimer  = 0f;

    public int enemyID=0;
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
            if (nextDamageTimer >= damageRate)
            {
                nextDamageTimer = 0;
                collision.gameObject.GetComponent<PlayerHealth>().TakeDamage(damageAmount, enemyID);
            }
            nextDamageTimer += Time.deltaTime;
        }
    }
}

