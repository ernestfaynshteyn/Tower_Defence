using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 6f;
    [SerializeField] private float maxDistance = 3f;

    public float damage = 50f;
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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) > maxDistance)
        {
            Destroy(gameObject);
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
            rb.linearVelocity = direction * speed;
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
        damage = newDamage;
        speed = newSpeed;
        maxDistance = newMaxDistance;
        critChance = Mathf.Clamp01(newCritChance);
        critMultiplier = Mathf.Max(1f, newCritMultiplier);
        lifestealChance = Mathf.Clamp01(newLifestealChance);
        lifestealAmount = Mathf.Max(0f, newLifestealAmount);
        ownerHealth = newOwnerHealth;
        playerTransform = shooterTransform;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyHealth enemy = collision.GetComponent<EnemyHealth>();

        if (enemy != null)
        {
            float finalDamage = damage;

            if (Random.value < critChance)
                finalDamage *= critMultiplier;

            enemy.TakeDamage(finalDamage);

            if (ownerHealth != null && lifestealAmount > 0f && Random.value < lifestealChance)
            {
                ownerHealth.Heal(finalDamage * lifestealAmount);
            }
        }

        Destroy(gameObject);
    }
}