using UnityEngine;
public class EnemyHealth : MonoBehaviour
{
    private float health = 100f;
    public int moneyReward = 10;

    public Animator animator;

    public void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        CheckForHealth();

    }
    public void CheckForHealth()
    {
        if (health <= 75f)
        {
            //animator.SetTrigger("HurtPhase");
        }
        if (health <= 0f)
        {
            WaveManager.Instance.enemyleft = WaveManager.Instance.enemyleft - 1;
            Die();
        }
    }
    void Die()
    {
        CurrencyManager.Instance.AddMoney(moneyReward);
        Destroy(gameObject);

        animator.SetTrigger("Die");
        // Disable the enemy
        GetComponent<Collider>().enabled = false;
        this.enabled = false;
    }
}