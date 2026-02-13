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

    public GameObject explosionFX;
    public GameObject smokeAreaPrefab;
    public GameObject fireAreaPrefab;

    //private PlayerStats stats;
    /*8
    void Start()
    {
        stats = Object.FindAnyObjectByType<PlayerStats>();


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
                hit.GetComponent<EnemyHealth>().TakeDamage(damage);
        }
    }

    void SmokeExplode()
    {
        float duration = GetSmokeDuration();

        GameObject smoke = Instantiate(smokeAreaPrefab, transform.position, Quaternion.identity);
        Destroy(smoke, duration);
    }

    void FlashExplode()
    {
        float radius = GetRadius();
        float stunDuration = GetStunDuration();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
                hit.GetComponent<EnemyHealth>().Stun(stunDuration);
        }
    }

    void MolotovExplode()
    {
        float duration = GetFireDuration();

        GameObject fire = Instantiate(fireAreaPrefab, transform.position, Quaternion.identity);
        Destroy(fire, duration);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
            other.GetComponent<EnemyHealth>().ApplySlow(GetSlowMultiplier());
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
            other.GetComponent<EnemyHealth>().RemoveSlow();
    }

    // ----------- STAT GETTERS -----------

    float GetFuseTime() =>
        stats.GetFinalStat("Grenade_FuseTime", baseFuseTime);

    float GetRadius() =>
        stats.GetFinalStat("Grenade_Radius", baseRadius);

    float GetDamage() =>
        stats.GetFinalStat("Grenade_Damage", baseDamage);

    float GetSmokeDuration() =>
        stats.GetFinalStat("Grenade_SmokeDuration", baseSmokeDuration);

    float GetSlowMultiplier() =>
        stats.GetFinalStat("Grenade_SlowMultiplier", baseSlowMultiplier);

    float GetStunDuration() =>
        stats.GetFinalStat("Grenade_StunDuration", baseStunDuration);

    float GetFireDuration() =>
        stats.GetFinalStat("Grenade_FireDuration", baseFireDuration);

    float GetFireDPS() =>
        stats.GetFinalStat("Grenade_FireDPS", baseFireDamagePerSecond);*/
}
