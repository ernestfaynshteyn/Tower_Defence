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

    [Header("Common")]
    public float fuseTime = 1.5f;
    public float radius = 3f;

    [Header("Frag")]
    public int damage = 60;

    [Header("Smoke")]
    public float smokeDuration = 5f;
    public float slowMultiplier = 0.5f;

    [Header("Flash")]
    public float stunDuration = 2f;

    [Header("Molotov")]
    public float fireDuration = 6f;
    public int fireDamagePerSecond = 8;

    public GameObject explosionFX;
    public GameObject smokeAreaPrefab;
    public GameObject fireAreaPrefab;

    void Start()
    {
        Invoke(nameof(Explode), fuseTime);
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
        {
            Instantiate(explosionFX, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }

        void FragExplode()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);

            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                    hit.GetComponent<EnemyHealth>().TakeDamage(damage);
            }
        }

        void SmokeExplode()
        {
            GameObject smoke = Instantiate(smokeAreaPrefab, transform.position, Quaternion.identity);
            Destroy(smoke, smokeDuration);
        }

        void FlashExplode()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);

            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                    hit.GetComponent<EnemyHealth>().Stun(stunDuration);
            }
        }

        void MolotovExplode()
        {
            GameObject fire = Instantiate(fireAreaPrefab, transform.position, Quaternion.identity);
            Destroy(fire, fireDuration);
        }
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
            other.GetComponent<EnemyHealth>().ApplySlow(slowMultiplier);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
            other.GetComponent<EnemyHealth>().RemoveSlow();
    }
}

