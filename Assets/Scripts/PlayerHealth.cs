using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    private const float LowHealthThreshold = 0.25f;
    private const float LowHealthResetThreshold = 0.35f;

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
    private bool lowHealthWarningActive;
    private float stunRemaining;

    public bool IsStunned => stunRemaining > 0f;

    private void Awake()
    {
        baseHealth = Mathf.Max(1f, health);

        // The range visual belongs to the player, so attach its lightweight
        // stat listener here instead of duplicating it on every weapon.
        if (GetComponent<RangeIndicatorScaler>() == null)
            gameObject.AddComponent<RangeIndicatorScaler>();

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
        if (stunRemaining > 0f)
            stunRemaining = Mathf.Max(0f, stunRemaining - Time.deltaTime);

        if (isDefeated || !defenceUnlocked || currentDefence >= maxDefence)
            return;

        currentDefence = Mathf.Min(maxDefence, currentDefence + defenceRegenPerSecond * Time.deltaTime);
        if (defenceBar != null)
            defenceBar.SetHealth(currentDefence);
    }

    public void TakeDamage(float damage, string enemyID)
    {
        TakeDamageInternal(damage, enemyID, 1f);
    }

    public void TakeCorrosiveDamage(float damage, string enemyID, float defenceDamageMultiplier)
    {
        TakeDamageInternal(damage, enemyID, Mathf.Max(1f, defenceDamageMultiplier));
    }

    private void TakeDamageInternal(float damage, string enemyID, float defenceDamageMultiplier)
    {
        if (isDefeated || damage <= 0f)
            return;

        float dodgeChance = PlayerStats.instance != null
            ? PlayerStats.instance.Get(StatNames.DodgeChance)
            : 0f;

        dodgeChance = Mathf.Clamp(dodgeChance, 0f, 100f);

        if (Random.value < dodgeChance / 100f)
        {
            Debug.Log($"Dodged {damage} damage.");
            BackgroundMusic.PlayerDodged();
            return;
        }

        float remainingDamage = damage;

        if (defenceUnlocked && currentDefence > 0f)
        {
            float defenceDamage = remainingDamage * defenceDamageMultiplier;
            float absorbedDefence = Mathf.Min(currentDefence, defenceDamage);
            float absorbedDamage = absorbedDefence / defenceDamageMultiplier;
            currentDefence -= absorbedDefence;
            remainingDamage -= absorbedDamage;

            if (absorbedDamage > 0f)
            {
                if (currentDefence <= 0.01f)
                    BackgroundMusic.DefenceBroken();
                else
                    BackgroundMusic.DefenceHit();
            }

            if (defenceBar != null)
                defenceBar.SetHealth(currentDefence);
        }

        if (remainingDamage > 0f)
            BackgroundMusic.PlayerHurt();

        currentHealth -= remainingDamage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        if (currentHealth > 0f)
            UpdateLowHealthWarning(true);

        CheckForHealth(enemyID);
    }

    public void ApplyStun(float duration)
    {
        if (isDefeated || duration <= 0f)
            return;

        stunRemaining = Mathf.Max(stunRemaining, duration);
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, health);
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        UpdateLowHealthWarning(false);
    }

    public void CheckForHealth(string enemyID)
    {
        if (!isDefeated && currentHealth <= 0.01f)
        {
            isDefeated = true;
            BackgroundMusic.PlayerDeath();
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
            string activeScene = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(PrototypeSceneRoutes.IsPrototypeGameplay(activeScene)
                ? PrototypeSceneRoutes.LossScene
                : "You lost! hahaha");
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

        UpdateLowHealthWarning(false);
    }

    private void UpdateLowHealthWarning(bool playSound)
    {
        float healthRatio = health > 0f ? currentHealth / health : 0f;
        if (healthRatio <= LowHealthThreshold)
        {
            if (!lowHealthWarningActive && playSound)
                BackgroundMusic.LowHealthWarning();

            lowHealthWarningActive = true;
        }
        else if (healthRatio >= LowHealthResetThreshold)
        {
            lowHealthWarningActive = false;
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

/// <summary>
/// Keeps the player's authored range circle in lockstep with projectile range.
/// </summary>
public class RangeIndicatorScaler : MonoBehaviour
{
    [SerializeField] private Transform rangeIndicator;

    private Vector3 baseScale;
    private float baseWorldRadius;
    private float runtimeRangeMultiplier = 1f;
    private SpriteRenderer rangeRenderer;
    private Transform[] rangeIndependentChildren = System.Array.Empty<Transform>();
    private Vector3[] baseChildLocalPositions = System.Array.Empty<Vector3>();
    private Vector3[] baseChildLocalScales = System.Array.Empty<Vector3>();

    private void Awake()
    {
        if (rangeIndicator == null)
            rangeIndicator = transform.Find("RangeIndicator");

        if (rangeIndicator != null)
        {
            baseScale = rangeIndicator.localScale;
            rangeRenderer = rangeIndicator.GetComponent<SpriteRenderer>();
            baseWorldRadius = CalculateCurrentWorldRadius();
            CacheRangeIndependentChildren();
        }
    }

    private void OnEnable()
    {
        PlayerStats.OnStatChanged += HandleStatChanged;
    }

    private void OnDisable()
    {
        PlayerStats.OnStatChanged -= HandleStatChanged;
    }

    private void Start()
    {
        RefreshScale();
    }

    private void HandleStatChanged(string statName, float oldValue, float newValue)
    {
        if (statName == StatNames.Range)
            RefreshScale();
    }

    private void RefreshScale()
    {
        if (rangeIndicator == null)
            return;

        float permanentMultiplier = PlayerStats.instance != null
            ? 1f + PlayerStats.instance.GetPercentModifier(StatNames.Range) / 100f
            : 1f;
        float multiplier = Mathf.Max(0.01f, permanentMultiplier) * Mathf.Max(0.01f, runtimeRangeMultiplier);
        rangeIndicator.localScale = baseScale * multiplier;

        // The current scene stores all weapon objects beneath RangeIndicator.
        // Counter-scale those children so buying range grows only the circle,
        // rather than making the gun, firing point, and collider grow as well.
        for (int i = 0; i < rangeIndependentChildren.Length; i++)
        {
            Transform child = rangeIndependentChildren[i];
            if (child == null)
                continue;

            child.localPosition = baseChildLocalPositions[i] / multiplier;
            child.localScale = baseChildLocalScales[i] / multiplier;
        }
    }

    public float WorldRadius
    {
        get
        {
            if (rangeIndicator == null)
                return 0f;

            if (rangeRenderer != null && rangeRenderer.sprite != null)
            {
                Vector3 extents = rangeRenderer.bounds.extents;
                return Mathf.Max(0f, Mathf.Min(extents.x, extents.y));
            }

            Vector3 scale = rangeIndicator.lossyScale;
            return Mathf.Max(0f, Mathf.Min(Mathf.Abs(scale.x), Mathf.Abs(scale.y)) * 0.5f);
        }
    }

    public float BaseWorldRadius => Mathf.Max(0f, baseWorldRadius);

    public void SetRuntimeRangeMultiplier(float multiplier)
    {
        runtimeRangeMultiplier = Mathf.Max(0.01f, multiplier);
        RefreshScale();
    }

    public Vector2 WorldCenter => rangeIndicator != null
        ? (Vector2)rangeIndicator.position
        : (Vector2)transform.position;

    public float ClampDistanceToIndicator(Vector2 origin, Vector2 direction, float desiredDistance)
    {
        desiredDistance = Mathf.Max(0f, desiredDistance);
        float radius = WorldRadius;
        if (radius <= 0f || direction.sqrMagnitude <= 0.0001f)
            return desiredDistance;

        Vector2 normalizedDirection = direction.normalized;
        Vector2 offset = origin - WorldCenter;
        float projection = Vector2.Dot(offset, normalizedDirection);
        float discriminant = projection * projection - (offset.sqrMagnitude - radius * radius);
        if (discriminant <= 0f)
            return 0f;

        float distanceToEdge = -projection + Mathf.Sqrt(discriminant);
        return Mathf.Min(desiredDistance, Mathf.Max(0f, distanceToEdge));
    }

    public Vector2 ClampPointToIndicator(Vector2 point)
    {
        float radius = WorldRadius;
        if (rangeIndicator == null || radius <= 0f)
            return point;

        Vector2 center = WorldCenter;
        Vector2 offset = point - center;
        return offset.sqrMagnitude <= radius * radius
            ? point
            : center + offset.normalized * radius;
    }

    private float CalculateCurrentWorldRadius()
    {
        if (rangeIndicator == null)
            return 0f;

        if (rangeRenderer != null && rangeRenderer.sprite != null)
        {
            Vector3 extents = rangeRenderer.bounds.extents;
            return Mathf.Max(0f, Mathf.Min(extents.x, extents.y));
        }

        Vector3 scale = rangeIndicator.lossyScale;
        return Mathf.Max(0f, Mathf.Min(Mathf.Abs(scale.x), Mathf.Abs(scale.y)) * 0.5f);
    }

    private void CacheRangeIndependentChildren()
    {
        var children = new System.Collections.Generic.List<Transform>();
        var positions = new System.Collections.Generic.List<Vector3>();
        var scales = new System.Collections.Generic.List<Vector3>();

        for (int i = 0; i < rangeIndicator.childCount; i++)
        {
            Transform child = rangeIndicator.GetChild(i);
            if (child.GetComponent<Shottingscript>() == null &&
                child.GetComponent<flameThrowerDamage>() == null)
            {
                continue;
            }

            children.Add(child);
            positions.Add(child.localPosition);
            scales.Add(child.localScale);
        }

        rangeIndependentChildren = children.ToArray();
        baseChildLocalPositions = positions.ToArray();
        baseChildLocalScales = scales.ToArray();
    }
}
