// AI assistance: Codex helped implement and revise the armory, loadout, and weapon systems. Ernest directed the design.
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

        float activeTime = Mathf.Min(Time.deltaTime, remainingDuration);
        remainingDuration -= activeTime;
        tickTimer += activeTime;
        while (tickTimer >= 0.5f)
        {
            enemyHealth.TakeDamage(damagePerSecond * 0.5f, DamageNumberType.Poison);
            tickTimer -= 0.5f;
        }

        if (remainingDuration <= 0f)
        {
            if (tickTimer > 0f)
                enemyHealth.TakeDamage(damagePerSecond * tickTimer, DamageNumberType.Poison);
            Destroy(this);
        }
    }
}
