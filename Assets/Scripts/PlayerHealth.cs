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
        healthBar.SetHealth(currentHealth);
        CheckForHealth(enemyID);
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
