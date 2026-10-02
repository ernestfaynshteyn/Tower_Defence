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
    private ParticleSystem[] flameParticles;
    private ParticleSystem.MinMaxCurve[] baseStartLifetimes;

    private void Awake()
    {
        flameParticles = particle != null
            ? particle.GetComponentsInChildren<ParticleSystem>(true)
            : System.Array.Empty<ParticleSystem>();

        baseStartLifetimes = new ParticleSystem.MinMaxCurve[flameParticles.Length];
        for (int i = 0; i < flameParticles.Length; i++)
            baseStartLifetimes[i] = flameParticles[i].main.startLifetime;
    }

    private void OnEnable()
    {
        PlayerStats.OnStatChanged += HandleStatChanged;
    }

    private void OnDisable()
    {
        if (firingAudio != null) firingAudio.Stop();
        PlayerStats.OnStatChanged -= HandleStatChanged;
    }

    private void Start()
    {
        firingAudio = BackgroundMusic.CreateSource(gameObject, true, 0.5f);
        firingAudio.clip = BackgroundMusic.WeaponLoop(true, false);
        RefreshParticleLifetime();
    }
    void Update()
    {
        HandleFlamethrower();
        bool sounding = Time.timeScale > 0f && Input.GetMouseButton(0) && !overheated &&
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
        bool firing = Input.GetMouseButton(0);
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
        if (!Input.GetMouseButton(0) || overheated)
            return;
        EnemyHealth enemy = collision.GetComponent<EnemyHealth>();
        if (enemy != null && (grenade == null || !grenade.isEquipped))
        {
            // Direct damage while inside the flame
            float directDamage = GetModifiedStat(StatNames.AttackDamage, flameDamage);
            enemy.TakeDamage(directDamage * Time.deltaTime);

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
        if (statName == StatNames.BurnDuration)
            RefreshParticleLifetime();
    }

    private void RefreshParticleLifetime()
    {
        float durationMultiplier = PlayerStats.instance != null
            ? 1f + PlayerStats.instance.GetPercentModifier(StatNames.BurnDuration) / 100f
            : 1f;

        for (int i = 0; i < flameParticles.Length; i++)
        {
            if (flameParticles[i] == null)
                continue;

            ParticleSystem.MinMaxCurve lifetime = baseStartLifetimes[i];
            switch (lifetime.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    lifetime.constant *= durationMultiplier;
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    lifetime.constantMin *= durationMultiplier;
                    lifetime.constantMax *= durationMultiplier;
                    break;
                case ParticleSystemCurveMode.Curve:
                case ParticleSystemCurveMode.TwoCurves:
                    lifetime.curveMultiplier *= durationMultiplier;
                    break;
            }

            ParticleSystem.MainModule main = flameParticles[i].main;
            main.startLifetime = lifetime;
        }
    }
}
