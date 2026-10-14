using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public int damageAmount = 10;       // How much damage the enemy does
    public float damageRate = 1f;       // Time between hits

    private float nextDamageTimer  = 0f;
    private IEnemyAttackEffect attackEffect;
    private SlimeCombatVfx combatVfx;

    public string enemyID;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        attackEffect = GetComponent<IEnemyAttackEffect>();
        combatVfx = GetComponent<SlimeCombatVfx>();
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
            if (playerHealth == null)
                return;

            if (nextDamageTimer >= damageRate)
            {
                nextDamageTimer = 0;
                Vector3 impactPosition = playerHealth.transform.position;
                if (attackEffect != null)
                    attackEffect.DealDamage(playerHealth, damageAmount, enemyID);
                else
                    playerHealth.TakeDamage(damageAmount, enemyID);

                combatVfx?.PlayAttackEffect(impactPosition);
            }
            if (playerHealth.currentHealth <= 0 && GlobalData.Instance != null)
            {
                GlobalData.Instance.sprite = GetComponent<SpriteRenderer>().sprite;
            }
            nextDamageTimer += Time.deltaTime;
        }
    }

    void ApplyDifficulty()
    {
        float damageMultiplier = 1f;
        float rateMultiplier = 1f;

        switch (GlobalData.ActiveDifficulty)
        {
            case Difficulty.Easy:
                damageMultiplier = 0.7f;
                rateMultiplier = 1.2f;
                break;

            case Difficulty.Normal:
                break;

            case Difficulty.Hard:
                damageMultiplier = 1.3f;
                rateMultiplier = 0.8f;
                break;

            case Difficulty.Extreme:
                damageMultiplier = 1.5f;
                rateMultiplier = 0.6f;
                break;
        }

        damageAmount = Mathf.Max(0, Mathf.RoundToInt(damageAmount * damageMultiplier));
        damageRate = Mathf.Max(0.05f, damageRate * rateMultiplier);
    }
}

