using UnityEngine;

[DisallowMultipleComponent]
public sealed class PrototypePoisonStatus : MonoBehaviour
{
    private EnemyHealth enemyHealth;
    private float remainingDuration;
    private float damagePerSecond;
    private float tickTimer;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
    }

    public void Apply(float duration, float dps)
    {
        remainingDuration = Mathf.Max(remainingDuration, duration);
        damagePerSecond = Mathf.Max(damagePerSecond, dps);
    }

    private void Update()
    {
        if (enemyHealth == null || remainingDuration <= 0f)
        {
            Destroy(this);
            return;
        }

        remainingDuration -= Time.deltaTime;
        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f)
            return;

        tickTimer = 0.5f;
        enemyHealth.TakeDamage(damagePerSecond * 0.5f, DamageNumberType.Poison);
    }
}
