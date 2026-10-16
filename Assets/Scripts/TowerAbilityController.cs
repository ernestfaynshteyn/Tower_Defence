using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns active tower abilities that are unlocked and upgraded by the main
/// skill tree. Public activation methods allow future HUD buttons or remapped
/// input to trigger the same behavior without duplicating gameplay logic.
/// </summary>
[DisallowMultipleComponent]
public sealed class TowerAbilityController : MonoBehaviour
{
    [Header("Sewer Pulse")]
    [SerializeField] private KeyCode sewerPulseKey = KeyCode.Z;
    [SerializeField, Min(0f)] private float baseSewerPulseDamage = 30f;
    [SerializeField, Min(0.1f)] private float baseSewerPulseRadius = 3f;
    [SerializeField, Min(0f)] private float baseSewerPulseForce = 2.5f;
    [SerializeField, Min(0.1f)] private float baseSewerPulseCooldown = 8f;

    [Header("EMP Shockwave")]
    [SerializeField] private KeyCode empKey = KeyCode.X;
    [SerializeField, Min(0.1f)] private float baseEmpDuration = 1f;
    [SerializeField, Min(0.1f)] private float baseEmpCooldown = 20f;
    [SerializeField, Min(1f)] private float empVisualRadius = 14f;

    [Header("Toxic Flood")]
    [SerializeField] private KeyCode toxicFloodKey = KeyCode.C;
    [SerializeField, Range(1, 8)] private int toxicZoneCount = 3;
    [SerializeField, Min(0f)] private float toxicZoneDistance = 2.8f;
    [SerializeField, Min(0.1f)] private float toxicZoneRadius = 2.25f;
    [SerializeField, Min(0.1f)] private float baseToxicDuration = 5f;
    [SerializeField, Min(0f)] private float baseToxicDamagePerSecond = 12f;
    [SerializeField, Min(0.1f)] private float baseToxicCooldown = 24f;
    [SerializeField, Min(0.05f)] private float toxicTickInterval = 0.5f;

    [Header("Ability Look")]
    [SerializeField] private Color sewerPulseColor = new Color(0.2f, 0.85f, 1f, 0.9f);
    [SerializeField] private Color empColor = new Color(0.25f, 0.85f, 1f, 0.95f);
    [SerializeField] private Color toxicColor = new Color(0.25f, 1f, 0.15f, 0.8f);
    [SerializeField, Range(12, 96)] private int circleSegments = 48;
    [SerializeField, Min(0.01f)] private float lineWidth = 0.09f;
    [SerializeField] private int sortingOrder = 11;

    private PlayerHealth playerHealth;
    private float sewerPulseReadyTime;
    private float empReadyTime;
    private float toxicReadyTime;

    private const int MaxPulseEnemies = 128;
    private readonly Collider2D[] pulseOverlapResults = new Collider2D[MaxPulseEnemies];
    private readonly HashSet<EnemyHealth> pulseEnemiesHit = new HashSet<EnemyHealth>();

    public bool IsSewerPulseUnlocked => GetFlag(StatNames.SewerPulseUnlocked);
    public bool IsEmpUnlocked => GetFlag(StatNames.EmpShockwaveUnlocked);
    public bool IsToxicFloodUnlocked => GetFlag(StatNames.ToxicFloodUnlocked);
    public float SewerPulseCooldownRemaining => Mathf.Max(0f, sewerPulseReadyTime - Time.time);
    public float EmpCooldownRemaining => Mathf.Max(0f, empReadyTime - Time.time);
    public float ToxicFloodCooldownRemaining => Mathf.Max(0f, toxicReadyTime - Time.time);
    public float SewerPulseCooldownDuration => GetReducedCooldown(
        baseSewerPulseCooldown,
        StatNames.SewerPulseCooldownReduction);
    public float EmpCooldownDuration => GetReducedCooldown(
        baseEmpCooldown,
        StatNames.EmpShockwaveCooldownReduction);
    public float ToxicFloodCooldownDuration => GetReducedCooldown(
        baseToxicCooldown,
        StatNames.ToxicFloodCooldownReduction);

    private void Awake()
    {
        playerHealth = GetComponentInParent<PlayerHealth>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(sewerPulseKey))
            TryActivateSewerPulse();

        if (Input.GetKeyDown(empKey))
            TryActivateEmp();

        if (Input.GetKeyDown(toxicFloodKey))
            TryActivateToxicFlood();
    }

    public bool TryActivateSewerPulse()
    {
        if (!CanActivate(IsSewerPulseUnlocked, sewerPulseReadyTime))
            return false;

        float radius = GetModified(StatNames.SewerPulseRadius, baseSewerPulseRadius);
        float damage = GetModified(StatNames.SewerPulseDamage, baseSewerPulseDamage);
        float pushDistance = GetModified(StatNames.SewerPulseForce, baseSewerPulseForce);
        int hitCount = Physics2D.OverlapCircle(
            transform.position,
            radius,
            ContactFilter2D.noFilter,
            pulseOverlapResults);

        pulseEnemiesHit.Clear();
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = pulseOverlapResults[i];
            pulseOverlapResults[i] = null;
            if (hit == null)
                continue;

            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy == null || !pulseEnemiesHit.Add(enemy))
                continue;

            enemy.TakeDamage(damage, DamageNumberType.Energy);
            enemy.PushAway(transform.position, pushDistance);
        }

        StartCoroutine(PlayExpandingRing("Sewer Pulse Visual", sewerPulseColor, radius, 0.35f));
        BackgroundMusic.PulsarActivated();
        BackgroundMusic.PulsarPulsed();
        sewerPulseReadyTime = Time.time + GetReducedCooldown(
            baseSewerPulseCooldown,
            StatNames.SewerPulseCooldownReduction);
        return true;
    }

    public bool TryActivateEmp()
    {
        if (!CanActivate(IsEmpUnlocked, empReadyTime))
            return false;

        float duration = GetModified(StatNames.EmpShockwaveDuration, baseEmpDuration);
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
                enemies[i].Stun(duration);
        }

        StartCoroutine(PlayExpandingRing("EMP Shockwave Visual", empColor, empVisualRadius, 0.45f));
        empReadyTime = Time.time + GetReducedCooldown(
            baseEmpCooldown,
            StatNames.EmpShockwaveCooldownReduction);
        return true;
    }

    public bool TryActivateToxicFlood()
    {
        if (!CanActivate(IsToxicFloodUnlocked, toxicReadyTime))
            return false;

        float duration = GetModified(StatNames.ToxicFloodDuration, baseToxicDuration);
        float damagePerSecond = GetModified(StatNames.ToxicFloodDamage, baseToxicDamagePerSecond);
        int count = Mathf.Max(1, toxicZoneCount);
        float startingAngle = Random.Range(0f, 360f);

        for (int i = 0; i < count; i++)
        {
            float angle = (startingAngle + i * 360f / count) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 zonePosition = (Vector2)transform.position + direction * toxicZoneDistance;

            GameObject zoneObject = new GameObject($"Toxic Flood Zone {i + 1}");
            zoneObject.transform.position = zonePosition;
            ToxicFloodZone zone = zoneObject.AddComponent<ToxicFloodZone>();
            zone.Initialize(
                duration,
                toxicZoneRadius,
                damagePerSecond,
                toxicTickInterval,
                toxicColor,
                circleSegments,
                lineWidth,
                sortingOrder);
        }

        toxicReadyTime = Time.time + GetReducedCooldown(
            baseToxicCooldown,
            StatNames.ToxicFloodCooldownReduction);
        return true;
    }

    private bool CanActivate(bool unlocked, float readyTime)
    {
        return unlocked && Time.timeScale > 0f && Time.time >= readyTime &&
               (playerHealth == null || !playerHealth.IsStunned);
    }

    private static bool GetFlag(string statName)
    {
        return PlayerStats.instance != null &&
               PlayerStats.instance.GetModifiedValue(statName, 0f) > 0.5f;
    }

    private static float GetModified(string statName, float baseValue)
    {
        return PlayerStats.instance != null
            ? Mathf.Max(0f, PlayerStats.instance.GetModifiedValue(statName, baseValue))
            : baseValue;
    }

    private static float GetReducedCooldown(float baseCooldown, string reductionStat)
    {
        float reduction = PlayerStats.instance != null
            ? PlayerStats.instance.GetPercentModifier(reductionStat)
            : 0f;
        return Mathf.Max(1f, baseCooldown * (1f - Mathf.Clamp(reduction, 0f, 80f) / 100f));
    }

    private IEnumerator PlayExpandingRing(string objectName, Color color, float targetRadius, float duration)
    {
        GameObject visual = new GameObject(objectName);
        visual.transform.SetParent(transform, false);
        LineRenderer line = CreateCircleRenderer(visual, color, 0.1f);
        Material material = line.sharedMaterial;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetCircle(line, Mathf.Lerp(0.1f, targetRadius, progress));

            Color faded = color;
            faded.a *= 1f - progress;
            line.startColor = faded;
            line.endColor = faded;
            yield return null;
        }

        if (material != null)
            Destroy(material);
        Destroy(visual);
    }

    private LineRenderer CreateCircleRenderer(GameObject owner, Color color, float radius)
    {
        LineRenderer line = owner.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.alignment = LineAlignment.View;
        line.positionCount = Mathf.Clamp(circleSegments, 12, 96);
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.startColor = color;
        line.endColor = color;
        line.sortingOrder = sortingOrder;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader != null)
            line.sharedMaterial = new Material(shader);

        SetCircle(line, radius);
        return line;
    }

    private static void SetCircle(LineRenderer line, float radius)
    {
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
    }
}

/// <summary>A temporary poison area spawned by Toxic Flood.</summary>
public sealed class ToxicFloodZone : MonoBehaviour
{
    private const int MaxEnemiesPerTick = 128;

    private readonly Collider2D[] overlapResults = new Collider2D[MaxEnemiesPerTick];
    private readonly HashSet<EnemyHealth> enemiesHit = new HashSet<EnemyHealth>();

    private float remainingDuration;
    private float radius;
    private float damagePerSecond;
    private float tickInterval;
    private float nextTickTime;
    private LineRenderer outline;
    private ParticleSystem particles;
    private Material visualMaterial;

    public void Initialize(
        float duration,
        float zoneRadius,
        float dps,
        float interval,
        Color color,
        int segments,
        float lineWidth,
        int sortingOrder)
    {
        remainingDuration = Mathf.Max(0.1f, duration);
        radius = Mathf.Max(0.1f, zoneRadius);
        damagePerSecond = Mathf.Max(0f, dps);
        tickInterval = Mathf.Max(0.05f, interval);
        nextTickTime = Time.time;

        CreateOutline(color, Mathf.Clamp(segments, 12, 96), lineWidth, sortingOrder);
        CreateParticles(color, sortingOrder);
    }

    private void Update()
    {
        remainingDuration -= Time.deltaTime;
        if (remainingDuration <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time >= nextTickTime)
        {
            DamageEnemies();
            nextTickTime = Time.time + tickInterval;
        }

        float pulse = 0.85f + Mathf.Sin(Time.time * 5f) * 0.08f;
        transform.localScale = new Vector3(pulse, pulse, 1f);
    }

    private void DamageEnemies()
    {
        int count = Physics2D.OverlapCircle(
            transform.position,
            radius,
            ContactFilter2D.noFilter,
            overlapResults);

        enemiesHit.Clear();
        float damageThisTick = damagePerSecond * tickInterval;
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = overlapResults[i];
            overlapResults[i] = null;
            if (hit == null)
                continue;

            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy != null && enemiesHit.Add(enemy))
                enemy.TakeDamage(damageThisTick, DamageNumberType.Poison);
        }
    }

    private void CreateOutline(Color color, int segments, float lineWidth, int sortingOrder)
    {
        outline = gameObject.AddComponent<LineRenderer>();
        outline.useWorldSpace = false;
        outline.loop = true;
        outline.alignment = LineAlignment.View;
        outline.positionCount = segments;
        outline.startWidth = lineWidth;
        outline.endWidth = lineWidth;
        outline.startColor = color;
        outline.endColor = color;
        outline.sortingOrder = sortingOrder;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader != null)
        {
            visualMaterial = new Material(shader);
            outline.sharedMaterial = visualMaterial;
        }

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            outline.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
    }

    private void CreateParticles(Color color, int sortingOrder)
    {
        particles = gameObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(color);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 26f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 1f;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = sortingOrder - 1;
        if (visualMaterial != null)
            renderer.sharedMaterial = visualMaterial;

        particles.Play();
    }

    private void OnDestroy()
    {
        if (visualMaterial != null)
            Destroy(visualMaterial);
    }
}
