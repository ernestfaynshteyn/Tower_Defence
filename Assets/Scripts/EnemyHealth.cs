using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    private float health = 100f;

    public void TakeDamage(float damage)
    {
        health -= damage;
        CheckForHealth();

    }
    public void CheckForHealth()
    {
        if (health <= 75f)
        {

        }
        if (health <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
