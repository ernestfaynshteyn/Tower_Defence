using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 6f;
    [SerializeField] private float maxDistance = 3f;

    [SerializeField] private float defaultDamage = 50f;

    public float damage;

    private float critChance = 0f;
    private float critMultiplier = 2f;
    private float lifestealChance = 0f;
    private float lifestealAmount = 0f;

    private Rigidbody2D rb;
    private PlayerHealth ownerHealth;

    public Vector3 direction;
    public Transform playerTransform;

    public float BaseSpeed => speed;
    public float BaseMaxDistance => maxDistance;

    // Cached normalized movement to avoid repeated normalization every FixedUpdate
    private Vector2 cachedDirection = Vector2.zero;
    private Vector3 lastDirection = Vector3.zero;
    private bool isDirectionCached = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (damage <= 0f)
        {
            damage = defaultDamage;
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (playerTransform != null)
        {
            float maxDistSqr = maxDistance * maxDistance;
            if ((transform.position - playerTransform.position).sqrMagnitude > maxDistSqr)
            {
                Destroy(gameObject);
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            // Cache normalized direction and only recompute when direction changes
            if (!isDirectionCached || lastDirection != direction)
            {
                if (direction == Vector3.zero)
                {
                    cachedDirection = Vector2.zero;
                }
                else
                {
                    cachedDirection = ((Vector2)direction).normalized;
                }
                lastDirection = direction;
                isDirectionCached = true;
            }

            rb.linearVelocity = cachedDirection * speed;
        }
    }

    public void Setup(
        float newDamage,
        float newSpeed,
        float newMaxDistance,
        float newCritChance,
        float newCritMultiplier,
        float newLifestealChance,
        float newLifestealAmount,
        PlayerHealth newOwnerHealth,
        Transform shooterTransform
    )
    {
        if (newDamage <= 0f)
        {
            damage = defaultDamage;
        }
        else
        {
            damage = newDamage;
        }

        speed = newSpeed;
        maxDistance = newMaxDistance;
        critChance = Mathf.Clamp01(newCritChance);
        critMultiplier = Mathf.Max(1f, newCritMultiplier);
        lifestealChance = Mathf.Clamp01(newLifestealChance);
        lifestealAmount = Mathf.Max(0f, newLifestealAmount);
        ownerHealth = newOwnerHealth;
        playerTransform = shooterTransform;

        // Prime cached direction if direction already set by spawner before Setup is called
        if (direction != Vector3.zero)
        {
            cachedDirection = ((Vector2)direction).normalized;
            lastDirection = direction;
            isDirectionCached = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Avoid logging in hot collision code paths
        EnemyHealth enemy = collision.GetComponentInParent<EnemyHealth>();

        if (enemy != null)
        {
            float finalDamage = damage;

            if (finalDamage <= 0f)
            {
                finalDamage = defaultDamage;
            }

            if (Random.value < critChance)
            {
                finalDamage *= critMultiplier;
            }

            enemy.TakeDamage(finalDamage);

            if (ownerHealth != null && lifestealAmount > 0f && Random.value < lifestealChance)
            {
                ownerHealth.Heal(finalDamage * lifestealAmount);
            }

            Destroy(gameObject);
        }
    }
}
