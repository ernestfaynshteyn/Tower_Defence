using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public int damageAmount = 10;       // How much damage the enemy does
    public float damageRate = 1f;       // Time between hits

    private float nextDamageTimer  = 0f;
    private IEnemyAttackEffect attackEffect;
    private SlimeCombatVfx combatVfx;
    private Collider2D bodyCollider;
    private Collider2D playerCollider;
    private PlayerHealth playerHealth;
    private EnemyHealth enemyHealth;
    private float earlyWavePower = 1f;

    public string enemyID;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        attackEffect = GetComponent<IEnemyAttackEffect>();
        combatVfx = GetComponent<SlimeCombatVfx>();
        bodyCollider = GetComponent<Collider2D>();
        enemyHealth = GetComponent<EnemyHealth>();
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            playerCollider = player.GetComponent<Collider2D>();
        }
        ApplyDifficulty();
        earlyWavePower = WaveManager.Instance != null
            ? WaveManager.Instance.EarlyWaveEnemyPowerMultiplier : 1f;
    }

    private void FixedUpdate()
    {
        // Trigger Stay callbacks can stop when the slime's Rigidbody2D sleeps.
        // Check the root hurtbox directly so stationary contact still attacks,
        // and weapon/range colliders cannot count as touching the tower.
        if (playerHealth == null || bodyCollider == null || playerCollider == null ||
            !bodyCollider.enabled || !playerCollider.enabled ||
            (enemyHealth != null && enemyHealth.CurrentHealth <= 0f) ||
            !bodyCollider.Distance(playerCollider).isOverlapped)
            return;

        nextDamageTimer += Time.fixedDeltaTime;
        if (nextDamageTimer >= damageRate)
        {
            nextDamageTimer = 0;
            Vector3 impactPosition = playerHealth.transform.position;
            if (attackEffect != null)
                attackEffect.DealDamage(playerHealth, damageAmount * earlyWavePower, enemyID);
            else
                playerHealth.TakeDamage(damageAmount * earlyWavePower, enemyID);

            combatVfx?.PlayAttackEffect(impactPosition);
        }
        if (playerHealth.currentHealth <= 0 && GlobalData.Instance != null)
        {
            GlobalData.Instance.sprite = GetComponent<SpriteRenderer>().sprite;
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

