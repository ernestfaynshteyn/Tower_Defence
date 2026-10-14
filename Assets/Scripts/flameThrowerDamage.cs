using UnityEngine;
public class flameThrowerDamage : MonoBehaviour
{
    [Range(1, 1000)]
    [SerializeField] private float flameDamage;
    [SerializeField] private float maxHeat = 100f;
    [SerializeField] private float currentHeat = 0f;
    [SerializeField] private float heatPerSecond = 25f;
    [SerializeField] private float coolingRate = 40f;
    [SerializeField] private ParticleSystem particle;
    [Header("Burn")]
    [SerializeField] private float burnDuration = 0.5f;
    [SerializeField] private float burnDamage = 2f;
    public UseGrenade grenade;
    private bool overheated = false;
    private AudioSource firingAudio;
    private PlayerHealth playerHealth;
    private SmoothMouseFollow autoAim;
    private ParticleSystem[] flameParticles;
    private ParticleSystem.MinMaxCurve[] baseStartLifetimes;
    private PolygonCollider2D flameCollider;
    private Vector2[][] baseColliderPaths;
    private bool rangeDataCached;
    private bool externalVisualPlaying;
    private float appliedRangeMultiplier = -1f;

    public ParticleSystem ParticleEffect => particle;

    private void Awake()
    {
        playerHealth = GetComponentInParent<PlayerHealth>();
        autoAim = GetComponentInParent<SmoothMouseFollow>();
        CacheRangeData();
        StopParticleEffect(true);
    }

    private void CacheRangeData()
    {
        if (rangeDataCached)
            return;

        rangeDataCached = true;
        flameParticles = particle != null
            ? particle.GetComponentsInChildren<ParticleSystem>(true)
            : System.Array.Empty<ParticleSystem>();

        baseStartLifetimes = new ParticleSystem.MinMaxCurve[flameParticles.Length];
        for (int i = 0; i < flameParticles.Length; i++)
            baseStartLifetimes[i] = flameParticles[i].main.startLifetime;

        flameCollider = GetComponent<PolygonCollider2D>();
        if (flameCollider != null)
        {
            baseColliderPaths = new Vector2[flameCollider.pathCount][];
            for (int i = 0; i < flameCollider.pathCount; i++)
                baseColliderPaths[i] = flameCollider.GetPath(i);
        }
    }

    private void OnEnable()
    {
        StopParticleEffect(true);
        PlayerStats.OnStatChanged += HandleStatChanged;
    }

    private void OnDisable()
    {
        if (firingAudio != null) firingAudio.Stop();
        StopParticleEffect(true);
        PlayerStats.OnStatChanged -= HandleStatChanged;
    }

    private void Start()
    {
        firingAudio = BackgroundMusic.CreateSource(gameObject, true, 0.5f);
        firingAudio.clip = BackgroundMusic.WeaponLoop(true, false);
        RefreshRange();
    }
    void Update()
    {
        HandleFlamethrower();
        bool sounding = Time.timeScale > 0f && WantsToFire() && !overheated &&
            (playerHealth == null || !playerHealth.IsStunned) &&
            (grenade == null || !grenade.isEquipped);
        if (firingAudio != null)
        {
            if (sounding && firingAudio.clip != null && !firingAudio.isPlaying) firingAudio.Play();
            else if (!sounding && firingAudio.isPlaying) firingAudio.Stop();
        }
        HealthBar.SetOverheat(currentHeat, maxHeat);
    }
    private void HandleFlamethrower()
    {
        bool firing = WantsToFire() &&
            (playerHealth == null || !playerHealth.IsStunned) &&
            (grenade == null || !grenade.isEquipped);
        // FIRING
        if (firing && !overheated)
        {
            if (particle != null && !particle.isPlaying)
                particle.Play();
            currentHeat += heatPerSecond * Time.deltaTime;
            if (currentHeat >= maxHeat)
            {
                currentHeat = maxHeat;
                overheated = true;
                BackgroundMusic.Overheated(true);
                if (particle != null)
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
        // COOLING
        else
        {
            if (particle != null && particle.isPlaying)
                particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            currentHeat -= coolingRate * Time.deltaTime;
            currentHeat = Mathf.Clamp(currentHeat, 0, maxHeat);
        }
        // RECOVER FROM OVERHEAT
        if (overheated && currentHeat <= 0)
        {
            overheated = false;
        }
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!WantsToFire() || overheated || (playerHealth != null && playerHealth.IsStunned))
            return;
        EnemyHealth enemy = collision.GetComponentInParent<EnemyHealth>();
        if (enemy != null && (grenade == null || !grenade.isEquipped))
        {
            // Direct damage while inside the flame
            float directDamage = GetModifiedStat(StatNames.AttackDamage, flameDamage);
            enemy.TakeDamage(directDamage * Time.deltaTime, DamageNumberType.Fire);

            // Apply/refresh burn DoT (only the flamethrower calls this)
            enemy.ApplyBurn(
                GetModifiedStat(StatNames.BurnDuration, burnDuration),
                GetModifiedStat(StatNames.BurnRate, burnDamage));
        }
    }

    private float GetModifiedStat(string statName, float baseValue)
    {
        return PlayerStats.instance != null
            ? PlayerStats.instance.GetModifiedValue(statName, baseValue)
            : baseValue;
    }

    private void HandleStatChanged(string statName, float oldValue, float newValue)
    {
        if (statName == StatNames.Range)
            RefreshRange();
    }

    private void RefreshRange()
    {
        float rangeMultiplier = PlayerStats.instance != null
            ? 1f + PlayerStats.instance.GetPercentModifier(StatNames.Range) / 100f
            : 1f;
        ApplyRangeMultiplier(rangeMultiplier);
    }

    private bool WantsToFire()
    {
        return GlobalData.AutomaticPrimaryAiming
            ? autoAim != null && autoAim.HasAutomaticTarget
            : Input.GetMouseButton(0);
    }

    /// <summary>
    /// Lets another weapon controller reuse this authored Particle System
    /// without enabling this component's heat or damage logic.
    /// </summary>
    public void SetExternalVisual(bool shouldPlay, float rangeMultiplier)
    {
        CacheRangeData();
        ApplyRangeMultiplier(rangeMultiplier);

        if (shouldPlay == externalVisualPlaying || particle == null)
            return;

        externalVisualPlaying = shouldPlay;
        if (shouldPlay)
            particle.Play(true);
        else
            particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    public void StopParticleEffect(bool clearParticles)
    {
        externalVisualPlaying = false;
        if (particle == null)
            return;

        particle.Stop(
            true,
            clearParticles
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting);
    }

    private void ApplyRangeMultiplier(float rangeMultiplier)
    {
        CacheRangeData();
        rangeMultiplier = Mathf.Max(0.01f, rangeMultiplier);
        if (Mathf.Approximately(appliedRangeMultiplier, rangeMultiplier))
            return;

        appliedRangeMultiplier = rangeMultiplier;

        for (int i = 0; i < flameParticles.Length; i++)
        {
            if (flameParticles[i] == null)
                continue;

            ParticleSystem.MinMaxCurve lifetime = baseStartLifetimes[i];
            switch (lifetime.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    lifetime.constant *= rangeMultiplier;
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    lifetime.constantMin *= rangeMultiplier;
                    lifetime.constantMax *= rangeMultiplier;
                    break;
                case ParticleSystemCurveMode.Curve:
                case ParticleSystemCurveMode.TwoCurves:
                    lifetime.curveMultiplier *= rangeMultiplier;
                    break;
            }

            ParticleSystem.MainModule main = flameParticles[i].main;
            main.startLifetime = lifetime;
        }

        // Match the damaging cone to the visible flame. Only the local forward
        // axis grows, so wider range does not also make the flame unnaturally wide.
        if (flameCollider != null && baseColliderPaths != null)
        {
            for (int pathIndex = 0; pathIndex < baseColliderPaths.Length; pathIndex++)
            {
                Vector2[] points = (Vector2[])baseColliderPaths[pathIndex].Clone();
                for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
                    points[pointIndex].x *= rangeMultiplier;

                flameCollider.SetPath(pathIndex, points);
            }
        }
    }
}
