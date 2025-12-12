using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float health = 100f;
    public float currentHealth;
    public HealthBar healthBar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = health;
        healthBar.SetMaxHealth(health);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void TakeDamage(float damage, int enemyID)
    {
        currentHealth -= damage;
        healthBar.SetHealth(currentHealth);
        CheckForHealth(enemyID);

    }
    public void CheckForHealth(int enemyID)
    {
        if (currentHealth <= 75f)
        {

        }
        if (currentHealth <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
