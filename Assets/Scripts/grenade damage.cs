using UnityEngine;

public class GrenadeDamage : MonoBehaviour
{
    public float explosionRadius = 2f;
    public float damage = 50f;
    public LayerMask enemyLayer;

    private bool hasExploded = false;

    public GameObject explosion;
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasExploded) return;

        hasExploded = true;
        Explosion();
    }

    public void Explosion()
    {
        Collider2D[] enemiesHit = Physics2D.OverlapCircleAll(
            transform.position,
            explosionRadius,
            enemyLayer
        );

        foreach (Collider2D enemyCollider in enemiesHit)
        {
            EnemyHealth enemyHealth = enemyCollider.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
        }
        Instantiate(explosion, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}