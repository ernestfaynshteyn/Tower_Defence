using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public float health = 100f;
    public float currentHealth;
    public HealthBar healthBar;

    [Header("Defence")]
    [Tooltip("Defence capacity granted when the Defence Bar upgrade is unlocked.")]
    public float baseDefence = 20f;
    [Tooltip("Defence restored per second after the player has taken damage.")]
    public float defenceRegenPerSecond = 4f;
    public HealthBar defenceBar;

    public float currentDefence { get; private set; }
    public float maxDefence { get; private set; }

    private float baseHealth;
    private bool isDefeated;
    private bool defenceUnlocked;

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
        ApplyMaximumDefence(false);
    }

    private void Update()
    {
        if (isDefeated || !defenceUnlocked || currentDefence >= maxDefence)
            return;

        currentDefence = Mathf.Min(maxDefence, currentDefence + defenceRegenPerSecond * Time.deltaTime);
        if (defenceBar != null)
            defenceBar.SetHealth(currentDefence);
    }

    public void TakeDamage(float damage, string enemyID)
    {
        if (isDefeated || damage <= 0f)
            return;

        float remainingDamage = damage;

        if (defenceUnlocked && currentDefence > 0f)
        {
            float absorbedDamage = Mathf.Min(currentDefence, remainingDamage);
            currentDefence -= absorbedDamage;
            remainingDamage -= absorbedDamage;

            if (defenceBar != null)
                defenceBar.SetHealth(currentDefence);
        }

        currentHealth -= remainingDamage;
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

        if (statName == StatNames.Defence || statName == StatNames.DefenceUnlocked)
            ApplyMaximumDefence(true);
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

    private void ApplyMaximumDefence(bool preserveDefencePercentage)
    {
        float previousMaxDefence = maxDefence;
        float defenceRatio = previousMaxDefence > 0f
            ? currentDefence / previousMaxDefence
            : 1f;
        bool wasDefenceUnlocked = defenceUnlocked;

        defenceUnlocked = PlayerStats.instance != null &&
            PlayerStats.instance.GetModifiedValue(StatNames.DefenceUnlocked, 0f) > 0.5f;

        if (!defenceUnlocked)
        {
            maxDefence = 0f;
            currentDefence = 0f;
            if (defenceBar != null)
                defenceBar.gameObject.SetActive(false);
            return;
        }

        maxDefence = PlayerStats.instance != null
            ? PlayerStats.instance.GetModifiedValue(StatNames.Defence, baseDefence)
            : baseDefence;
        maxDefence = Mathf.Max(0f, maxDefence);

        currentDefence = preserveDefencePercentage && wasDefenceUnlocked
            ? Mathf.Clamp(maxDefence * defenceRatio, 0f, maxDefence)
            : maxDefence;

        if (defenceBar != null)
        {
            defenceBar.gameObject.SetActive(true);
            defenceBar.SetMaxHealth(maxDefence);
            defenceBar.SetHealth(currentDefence);
        }
    }
}
