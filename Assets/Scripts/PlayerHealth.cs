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

    public void TakeDamage(float damage, string enemyID)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        CheckForHealth(enemyID);
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, health);

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);
    }

    public void CheckForHealth(string enemyID)
    {
        if (currentHealth <= 0.01f)
        {
            if (GlobalData.Instance != null)
            {
                GlobalData.Instance.lastEnemyThatKilledPlayer = enemyID;
                Debug.Log("Player defeated by enemy ID: " + enemyID);
            }
            else
            {
                Debug.LogError("GlobalData Instance is NULL!");
            }

            currentHealth = 0f;
            SceneManager.LoadScene(1);
        }
    }
}