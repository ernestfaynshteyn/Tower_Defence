using UnityEngine;

public class GrenadeDamage : MonoBehaviour
{
 public void Explosion()
    {
        Collider2D[] enemiesHit = Physics2D.OverlapCircleAll(
            transf    public float explosionRadius = 2f;
    public float damage = 50f;
    public LayerMask enemyLayer;

   orm.position,
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

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}