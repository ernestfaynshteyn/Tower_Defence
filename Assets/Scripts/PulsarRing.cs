using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An automatic area-damage ability unlocked by the Pulsar Ring skill.
/// While active it follows the player, pulses nearby enemies, and shows a
/// lightweight ring. The final skill keeps the ring active permanently.
/// </summary>
[DisallowMultipleComponent]
public class PulsarRing : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float baseDamage = 30f;
    [SerializeField] private float baseDuration = 10f;
    [SerializeField] private float radius = 3f;
    [SerializeField] private float pulseInterval = 1f;
    [SerializeField] private float cooldown = 4f;

    [Header("Ring Look")]
    [SerializeField] private Color ringColor = new Color(0.2f, 0.85f, 1f, 0.9f);
    [SerializeField, Range(12, 96)] private int ringSegments = 48;
    [SerializeField] private float ringWidth = 0.08f;
    [SerializeField] private int ringSortingOrder = 10;

    private const int MaxEnemiesPerPulse = 64;

    private readonly Collider2D[] overlapResults = new Collider2D[MaxEnemiesPerPulse];
    private readonly HashSet<EnemyHealth> enemiesHitThisPulse = new HashSet<EnemyHealth>();

    private LineRenderer ringRenderer;
    private Transform ringVisualTransform;
    private Material ringMaterial;

    private bool isUnlocked;
    private bool isPermanent;
    private bool isActive;
    private float activeUntil;
    private float nextActivationTime;
    private float nextPulseTime;

    private void Awake()
    {
        CreateRingVisual();
        SetRingVisible(false);
    }

    private void OnEnable()
    {
        PlayerStats.OnStatChanged += HandleStatChanged;
    }

    private void OnDisable()
    {
        PlayerStats.OnStatChanged -= HandleStatChanged;
        StopRing();
    }

    private void Start()
    {
        RefreshStats(true);
    }

    private void Update()
    {
        if (!isUnlocked)
            return;

        if (isPermanent)
        {
            if (!isActive)
                BeginRing();
        }
        else if (isActive && Time.time >= activeUntil)
        {
            EndTemporaryRing();
        }
        else if (!isActive && Time.time >= nextActivationTime)
        {
            BeginRing();
        }

        if (!isActive)
            return;

        if (Time.time >= nextPulseTime)
        {
            PulseEnemies();
            nextPulseTime = Time.time + Mathf.Max(0.05f, pulseInterval);
        }

        UpdateRingVisual();
    }

    private void HandleStatChanged(string statName, float oldValue, float newValue)
    {
        if (statName == StatNames.PizzaUnlocked ||
            statName == StatNames.PizzaDamage ||
            statName == StatNames.PizzaDuration ||
            statName == StatNames.PizzaPermanent)
        {
            RefreshStats(false);
        }
    }

    private void RefreshStats(bool startImmediately)
    {
        if (PlayerStats.instance == null)
            return;

        bool wasUnlocked = isUnlocked;
        bool wasPermanent = isPermanent;

        isUnlocked = PlayerStats.instance.GetModifiedValue(StatNames.PizzaUnlocked, 0f) > 0.5f;
        isPermanent = PlayerStats.instance.GetModifiedValue(StatNames.PizzaPermanent, 0f) > 0.5f;

        if (!isUnlocked)
        {
            StopRing();
            return;
        }

        if (isPermanent)
        {
            BeginRing();
            return;
        }

        if (startImmediately || !wasUnlocked || wasPermanent)
        {
            BeginRing();
        }
        else if (isActive)
        {
            // Buying duration while the ring is active should never shorten it.
            activeUntil = Mathf.Max(activeUntil, Time.time + GetDuration());
        }
    }

    private void BeginRing()
    {
        if (!isUnlocked)
            return;

        isActive = true;
        activeUntil = isPermanent ? float.PositiveInfinity : Time.time + GetDuration();
        nextPulseTime = Time.time;
        SetRingVisible(true);
    }

    private void EndTemporaryRing()
    {
        isActive = false;
        nextActivationTime = Time.time + Mathf.Max(0f, cooldown);
        SetRingVisible(false);
    }

    private void StopRing()
    {
        isActive = false;
        nextActivationTime = float.PositiveInfinity;
        SetRingVisible(false);
    }

    private void PulseEnemies()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, radius, overlapResults);
        if (hitCount <= 0)
            return;

        enemiesHitThisPulse.Clear();
        float damage = GetDamage();

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = overlapResults[i];
            if (hit == null)
                continue;

            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy != null && enemiesHitThisPulse.Add(enemy))
                enemy.TakeDamage(damage);

            overlapResults[i] = null;
        }
    }

    private float GetDamage()
    {
        float damage = PlayerStats.instance != null
            ? PlayerStats.instance.GetModifiedValue(StatNames.PizzaDamage, baseDamage)
            : baseDamage;
        return Mathf.Max(0f, damage);
    }

    private float GetDuration()
    {
        float duration = PlayerStats.instance != null
            ? PlayerStats.instance.GetModifiedValue(StatNames.PizzaDuration, baseDuration)
            : baseDuration;
        return Mathf.Max(0.05f, duration);
    }

    private void CreateRingVisual()
    {
        GameObject visual = new GameObject("Pulsar Ring Visual");
        ringVisualTransform = visual.transform;
        ringVisualTransform.SetParent(transform, false);

        ringRenderer = visual.AddComponent<LineRenderer>();
        ringRenderer.useWorldSpace = false;
        ringRenderer.loop = false;
        ringRenderer.alignment = LineAlignment.View;
        ringRenderer.textureMode = LineTextureMode.Stretch;
        ringRenderer.numCapVertices = 2;
        ringRenderer.sortingOrder = ringSortingOrder;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

        if (shader != null)
        {
            ringMaterial = new Material(shader);
            ringRenderer.material = ringMaterial;
        }
        else
        {
            Debug.LogWarning("[PulsarRing] Could not find a sprite shader for the ring visual.", this);
        }

        RebuildRingGeometry();
    }

    private void RebuildRingGeometry()
    {
        if (ringRenderer == null)
            return;

        int segments = Mathf.Clamp(ringSegments, 12, 96);
        float localRadius = radius;

        ringRenderer.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            ringRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * localRadius, Mathf.Sin(angle) * localRadius, 0f));
        }
    }

    private void UpdateRingVisual()
    {
        if (ringRenderer == null || ringVisualTransform == null)
            return;

        float visualScale = GetVisualScale();
        ringVisualTransform.localScale = new Vector3(1f / visualScale, 1f / visualScale, 1f);

        float glow = 0.7f + 0.3f * Mathf.Sin(Time.time * 6f);
        Color currentColor = ringColor;
        currentColor.a *= Mathf.Clamp01(glow);
        ringRenderer.startColor = currentColor;
        ringRenderer.endColor = currentColor;
        ringRenderer.startWidth = ringWidth;
        ringRenderer.endWidth = ringWidth;
    }

    private float GetVisualScale()
    {
        Vector3 scale = transform.lossyScale;
        return Mathf.Max(0.01f, (Mathf.Abs(scale.x) + Mathf.Abs(scale.y)) * 0.5f);
    }

    private void SetRingVisible(bool visible)
    {
        if (ringRenderer != null)
            ringRenderer.enabled = visible;
    }

    private void OnDestroy()
    {
        if (ringMaterial != null)
            Destroy(ringMaterial);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = ringColor;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
