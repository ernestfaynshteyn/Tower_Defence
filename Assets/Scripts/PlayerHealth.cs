using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public float health = 100f;
    public float currentHealth;
    public HealthBar healthBar;
    private float baseHealth;
    private bool isDefeated;

    private void Awake()
    {
        baseHealth = Mathf.Max(1f, health);
    }

    private void OnEnable()
    {
        PlayerStats.OnStatChanged += HandleStatChanged;
    }

    private void OnDisable()
    {
        PlayerStats.OnStatChanged -= HandleStatChanged;
    }

    void Start()
    {
        ApplyMaximumHealth(false);
    }

    public void TakeDamage(float damage, string enemyID)
    {
        if (isDefeated || damage <= 0f)
            return;

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
        if (!isDefeated && currentHealth <= 0.01f)
        {
            isDefeated = true;
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
            SceneManager.LoadScene("You lost! hahaha");
        }
    }

    private void HandleStatChanged(string statName, float oldValue, float newValue)
    {
        if (statName == StatNames.Health)
            ApplyMaximumHealth(true);
    }

    private void ApplyMaximumHealth(bool preserveHealthPercentage)
    {
        float previousMaxHealth = health;
        float healthRatio = previousMaxHealth > 0f ? currentHealth / previousMaxHealth : 1f;

        health = PlayerStats.instance != null
            ? PlayerStats.instance.GetModifiedValue(StatNames.Health, baseHealth)
            : baseHealth;
        health = Mathf.Max(1f, health);
        currentHealth = preserveHealthPercentage
            ? Mathf.Clamp(health * healthRatio, 0f, health)
            : health;

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(health);
            healthBar.SetHealth(currentHealth);
        }
    }
}
