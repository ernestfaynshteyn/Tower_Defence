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
        if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) > maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed;
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
            Debug.LogWarning("Bullet Setup received 0 damage. Using default damage instead.");
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

        Debug.Log("Bullet damage set to: " + damage);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Bullet hit: " + collision.gameObject.name);

        EnemyHealth enemy = collision.GetComponentInParent<EnemyHealth>();

        if (enemy != null)
        {
            Debug.Log("EnemyHealth found on: " + enemy.gameObject.name);

            float finalDamage = damage;

            if (finalDamage <= 0f)
            {
                Debug.LogWarning("Bullet tried to deal 0 damage. Using default damage.");
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