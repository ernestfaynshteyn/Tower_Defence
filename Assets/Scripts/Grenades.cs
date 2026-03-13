using UnityEngine;

public enum GrenadeType
{
    Frag,
    Smoke,
    Flash,
    Molotov
}

public class Grenades : MonoBehaviour
{
    PlayerStats stats;

    [Header("Type")]
    public GrenadeType grenadeType;

    [Header("Common Base Stats")]
    public float baseFuseTime = 1.5f;
    public float baseRadius = 3f;

    [Header("Frag")]
    public int baseDamage = 60;

    [Header("Smoke")]
    public float baseSmokeDuration = 5f;
    public float baseSlowMultiplier = 0.5f;

    [Header("Flash")]
    public float baseStunDuration = 2f;

    [Header("Molotov")]
    public float baseFireDuration = 6f;
    public int baseFireDamagePerSecond = 8;

    [Header("Effects")]
    public GameObject explosionFX;
    public GameObject smokeAreaPrefab;
    public GameObject fireAreaPrefab;

    void Start()
    {
        stats = PlayerStats.instance;

        Invoke(nameof(Explode), GetFuseTime());
    }

    void Explode()
    {
        switch (grenadeType)
        {
            case GrenadeType.Frag:
                FragExplode();
                break;

            case GrenadeType.Smoke:
                SmokeExplode();
                break;

            case GrenadeType.Flash:
                FlashExplode();
                break;

            case GrenadeType.Molotov:
                MolotovExplode();
                break;
        }

        if (explosionFX)
            Instantiate(explosionFX, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    void FragExplode()
    {
        float radius = GetRadius();
        int damage = Mathf.RoundToInt(GetDamage());

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
                if (enemy != null)
                    enemy.TakeDamage(damage);
            }
        }
    }

    void SmokeExplode()
    {
        float duration = GetSmokeDuration();

        if (smokeAreaPrefab)
        {
            GameObject smoke = Instantiate(smokeAreaPrefab, transform.position, Quaternion.identity);
            Destroy(smoke, duration);
        }
    }

    void FlashExplode()
    {
        float radius = GetRadius();
        float stunDuration = GetStunDuration();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
                if (enemy != null)
                    enemy.Stun(stunDuration);
            }
        }
    }

    void MolotovExplode()
    {
        float duration = GetFireDuration();

        if (fireAreaPrefab)
        {
            GameObject fire = Instantiate(fireAreaPrefab, transform.position, Quaternion.identity);
            Destroy(fire, duration);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (grenadeType != GrenadeType.Smoke) return;

        if (other.CompareTag("Enemy"))
        {
            EnemyHealth enemy = other.GetComponent<EnemyHealth>();
            if (enemy != null)
                enemy.ApplySlow(GetSlowMultiplier());
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (grenadeType != GrenadeType.Smoke) return;

        if (other.CompareTag("Enemy"))
        {
            EnemyHealth enemy = other.GetComponent<EnemyHealth>();
            if (enemy != null)
                enemy.RemoveSlow();
        }
    }

//stat getter with random stuff idek how works i forgor

    float GetFuseTime() =>
        stats != null ? stats.GetFinalStat("Grenade_FuseTime", baseFuseTime) : baseFuseTime;

    float GetRadius() =>
        stats != null ? stats.GetFinalStat("Grenade_Radius", baseRadius) : baseRadius;

    float GetDamage() =>
        stats != null ? stats.GetFinalStat("Grenade_Damage", baseDamage) : baseDamage;

    float GetSmokeDuration() =>
        stats != null ? stats.GetFinalStat("Grenade_SmokeDuration", baseSmokeDuration) : baseSmokeDuration;

    float GetSlowMultiplier() =>
        stats != null ? stats.GetFinalStat("Grenade_SlowMultiplier", baseSlowMultiplier) : baseSlowMultiplier;

    float GetStunDuration() =>
        stats != null ? stats.GetFinalStat("Grenade_StunDuration", baseStunDuration) : baseStunDuration;

    float GetFireDuration() =>
        stats != null ? stats.GetFinalStat("Grenade_FireDuration", baseFireDuration) : baseFireDuration;

    float GetFireDPS() =>
        stats != null ? stats.GetFinalStat("Grenade_FireDPS", baseFireDamagePerSecond) : baseFireDamagePerSecond;
}