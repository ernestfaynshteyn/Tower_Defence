using Unity.VisualScripting;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Range(1, 10)]
    [SerializeField] private float speed = 10f;

    [Range(1, 10)]
    [SerializeField] private float lifeTime = 6f;

    [Range(1, 10)]
    [SerializeField] private float maxDistance = 3f;
    public float damage = 50f;
    
    private Rigidbody2D rb;

    public Vector3 direction;

    public Transform playerTransform;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifeTime);
    }
    private void Update()
    {
        if (Vector3.Distance(transform.position, playerTransform.position) > maxDistance)
        {
            Destroy(gameObject);
        }
    }
    private void FixedUpdate()
    {
        rb.linearVelocity = direction * speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyHealth enemy = collision.GetComponent<EnemyHealth>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);  // deal damage
        }

        Destroy(gameObject);  // delete bullet after hit
    }
}
