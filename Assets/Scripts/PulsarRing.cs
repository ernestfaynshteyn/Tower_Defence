using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The original Pulsar Ring component is retained so existing scene references
/// stay valid, but it now implements the player-triggered Sewer Pulse ability.
/// </summary>
[DisallowMultipleComponent]
public class PulsarRing : MonoBehaviour
{
    [Header("Sewer Pulse")]
    [SerializeField] private KeyCode activationKey = KeyCode.Z;
    [SerializeField, Min(0f)] private float baseDamage = 30f;
    [SerializeField, Min(0.1f)] private float radius = 3f;
    [SerializeField, Min(0f)] private float baseKnockbackDistance = 2.5f;
    [SerializeField, Min(0.1f)] private float baseCooldown = 8f;

    [Header("Pulse Look")]
    [SerializeField] private Color pulseColor = new Color(0.2f, 0.85f, 1f, 0.9f);
    [SerializeField, Range(12, 96)] private int ringSegments = 48;
    [SerializeField, Min(0.01f)] private float ringWidth = 0.08f;
    [SerializeField] private int ringSortingOrder = 10;
    [SerializeField, Min(0.05f)] private float visualDuration = 0.35f;

    private const int MaxEnemiesPerPulse = 128;

    private readonly Collider2D[] overlapResults = new Collider2D[MaxEnemiesPerPulse];
    private readonly HashSet<EnemyHealth> enemiesHit = new HashSet<EnemyHealth>();

    private PlayerHealth playerHealth;
    private bool isUnlocked;
    private float nextReadyTime;

    public bool IsUnlocked => isUnlocked;
    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);

    private void Awake()
    {
        playerHealth = GetComponentInParent<PlayerHealth>();

        // EMP and Toxic Flood share this player object and are kept in a
        // separate reusable controller. This also repairs older scenes that do
        // not have the component serialized yet.
        if (GetComponent<TowerAbilityController>() == null)
            gameObject.AddComponent<TowerAbilityController>();
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
        RefreshUnlock();
    }

    private void Update()
    {
        if (Input.GetKeyDown(activationKey))
            TryActivate();
    }

    public bool TryActivate()
    {
        if (!isUnlocked || Time.timeScale <= 0f || Time.time < nextReadyTime ||
            (playerHealth != null && playerHealth.IsStunned))
        {
            return false;
        }

        PulseEnemies();
        StartCoroutine(PlayPulseVisual(GetRadius()));
        BackgroundMusic.PulsarActivated();
        BackgroundMusic.PulsarPulsed();
        nextReadyTime = Time.time + GetCooldown();
        return true;
    }

    private void HandleStatChanged(string statName, float oldValue, float newValue)
    {
        if (statName == StatNames.SewerPulseUnlocked || statName == StatNames.PizzaUnlocked)
            RefreshUnlock();
    }

    private void RefreshUnlock()
    {
        if (PlayerStats.instance == null)
        {
            isUnlocked = false;
            return;
        }

        // PizzaUnlocked is read only as a compatibility fallback for a scene
        // that has not yet been resaved by the skill-tree installer.
        isUnlocked = PlayerStats.instance.GetModifiedValue(StatNames.SewerPulseUnlocked, 0f) > 0.5f ||
                     PlayerStats.instance.GetModifiedValue(StatNames.PizzaUnlocked, 0f) > 0.5f;
    }

    private void PulseEnemies()
    {
        float pulseRadius = GetRadius();
        int hitCount = Physics2D.OverlapCircle(
            transform.position,
            pulseRadius,
            ContactFilter2D.noFilter,
            overlapResults);

        enemiesHit.Clear();
        float damage = GetDamage();
        float knockbackDistance = GetKnockbackDistance();

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = overlapResults[i];
            overlapResults[i] = null;
            if (hit == null)
                continue;

            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy == null || !enemiesHit.Add(enemy))
                continue;

            enemy.TakeDamage(damage, DamageNumberType.Energy);
            enemy.PushAway(transform.position, knockbackDistance);
        }
    }

    private float GetDamage()
    {
        return PlayerStats.instance != null
            ? Mathf.Max(0f, PlayerStats.instance.GetModifiedValue(StatNames.SewerPulseDamage, baseDamage))
            : baseDamage;
    }

    private float GetRadius()
    {
        return PlayerStats.instance != null
            ? Mathf.Max(0.1f, PlayerStats.instance.GetModifiedValue(StatNames.SewerPulseRadius, radius))
            : radius;
    }

    private float GetKnockbackDistance()
    {
        return PlayerStats.instance != null
            ? Mathf.Max(0f, PlayerStats.instance.GetModifiedValue(StatNames.SewerPulseForce, baseKnockbackDistance))
            : baseKnockbackDistance;
    }

    private float GetCooldown()
    {
        float reduction = PlayerStats.instance != null
            ? PlayerStats.instance.GetPercentModifier(StatNames.SewerPulseCooldownReduction)
            : 0f;
        return Mathf.Max(1f, baseCooldown * (1f - Mathf.Clamp(reduction, 0f, 80f) / 100f));
    }

    private IEnumerator PlayPulseVisual(float targetRadius)
    {
        GameObject visual = new GameObject("Sewer Pulse Visual");
        visual.transform.SetParent(transform, false);

        LineRenderer line = visual.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.alignment = LineAlignment.View;
        line.positionCount = Mathf.Clamp(ringSegments, 12, 96);
        line.startWidth = ringWidth;
        line.endWidth = ringWidth;
        line.sortingOrder = ringSortingOrder;
        line.material = CreateSpriteMaterial();

        float elapsed = 0f;
        while (elapsed < visualDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / visualDuration);
            float currentRadius = Mathf.Lerp(0.15f, targetRadius, progress);
            SetCircle(line, currentRadius);

            Color color = pulseColor;
            color.a *= 1f - progress;
            line.startColor = color;
            line.endColor = color;
            yield return null;
        }

        if (line.material != null)
            Destroy(line.material);
        Destroy(visual);
    }

    private static Material CreateSpriteMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        return shader != null ? new Material(shader) : null;
    }

    private static void SetCircle(LineRenderer line, float circleRadius)
    {
        int count = line.positionCount;
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * circleRadius);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = pulseColor;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
