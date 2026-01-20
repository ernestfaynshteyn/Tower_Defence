using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public float health = 100f;
    public float currentHealth;
    public HealthBar healthBar;

    void Start()
    {
        currentHealth = health;
        healthBar.SetMaxHealth(health);
    }
    public void TakeDamage(float damage, int enemyID)
    {
        currentHealth -= damage;
        healthBar.SetHealth(currentHealth);
        CheckForHealth(enemyID);
    }

    public void CheckForHealth(int enemyID)
    {
        if (currentHealth <= 0.01f)
        {
            Debug.Log("Player defeated by enemy ID: " + enemyID);
            currentHealth = 0f;
            SceneManager.LoadScene(1);
        }
    }
}
