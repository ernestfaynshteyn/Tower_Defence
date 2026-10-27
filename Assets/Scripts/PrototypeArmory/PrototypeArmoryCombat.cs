using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum PrototypePrimaryWeapon
{
    RapidBullets,
    PiercingLaser,
    ShotgunBurst,
    Flamethrower,
    LightningArc,
    ToxicSpitter
}

public enum PrototypeAttachment
{
    MiniTurretSatellites,
    SpikeBarrier,
    OverchargeModule,
    FreezeBurst,
    ReflectorDome
}

public enum PrototypeWeaponUpgrade
{
    Damage,
    Range,
    FireRate,
    Cooling,
    SpecialOne,
    SpecialTwo,
    SpecialThree
}

[DisallowMultipleComponent]
public sealed class PrototypeArmoryCombat : MonoBehaviour
{
    private sealed class WeaponState
    {
        public readonly Dictionary<PrototypeWeaponUpgrade, int> UpgradeLevels =
            new Dictionary<PrototypeWeaponUpgrade, int>();

        public float Heat;
        public bool Overheated;

        public WeaponState()
        {
            foreach (PrototypeWeaponUpgrade upgrade in Enum.GetValues(typeof(PrototypeWeaponUpgrade)))
                UpgradeLevels[upgrade] = 0;
        }
    }

    private sealed class TimedLineEffect
    {
        public GameObject Object;
        public LineRenderer Renderer;
        public float ReleaseAt;
    }

    private sealed class TimedParticleEffect
    {
        public GameObject Object;
        public ParticleSystem Particles;
        public float ReleaseAt;
        public bool ReleaseScheduled;
    }

    private readonly struct WeaponProfile
    {
        public readonly string DisplayName;
        public readonly float Damage;
        public readonly float Range;
        public readonly float FireDelay;
        public readonly float MaxHeat;
        public readonly float HeatPerShot;
        public readonly float CoolingPerSecond;
        public readonly int Pierce;

        public WeaponProfile(string displayName, float damage, float range, float fireDelay,
            float maxHeat, float heatPerShot, float coolingPerSecond, int pierce)
        {
            DisplayName = displayName;
            Damage = damage;
            Range = range;
            FireDelay = fireDelay;
            MaxHeat = maxHeat;
            HeatPerShot = heatPerShot;
            CoolingPerSecond = coolingPerSecond;
            Pierce = pierce;
        }
    }

    public event Action StateChanged;

    public PrototypePrimaryWeapon ActivePrimary { get; private set; }

    private const string LegacyProgressSaveKey = "PrototypeArmory.Progress.v1";
    private const float BulletMultishotSpread = 10f;
    private const float LaserMultishotSpread = 8f;
    private const float ShotgunMultishotSpread = 8f;
    private const float FlamethrowerMultishotSpread = 28f;
    private const float LightningMultishotSpread = 20f;
    private const float OverchargeCooldown = 15f;
    private const float FreezeBurstCooldown = 10f;

    private readonly HashSet<PrototypePrimaryWeapon> ownedPrimaries = new HashSet<PrototypePrimaryWeapon>();
    private readonly HashSet<PrototypePrimaryWeapon> purchasedPrimaries = new HashSet<PrototypePrimaryWeapon>();
    private readonly Dictionary<PrototypeAttachment, int> attachmentLevels = new Dictionary<PrototypeAttachment, int>();
    private readonly Dictionary<PrototypePrimaryWeapon, WeaponState> weaponStates =
        new Dictionary<PrototypePrimaryWeapon, WeaponState>();
    private readonly List<GameObject> satellites = new List<GameObject>();
    private readonly List<GameObject> runtimeObjects = new List<GameObject>();
    private readonly List<TimedLineEffect> lineEffectPool = new List<TimedLineEffect>();
    private readonly List<TimedParticleEffect> particleEffectPool = new List<TimedParticleEffect>();
    private readonly List<RaycastHit2D> raycastHits = new List<RaycastHit2D>(64);
    private readonly List<Collider2D> overlapColliders = new List<Collider2D>(128);
    private readonly HashSet<EnemyHealth> uniqueEnemySearchResults = new HashSet<EnemyHealth>();
    private readonly List<EnemyHealth> enemySearchResults = new List<EnemyHealth>(64);
    private readonly Dictionary<Color32, Gradient> particleGradients = new Dictionary<Color32, Gradient>();
    private readonly HashSet<EnemyHealth> combustionTrackedEnemies = new HashSet<EnemyHealth>();

    private PlayerHealth playerHealth;
    private UseGrenade grenadeController;
    private Camera mainCamera;
    private RangeIndicatorScaler rangeIndicator;
    private LineRenderer weaponVisual;
    private SpriteRenderer equippedWeaponSprite;
    private PrototypePrimaryWeapon displayedWeapon;
    private LineRenderer spikeVisual;
    private LineRenderer reflectorVisual;
    private flameThrowerDamage originalFlamethrower;
    private GameObject originalFlamethrowerObject;
    private AudioSource weaponLoopSource;
    private PrototypePrimaryWeapon loopWeapon;
    private float nextPrimaryFireTime;
    private float nextSatelliteFireTime;
    private float nextSpikeDamageTime;
    private float nextFreezeBurstTime;
    private float overchargeEndsAt;
    private float overchargeReadyAt;
    private EnemyHealth automaticPrimaryTarget;
    private float lastLaserPulseTime = float.NegativeInfinity;
    private float laserFocusBonus;
    private int rapidBulletCounter;
    private bool initialized;
    private PrototypePrimaryWeapon startingPrimary;
    private ContactFilter2D physicsContactFilter;

    private static Material sharedLineMaterial;
    private static Sprite sharedCircleSprite;

    public bool IsOverchargeUnlocked =>
        GetAttachmentLevel(PrototypeAttachment.OverchargeModule) > 0;
    public float OverchargeCooldownRemaining => IsOverchargeUnlocked
        ? Mathf.Max(0f, overchargeReadyAt - Time.time)
        : 0f;
    public float OverchargeCooldownDuration => OverchargeCooldown;
    public bool IsFreezeBurstUnlocked =>
        GetAttachmentLevel(PrototypeAttachment.FreezeBurst) > 0;
    public float FreezeBurstCooldownRemaining => IsFreezeBurstUnlocked
        ? Mathf.Max(0f, nextFreezeBurstTime - Time.time)
        : 0f;
    public float FreezeBurstCooldownDuration => FreezeBurstCooldown;

    public void Initialize(PlayerHealth targetPlayer)
    {
        if (initialized || targetPlayer == null)
            return;

        initialized = true;
        playerHealth = targetPlayer;
        grenadeController = targetPlayer.GetComponentInChildren<UseGrenade>(true);
        mainCamera = Camera.main;
        rangeIndicator = targetPlayer.GetComponent<RangeIndicatorScaler>();
        weaponLoopSource = BackgroundMusic.CreateSource(gameObject, true, 0.3f);
        physicsContactFilter = ContactFilter2D.noFilter;
        physicsContactFilter.useTriggers = Physics2D.queriesHitTriggers;

        foreach (Shottingscript weapon in targetPlayer.GetComponentsInChildren<Shottingscript>(true))
            weapon.enabled = false;
        foreach (flameThrowerDamage flame in targetPlayer.GetComponentsInChildren<flameThrowerDamage>(true))
        {
            if (originalFlamethrower == null)
            {
                originalFlamethrower = flame;
                originalFlamethrowerObject = flame.gameObject;
            }

            flame.StopParticleEffect(true);
            flame.enabled = false;
        }

        WeaponSelected oldSelection = targetPlayer.GetComponentInChildren<WeaponSelected>(true);
        if (oldSelection != null)
        {
            if (oldSelection.flamethrower != null) oldSelection.flamethrower.SetActive(false);
            if (oldSelection.gun1 != null) oldSelection.gun1.SetActive(false);
            if (oldSelection.gun2 != null) oldSelection.gun2.SetActive(false);
            if (oldSelection.gun3 != null) oldSelection.gun3.SetActive(false);
        }

        startingPrimary = MapStartingWeapon();
        ActivePrimary = startingPrimary;
        foreach (PrototypePrimaryWeapon weapon in Enum.GetValues(typeof(PrototypePrimaryWeapon)))
            weaponStates[weapon] = new WeaponState();
        foreach (PrototypeAttachment attachment in Enum.GetValues(typeof(PrototypeAttachment)))
            attachmentLevels[attachment] = 0;

        // Armory purchases belong to one gameplay run. Permanent gem-tree
        // upgrades are owned by SkillTreeScript and are intentionally untouched.
        ClearLegacySavedProgress();
        CurrencyManager.Instance?.ResetToStartingMoney();
        ownedPrimaries.Add(startingPrimary);
        PlayerStats.OnStatChanged += HandlePlayerStatChanged;
        UpdateRangeIndicatorForActiveWeapon();

        weaponVisual = CreatePersistentLine("Prototype Primary Weapon", 0.1f, 0.055f, 14);
        int satelliteCount = GetAttachmentLevel(PrototypeAttachment.MiniTurretSatellites);
        for (int i = 0; i < satelliteCount; i++)
            CreateSatellite();
        if (GetAttachmentLevel(PrototypeAttachment.FreezeBurst) > 0)
            nextFreezeBurstTime = Time.time + FreezeBurstCooldown;
        UpdatePersistentVisuals();
    }

    private PrototypePrimaryWeapon MapStartingWeapon()
    {
        switch (GlobalData.ActiveWeapon)
        {
            case Weapon.flamethrower: return PrototypePrimaryWeapon.Flamethrower;
            case Weapon.gun2: return PrototypePrimaryWeapon.ShotgunBurst;
            case Weapon.gun3: return PrototypePrimaryWeapon.PiercingLaser;
            case Weapon.piercingLaser: return PrototypePrimaryWeapon.PiercingLaser;
            case Weapon.lightningArc: return PrototypePrimaryWeapon.LightningArc;
            case Weapon.toxicSpitter: return PrototypePrimaryWeapon.ToxicSpitter;
            default: return PrototypePrimaryWeapon.RapidBullets;
        }
    }

    private void Update()
    {
        if (!initialized || playerHealth == null)
            return;

        UpdateEffectPools();
        UpdateSatellites();
        UpdateAttachments();
        UpdateAutomaticPrimaryTarget();
        UpdateWeaponVisual();

        bool wantsToFire = CanUsePrimary() && WantsToFirePrimary();
        UpdateWeaponAudio(wantsToFire && !GetWeaponState(ActivePrimary).Overheated);
        UpdateWeaponHeat(wantsToFire);

        if (GetAttachmentLevel(PrototypeAttachment.OverchargeModule) > 0 && CanUsePrimary() &&
            Input.GetKeyDown(KeyCode.R) && Time.time >= overchargeReadyAt)
        {
            ActivateOvercharge();
        }

        if (wantsToFire && !GetWeaponState(ActivePrimary).Overheated && Time.time >= nextPrimaryFireTime)
            FirePrimary();

        bool showOriginalFlame = wantsToFire &&
            ActivePrimary == PrototypePrimaryWeapon.Flamethrower &&
            !GetWeaponState(ActivePrimary).Overheated;
        UpdateOriginalFlamethrowerVisual(showOriginalFlame);

        WeaponProfile activeProfile = GetProfile(ActivePrimary);
        HealthBar.SetOverheat(GetWeaponState(ActivePrimary).Heat, GetMaxHeat(activeProfile));
    }

    private bool CanUsePrimary()
    {
        if (Time.timeScale <= 0f || playerHealth.IsStunned)
            return false;
        if (grenadeController != null && grenadeController.isEquipped)
            return false;
        return GlobalData.AutomaticPrimaryAiming ||
               EventSystem.current == null ||
               !EventSystem.current.IsPointerOverGameObject();
    }

    private bool WantsToFirePrimary()
    {
        return GlobalData.AutomaticPrimaryAiming
            ? automaticPrimaryTarget != null
            : Input.GetMouseButton(0);
    }

    private void UpdateAutomaticPrimaryTarget()
    {
        if (!GlobalData.AutomaticPrimaryAiming || playerHealth == null)
        {
            automaticPrimaryTarget = null;
            return;
        }

        WeaponProfile profile = GetProfile(ActivePrimary);
        float range = GetWeaponRadius(profile.Range);
        automaticPrimaryTarget = FindNearestEnemyWithinIndicator(
            playerHealth.transform.position,
            range,
            null);
    }

    private void FirePrimary()
    {
        Vector2 origin = playerHealth.transform.position;
        Vector2 aim = GetAimDirection(origin);
        WeaponProfile profile = GetProfile(ActivePrimary);
        float multishotChance = GetChance(StatNames.MultishotChance);
        float multishotSide = UnityEngine.Random.value < 0.5f ? -1f : 1f;

        BackgroundMusic.PrototypeWeaponShot(ActivePrimary);

        switch (ActivePrimary)
        {
            case PrototypePrimaryWeapon.PiercingLaser:
                FirePiercingLaser(origin, aim, profile);
                if (RollMultishot(multishotChance))
                    FirePiercingLaser(origin, RotateDirection(aim, multishotSide * LaserMultishotSpread), profile);
                break;
            case PrototypePrimaryWeapon.ShotgunBurst:
                FireShotgun(origin, aim, profile, multishotChance);
                break;
            case PrototypePrimaryWeapon.Flamethrower:
                FireFlamethrower(origin, aim, profile);
                if (RollMultishot(multishotChance))
                    FireFlamethrower(origin, RotateDirection(aim, multishotSide * FlamethrowerMultishotSpread), profile);
                break;
            case PrototypePrimaryWeapon.LightningArc:
                bool lightningMultishot = RollMultishot(multishotChance);
                HashSet<EnemyHealth> lightningVolleyHits = lightningMultishot
                    ? new HashSet<EnemyHealth>()
                    : null;
                FireLightning(origin, aim, profile, lightningVolleyHits);
                if (lightningMultishot)
                {
                    FireLightning(origin, RotateDirection(aim, multishotSide * LightningMultishotSpread),
                        profile, lightningVolleyHits);
                }
                break;
            case PrototypePrimaryWeapon.ToxicSpitter:
                Vector2 toxicTarget = GetPrimaryAimPosition(origin);
                FireToxicBomb(origin, toxicTarget, profile);
                if (RollMultishot(multishotChance))
                {
                    Vector2 targetDirection = toxicTarget - origin;
                    targetDirection = targetDirection.sqrMagnitude > 0.001f ? targetDirection.normalized : aim;
                    Vector2 perpendicular = new Vector2(-targetDirection.y, targetDirection.x);
                    float targetSpacing = GetToxicSplashRadius() * 1.15f;
                    FireToxicBomb(origin, toxicTarget + perpendicular * (multishotSide * targetSpacing), profile);
                }
                break;
            default:
                FireRapidBullet(origin, aim, profile);
                if (RollMultishot(multishotChance))
                    FireRapidBullet(origin, RotateDirection(aim, multishotSide * BulletMultishotSpread), profile);
                break;
        }

        nextPrimaryFireTime = Time.time + GetFireDelay(profile.FireDelay);
        AddHeat(profile.HeatPerShot);
    }

    private void FireRapidBullet(Vector2 origin, Vector2 direction, WeaponProfile profile,
        bool allowSpecialCycle = true)
    {
        bool tracerRound = false;
        bool bulletStorm = false;
        if (allowSpecialCycle)
        {
            rapidBulletCounter++;
            tracerRound = HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialTwo) && rapidBulletCounter % 6 == 0;
            bulletStorm = HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialThree) && rapidBulletCounter % 20 == 0;
        }

        float range = GetWeaponDistance(profile.Range, origin, direction);
        Vector2 end = origin + direction * range;
        Color bulletColor = tracerRound ? new Color(1f, 0.25f, 0.08f) : new Color(1f, 0.9f, 0.25f);
        CreateTimedLine(tracerRound ? "Tracer Round" : "Rapid Bullet", origin, end,
            bulletColor, tracerRound ? 0.065f : 0.035f, 0.055f);
        CreateDirectionalParticles("Rapid Bullet Sparks", origin, direction,
            bulletColor, tracerRound ? 9 : 5, 2.8f, 0.1f, 0.18f, 10f);

        int targetsRemaining = 1 + GetPierceCount(profile);
        HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();
        foreach (RaycastHit2D hit in GetRaycastHits(origin, direction, range))
        {
            EnemyHealth enemy = hit.collider != null ? hit.collider.GetComponentInParent<EnemyHealth>() : null;
            if (enemy == null || !hitEnemies.Add(enemy))
                continue;

            float dealt = DealWeaponDamage(enemy, profile.Damage * (tracerRound ? 3f : 1f));
            TryReflect(enemy, dealt);
            if (--targetsRemaining <= 0)
                break;
        }

        if (bulletStorm)
        {
            CreateRingEffect(origin, 0.75f, new Color(1f, 0.9f, 0.25f), 0.18f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                Vector2 stormDirection = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                FireRapidBullet(origin, stormDirection, profile, false);
            }
        }
    }

    private void FirePiercingLaser(Vector2 origin, Vector2 direction, WeaponProfile profile)
    {
        float range = GetWeaponDistance(profile.Range, origin, direction);
        Vector2 end = origin + direction * range;
        Color laserColor = new Color(0.2f, 0.95f, 1f);
        bool wideBeam = HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialTwo);
        CreateTimedLine("Piercing Laser", origin, end, laserColor,
            wideBeam ? 0.32f : 0.1f, profile.FireDelay * 1.35f);
        CreateLineParticles("Piercing Laser Particles", origin, end, laserColor, 22, 0.09f, 0.14f);

        float pulseDamage = profile.Damage;
        if (HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialOne))
        {
            float timeSinceLastPulse = Time.time - lastLaserPulseTime;
            if (timeSinceLastPulse > 0.22f)
                laserFocusBonus = 0f;
            else if (timeSinceLastPulse > 0.001f)
                laserFocusBonus = Mathf.Min(0.5f, laserFocusBonus + 0.04f);
            pulseDamage *= 1f + laserFocusBonus;
            lastLaserPulseTime = Time.time;
        }

        HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();
        EnemyHealth last = null;
        float lastDamage = 0f;
        int targetsRemaining = Mathf.Max(1, GetPierceCount(profile));
        List<RaycastHit2D> laserHits = wideBeam
            ? GetCircleCastHits(origin, direction, range, 0.22f)
            : GetRaycastHits(origin, direction, range);
        foreach (RaycastHit2D hit in laserHits)
        {
            EnemyHealth enemy = hit.collider != null ? hit.collider.GetComponentInParent<EnemyHealth>() : null;
            if (enemy == null || !hitEnemies.Add(enemy))
                continue;
            float hitDamage = pulseDamage;
            if (HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialThree) &&
                !enemy.isBoss && enemy.MaxHealth > 0f && enemy.CurrentHealth <= enemy.MaxHealth * 0.15f)
            {
                hitDamage = Mathf.Max(hitDamage, enemy.CurrentHealth);
            }
            lastDamage = DealWeaponDamage(enemy, hitDamage, DamageNumberType.Energy);
            last = enemy;
            if (--targetsRemaining <= 0)
                break;
        }
        if (last != null) TryReflect(last, lastDamage);
    }

    private void FireShotgun(Vector2 origin, Vector2 direction, WeaponProfile profile, float multishotChance)
    {
        const int pelletCount = 7;
        float halfSpread = HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialOne) ? 11.5f : 19f;
        CreateDirectionalParticles("Shotgun Muzzle Particles", origin, direction,
            new Color(1f, 0.65f, 0.2f), 14, 3.2f, 0.12f, 0.2f, 22f);
        for (int i = 0; i < pelletCount; i++)
        {
            float angle = Mathf.Lerp(-halfSpread, halfSpread, i / (pelletCount - 1f));
            Vector2 pelletDirection = Quaternion.Euler(0f, 0f, angle) * direction;
            FireShotgunPellet(origin, pelletDirection, profile);

            // Each pellet rolls independently. Bonus pellets spread outward so
            // they cover new space instead of drawing directly over the original.
            if (RollMultishot(multishotChance))
            {
                float outwardSide = Mathf.Abs(angle) > 0.01f
                    ? Mathf.Sign(angle)
                    : (UnityEngine.Random.value < 0.5f ? -1f : 1f);
                Vector2 bonusDirection = RotateDirection(pelletDirection,
                    outwardSide * ShotgunMultishotSpread);
                FireShotgunPellet(origin, bonusDirection, profile);
            }
        }

        if (HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialThree))
            FireShotgunSlug(origin, direction, profile);
    }

    private void FireShotgunPellet(Vector2 origin, Vector2 direction, WeaponProfile profile)
    {
        float range = GetWeaponDistance(profile.Range, origin, direction);
        Vector2 end = origin + direction * range;
        CreateTimedLine("Shotgun Pellet", origin, end, new Color(1f, 0.65f, 0.2f), 0.025f, 0.045f);

        int targetsRemaining = 1 + GetPierceCount(profile);
        HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();
        foreach (RaycastHit2D hit in GetRaycastHits(origin, direction, range))
        {
            EnemyHealth enemy = hit.collider != null ? hit.collider.GetComponentInParent<EnemyHealth>() : null;
            if (enemy == null || !hitEnemies.Add(enemy))
                continue;
            float distance = Vector2.Distance(origin, enemy.transform.position);
            float falloff = Mathf.Lerp(1f, 0.45f, Mathf.Clamp01(distance / Mathf.Max(0.01f, range)));
            float dealt = DealWeaponDamage(enemy, profile.Damage * falloff);
            TryReflect(enemy, dealt);
            if (HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialTwo))
                enemy.PushAway(origin, 0.55f);
            if (--targetsRemaining <= 0)
                break;
        }

    }

    private void FireShotgunSlug(Vector2 origin, Vector2 direction, WeaponProfile profile)
    {
        float range = GetWeaponDistance(profile.Range, origin, direction);
        Vector2 end = origin + direction * range;
        CreateTimedLine("Shotgun Center Slug", origin, end,
            new Color(1f, 0.82f, 0.32f), 0.075f, 0.08f);

        foreach (RaycastHit2D hit in GetRaycastHits(origin, direction, range))
        {
            EnemyHealth enemy = hit.collider != null ? hit.collider.GetComponentInParent<EnemyHealth>() : null;
            if (enemy == null)
                continue;

            float distance = Vector2.Distance(origin, enemy.transform.position);
            float falloff = Mathf.Lerp(1f, 0.65f, Mathf.Clamp01(distance / Mathf.Max(0.01f, range)));
            float dealt = DealWeaponDamage(enemy, profile.Damage * 2.2f * falloff);
            TryReflect(enemy, dealt);
            break;
        }
    }

    private void FireFlamethrower(Vector2 origin, Vector2 direction, WeaponProfile profile)
    {
        float range = GetWeaponDistance(profile.Range, origin, direction);
        bool longBurn = HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialOne);
        bool wideSpread = HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialTwo);
        bool combustion = HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialThree);
        float halfAngle = wideSpread ? 36f : 24f;
        if (originalFlamethrower == null)
        {
            Color fallbackColor = new Color(1f, 0.25f, 0.04f, 0.8f);
            CreateDirectionalParticles("Flamethrower Fallback", origin, direction, fallbackColor,
                12, range / 0.32f, 0.32f, 0.2f, halfAngle);
        }

        foreach (EnemyHealth enemy in FindEnemies(origin, range))
        {
            Vector2 toEnemy = (Vector2)enemy.transform.position - origin;
            if (Vector2.Angle(direction, toEnemy) > halfAngle)
                continue;

            if (combustion)
                TrackCombustion(enemy);
            DealWeaponDamage(enemy, profile.Damage, DamageNumberType.Fire);
            float burnDuration = GetModifiedStat(StatNames.BurnDuration, longBurn ? 3f : 1f);
            float burnDamage = GetModifiedStat(StatNames.BurnRate,
                GetWeaponDamage(3f));
            enemy.ApplyBurn(burnDuration, burnDamage);
        }
    }

    private void TrackCombustion(EnemyHealth enemy)
    {
        if (enemy == null || !combustionTrackedEnemies.Add(enemy))
            return;

        enemy.Died += HandleCombustionDeath;
    }

    private void HandleCombustionDeath(EnemyHealth enemy)
    {
        if (enemy == null)
            return;

        enemy.Died -= HandleCombustionDeath;
        combustionTrackedEnemies.Remove(enemy);
        StartCoroutine(CombustionRoutine(enemy.transform.position));
    }

    private IEnumerator CombustionRoutine(Vector2 position)
    {
        // The death event can occur while the flamethrower is iterating the
        // shared overlap results, so defer the new overlap query by one frame.
        yield return null;

        const float radius = 1.65f;
        CreateRingEffect(position, radius, new Color(1f, 0.2f, 0.03f), 0.2f);
        CreateRadialParticles("Flame Combustion", position,
            new Color(1f, 0.25f, 0.04f), 28, 2.8f, 0.16f, 0.4f);

        int damageLevel = GetWeaponUpgradeLevel(
            PrototypePrimaryWeapon.Flamethrower, PrototypeWeaponUpgrade.Damage);
        float damage = GetWeaponDamageAtLevel(18f, PrototypePrimaryWeapon.Flamethrower, damageLevel);
        foreach (EnemyHealth nearby in FindEnemies(position, radius))
        {
            nearby.TakeDamage(damage, DamageNumberType.Fire);
            nearby.ApplyBurn(1.25f, GetWeaponDamageAtLevel(
                2.5f, PrototypePrimaryWeapon.Flamethrower, damageLevel));
        }
    }

    private void ClearCombustionTracking()
    {
        foreach (EnemyHealth enemy in combustionTrackedEnemies)
        {
            if (enemy != null)
                enemy.Died -= HandleCombustionDeath;
        }
        combustionTrackedEnemies.Clear();
    }

    private void FireLightning(Vector2 origin, Vector2 direction, WeaponProfile profile,
        HashSet<EnemyHealth> volleyHits)
    {
        float playerRange = GetWeaponRadius(profile.Range);
        EnemyHealth current = FindAimTarget(origin, direction, playerRange, 32f, volleyHits);
        if (current == null && volleyHits != null && volleyHits.Count > 0)
            current = FindAimTarget(origin, direction, playerRange, 32f, null);
        if (current == null)
        {
            CreateDirectionalParticles("Lightning Charge", origin, direction,
                new Color(0.95f, 0.85f, 0.12f), 8, 2.2f, 0.12f, 0.12f, 35f);
            return;
        }

        HashSet<EnemyHealth> hit = new HashSet<EnemyHealth>();
        Vector2 start = origin;
        float damage = profile.Damage;
        int chainCount = Mathf.Max(1, GetPierceCount(profile));
        EnemyHealth firstHit = null;
        EnemyHealth lastHit = null;
        float lastChainDamage = 0f;
        for (int chain = 0; chain < chainCount && current != null; chain++)
        {
            if (chain > 0)
                BackgroundMusic.LightningChained();

            hit.Add(current);
            if (firstHit == null)
                firstHit = current;
            lastHit = current;
            if (volleyHits != null)
                volleyHits.Add(current);
            Color lightningColor = new Color(0.95f, 0.85f, 0.12f);
            CreateTimedLine("Lightning Arc", start, current.transform.position,
                lightningColor, 0.075f, 0.15f);
            CreateLineParticles("Lightning Arc Particles", start, current.transform.position,
                lightningColor, 10, 0.11f, 0.16f);
            DamageNumberType damageType = chain == 0
                ? DamageNumberType.Normal
                : DamageNumberType.ElectricChain;
            float dealt = DealWeaponDamage(current, damage, damageType);
            lastChainDamage = damage;
            current.RecordStatusEffect(StatusEffectRecord.Shocked);
            if (HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialTwo))
                current.Stun(0.3f);
            if (chain == 0) TryReflect(current, dealt);
            start = current.transform.position;
            float jumpRange = HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialOne) ? 4.6f : 3.4f;
            current = FindNearestEnemyWithinIndicator(start, ApplyWeaponRange(jumpRange), hit, volleyHits);
            damage *= 0.82f;
        }

        if (firstHit != null && lastHit != null && firstHit != lastHit &&
            HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialThree))
        {
            DealWeaponDamage(firstHit, lastChainDamage * 0.75f, DamageNumberType.ElectricChain);
            CreateTimedLine("Lightning Feedback Loop", lastHit.transform.position, firstHit.transform.position,
                new Color(1f, 0.95f, 0.35f), 0.085f, 0.18f);
            CreateLineParticles("Lightning Feedback Particles", lastHit.transform.position,
                firstHit.transform.position, new Color(1f, 0.95f, 0.35f), 12, 0.12f, 0.18f);
        }
    }

    private void FireToxicBomb(Vector2 origin, Vector2 requestedTarget, WeaponProfile profile)
    {
        Vector2 target = requestedTarget;
        float splashRadius = GetToxicSplashRadius();
        float range = GetWeaponRadius(profile.Range);
        if (rangeIndicator != null && rangeIndicator.WorldRadius > 0f)
            range = Mathf.Min(range, Mathf.Max(0f, rangeIndicator.WorldRadius - splashRadius));
        Vector2 delta = target - origin;
        if (delta.magnitude > range)
            target = origin + delta.normalized * range;
        StartCoroutine(ToxicBombRoutine(origin, target, splashRadius, profile.Damage));
    }

    private float GetToxicSplashRadius()
    {
        return HasWeaponUpgrade(PrototypePrimaryWeapon.ToxicSpitter, PrototypeWeaponUpgrade.SpecialTwo)
            ? 2.9f
            : 2f;
    }

    private IEnumerator ToxicBombRoutine(Vector2 start, Vector2 target, float splashRadius,
        float baseDamage)
    {
        GameObject bomb = CreateCircleObject("Toxic Bomb", new Color(0.55f, 1f, 0.12f), 0.24f, 16);
        bomb.transform.position = start;
        runtimeObjects.Add(bomb);

        ParticleSystem trail = CreateParticleSystem("Toxic Spitter Trail", new Color(0.45f, 1f, 0.08f),
            0.12f, 0.34f, 80);
        trail.Play();

        const float travelTime = 0.38f;
        float elapsed = 0f;
        while (elapsed < travelTime && bomb != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);
            Vector2 position = Vector2.Lerp(start, target, t);
            position.y += Mathf.Sin(t * Mathf.PI) * 0.8f;
            if (rangeIndicator != null)
                position = rangeIndicator.ClampPointToIndicator(position);
            bomb.transform.position = position;
            EmitParticle(trail, position, UnityEngine.Random.insideUnitCircle * 0.12f, 1f);
            yield return null;
        }

        if (trail != null)
        {
            trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            ReleaseParticleSystemAfter(trail, 0.45f);
        }

        if (bomb != null)
        {
            runtimeObjects.Remove(bomb);
            Destroy(bomb);
        }

        BackgroundMusic.ToxicSpitterImpacted();
        CreateRingEffect(target, splashRadius, new Color(0.45f, 1f, 0.08f), 0.22f);
        CreateRadialParticles("Toxic Spitter Splash", target, new Color(0.45f, 1f, 0.08f),
            28, 2.6f, 0.2f, 0.5f);
        EnemyHealth reflectionSource = null;
        float reflectedDamage = 0f;
        float poisonDamage = GetWeaponDamage(4f) *
                             (HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialOne) ? 1.65f : 1f);
        foreach (EnemyHealth enemy in FindEnemies(target, splashRadius))
        {
            reflectedDamage = DealWeaponDamage(enemy, baseDamage, DamageNumberType.Poison);
            PrototypePoisonStatus poison = enemy.GetComponent<PrototypePoisonStatus>();
            if (poison == null) poison = enemy.gameObject.AddComponent<PrototypePoisonStatus>();
            poison.Apply(4f, poisonDamage);
            reflectionSource = enemy;
        }
        if (reflectionSource != null) TryReflect(reflectionSource, reflectedDamage);

        if (HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialThree))
            StartCoroutine(ToxicPoolRoutine(target, splashRadius, baseDamage, poisonDamage));
    }

    private IEnumerator ToxicPoolRoutine(Vector2 position, float radius, float baseDamage, float poisonDamage)
    {
        GameObject pool = CreateCircleObject("Lingering Toxic Pool",
            new Color(0.28f, 0.8f, 0.05f, 0.34f), radius * 2f, 5);
        pool.transform.position = position;
        runtimeObjects.Add(pool);

        const float duration = 3.5f;
        const float tickInterval = 0.5f;
        BackgroundMusic.ToxicPoolStarted(position, duration);
        float remaining = duration;
        while (remaining > 0f && pool != null)
        {
            foreach (EnemyHealth enemy in FindEnemies(position, radius))
            {
                DealWeaponDamage(enemy, baseDamage * 0.14f, DamageNumberType.Poison);
                PrototypePoisonStatus poison = enemy.GetComponent<PrototypePoisonStatus>();
                if (poison == null) poison = enemy.gameObject.AddComponent<PrototypePoisonStatus>();
                poison.Apply(1.1f, poisonDamage * 0.45f);
            }

            CreateRadialParticles("Toxic Pool Bubbles", position, new Color(0.45f, 1f, 0.08f),
                7, 0.65f, 0.11f, 0.35f);
            yield return new WaitForSeconds(tickInterval);
            remaining -= tickInterval;
        }

        if (pool != null)
        {
            runtimeObjects.Remove(pool);
            Destroy(pool);
        }
    }

    private void UpdateAttachments()
    {
        int spikeLevel = GetAttachmentLevel(PrototypeAttachment.SpikeBarrier);
        if (spikeLevel > 0 && Time.time >= nextSpikeDamageTime)
        {
            nextSpikeDamageTime = Time.time + 0.5f;
            bool hitEnemy = false;
            foreach (EnemyHealth enemy in FindEnemies(playerHealth.transform.position, 1.35f))
            {
                DealDamage(enemy, 5f * spikeLevel);
                hitEnemy = true;
            }
            if (hitEnemy)
            {
                BackgroundMusic.SpikeBarrierStruck();
                CreateRadialParticles("Spike Barrier Impact", playerHealth.transform.position,
                    new Color(1f, 0.2f, 0.14f), 10, 1.8f, 0.1f, 0.22f);
            }
        }

        int freezeLevel = GetAttachmentLevel(PrototypeAttachment.FreezeBurst);
        if (freezeLevel > 0 && Time.time >= nextFreezeBurstTime)
        {
            nextFreezeBurstTime = Time.time + FreezeBurstCooldown;
            float radius = ClampRadiusToIndicator(4f + 0.45f * freezeLevel);
            foreach (EnemyHealth enemy in FindEnemies(playerHealth.transform.position, radius))
                enemy.Stun(0.65f + 0.25f * freezeLevel);
            BackgroundMusic.FreezeBurstActivated();
            CreateRingEffect(playerHealth.transform.position, radius, new Color(0.25f, 0.85f, 1f), 0.3f);
            CreateRadialParticles("Freeze Burst Particles", playerHealth.transform.position,
                new Color(0.25f, 0.85f, 1f), 34, radius * 1.8f, 0.12f, 0.4f);
        }
    }

    private void UpdateSatellites()
    {
        if (satellites.Count == 0)
            return;

        Vector3 center = playerHealth.transform.position;
        float orbitTime = Time.time * 75f;
        for (int i = 0; i < satellites.Count; i++)
        {
            if (satellites[i] == null) continue;
            float angle = orbitTime + 360f * i / satellites.Count;
            satellites[i].transform.position = center + Quaternion.Euler(0f, 0f, angle) * Vector3.right * 1.25f;
        }

        if (Time.time < nextSatelliteFireTime)
            return;

        nextSatelliteFireTime = Time.time + 0.7f;
        int level = GetAttachmentLevel(PrototypeAttachment.MiniTurretSatellites);
        foreach (GameObject satellite in satellites)
        {
            if (satellite == null) continue;
            EnemyHealth target = FindNearestEnemyWithinIndicator(satellite.transform.position, ApplyGlobalRange(6f), null);
            if (target == null) continue;
            BackgroundMusic.MiniTurretFired();
            float dealt = DealDamage(target, 7f + level * 1.5f, DamageNumberType.Energy);
            CreateTimedLine("Satellite Shot", satellite.transform.position, target.transform.position,
                new Color(0.4f, 1f, 0.85f), 0.035f, 0.08f);
            CreateLineParticles("Satellite Shot Particles", satellite.transform.position, target.transform.position,
                new Color(0.4f, 1f, 0.85f), 7, 0.07f, 0.1f);
            TryReflect(target, dealt);
        }
    }

    private void ActivateOvercharge()
    {
        int level = GetAttachmentLevel(PrototypeAttachment.OverchargeModule);
        overchargeEndsAt = Time.time + 3.5f + level * 0.5f;
        overchargeReadyAt = Time.time + OverchargeCooldown;
        BackgroundMusic.OverchargeActivated();
        CreateRingEffect(playerHealth.transform.position, 1.7f, new Color(1f, 0.45f, 0.08f), 0.28f);
        CreateRadialParticles("Overcharge Particles", playerHealth.transform.position,
            new Color(1f, 0.45f, 0.08f), 30, 2.5f, 0.12f, 0.38f);
    }

    public bool OwnsPrimary(PrototypePrimaryWeapon weapon) => ownedPrimaries.Contains(weapon);

    public bool BuyOrEquipPrimary(PrototypePrimaryWeapon weapon)
    {
        if (!ownedPrimaries.Contains(weapon))
        {
            if (CurrencyManager.Instance == null || !CurrencyManager.Instance.SpendMoney(GetPrimaryCost(weapon)))
            {
                BackgroundMusic.PurchaseDenied();
                return false;
            }
            ownedPrimaries.Add(weapon);
            purchasedPrimaries.Add(weapon);
            BackgroundMusic.Purchase();
        }
        else
        {
            BackgroundMusic.ButtonClick();
        }

        ActivePrimary = weapon;
        laserFocusBonus = 0f;
        lastLaserPulseTime = float.NegativeInfinity;
        nextPrimaryFireTime = 0f;
        StopWeaponAudio();
        UpdateRangeIndicatorForActiveWeapon();
        UpdatePersistentVisuals();
        WeaponProfile profile = GetProfile(ActivePrimary);
        HealthBar.SetOverheat(GetWeaponState(ActivePrimary).Heat, GetMaxHeat(profile));
        StateChanged?.Invoke();
        return true;
    }

    public bool BuyAttachment(PrototypeAttachment attachment)
    {
        int level = GetAttachmentLevel(attachment);
        if (level >= GetAttachmentMaxLevel(attachment))
            return false;

        int cost = GetAttachmentCost(attachment, level);
        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.SpendMoney(cost))
        {
            BackgroundMusic.PurchaseDenied();
            return false;
        }

        attachmentLevels[attachment] = level + 1;
        if (attachment == PrototypeAttachment.MiniTurretSatellites)
            CreateSatellite();
        if (attachment == PrototypeAttachment.FreezeBurst && level == 0)
            nextFreezeBurstTime = Time.time + FreezeBurstCooldown;
        UpdatePersistentVisuals();
        BackgroundMusic.UpgradeUnlocked();
        StateChanged?.Invoke();
        return true;
    }

    public bool BuyWeaponUpgrade(PrototypeWeaponUpgrade upgrade)
    {
        WeaponState state = GetWeaponState(ActivePrimary);
        int level = state.UpgradeLevels[upgrade];
        int maxLevel = GetWeaponUpgradeMaxLevel(upgrade);
        if (level >= maxLevel)
            return false;

        int cost = GetWeaponUpgradeCost(ActivePrimary, upgrade, level);
        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.SpendMoney(cost))
        {
            BackgroundMusic.PurchaseDenied();
            return false;
        }

        state.UpgradeLevels[upgrade] = level + 1;
        WeaponProfile profile = GetProfile(ActivePrimary);
        state.Heat = Mathf.Min(state.Heat, GetMaxHeat(profile));
        UpdateRangeIndicatorForActiveWeapon();
        BackgroundMusic.UpgradeUnlocked();
        StateChanged?.Invoke();
        return true;
    }

    public int GetWeaponUpgradeLevel(PrototypePrimaryWeapon weapon, PrototypeWeaponUpgrade upgrade)
    {
        WeaponState state = GetWeaponState(weapon);
        return state.UpgradeLevels.TryGetValue(upgrade, out int level) ? level : 0;
    }

    private bool HasWeaponUpgrade(PrototypeWeaponUpgrade upgrade)
    {
        return HasWeaponUpgrade(ActivePrimary, upgrade);
    }

    private bool HasWeaponUpgrade(PrototypePrimaryWeapon weapon, PrototypeWeaponUpgrade upgrade)
    {
        return GetWeaponUpgradeLevel(weapon, upgrade) > 0;
    }

    public int ResetProgressAndRefund()
    {
        int refund = CalculateFullRefund();

        ownedPrimaries.Clear();
        purchasedPrimaries.Clear();
        ownedPrimaries.Add(startingPrimary);
        ActivePrimary = startingPrimary;

        foreach (WeaponState state in weaponStates.Values)
        {
            state.Heat = 0f;
            state.Overheated = false;
            foreach (PrototypeWeaponUpgrade upgrade in Enum.GetValues(typeof(PrototypeWeaponUpgrade)))
                state.UpgradeLevels[upgrade] = 0;
        }

        foreach (PrototypeAttachment attachment in Enum.GetValues(typeof(PrototypeAttachment)))
            attachmentLevels[attachment] = 0;

        for (int i = satellites.Count - 1; i >= 0; i--)
        {
            GameObject satellite = satellites[i];
            runtimeObjects.Remove(satellite);
            if (satellite != null)
                Destroy(satellite);
        }
        satellites.Clear();
        overchargeEndsAt = 0f;
        overchargeReadyAt = 0f;
        nextFreezeBurstTime = 0f;
        laserFocusBonus = 0f;
        lastLaserPulseTime = float.NegativeInfinity;
        rapidBulletCounter = 0;
        ClearCombustionTracking();
        UpdateRangeIndicatorForActiveWeapon();

        if (CurrencyManager.Instance != null && refund > 0)
            CurrencyManager.Instance.AddMoney(refund, false);

        UpdatePersistentVisuals();
        WeaponProfile profile = GetProfile(ActivePrimary);
        HealthBar.SetOverheat(0f, GetMaxHeat(profile));
        BackgroundMusic.Purchase();
        StateChanged?.Invoke();
        return refund;
    }

    public static int GetWeaponUpgradeMaxLevel(PrototypeWeaponUpgrade upgrade)
    {
        return IsSpecialUpgrade(upgrade) ? 1 : 5;
    }

    public static int GetWeaponUpgradeCost(PrototypePrimaryWeapon weapon, PrototypeWeaponUpgrade upgrade, int currentLevel)
    {
        int level = Mathf.Max(0, currentLevel);
        int baseCost;
        int costAddedPerLevel;
        float costMultiplierPerLevel;
        switch (upgrade)
        {
            case PrototypeWeaponUpgrade.Damage:
                baseCost = 100;
                costAddedPerLevel = 35;
                costMultiplierPerLevel = 1.25f;
                break;
            case PrototypeWeaponUpgrade.Range:
                baseCost = 90;
                costAddedPerLevel = 30;
                costMultiplierPerLevel = 1.22f;
                break;
            case PrototypeWeaponUpgrade.FireRate:
                baseCost = 110;
                costAddedPerLevel = 40;
                costMultiplierPerLevel = 1.30f;
                break;
            case PrototypeWeaponUpgrade.Cooling:
                baseCost = 95;
                costAddedPerLevel = 32;
                costMultiplierPerLevel = 1.24f;
                break;
            default:
                return GlobalData.ScaleUpgradePrice(GetSpecialUpgradeCost(weapon, upgrade));
        }

        float linearCost = baseCost + costAddedPerLevel * level;
        int basePrice = Mathf.Max(0, Mathf.RoundToInt(
            linearCost * Mathf.Pow(costMultiplierPerLevel, level)));
        return GlobalData.ScaleUpgradePrice(basePrice);
    }

    private static int GetSpecialUpgradeCost(PrototypePrimaryWeapon weapon, PrototypeWeaponUpgrade upgrade)
    {
        int slotCost = upgrade == PrototypeWeaponUpgrade.SpecialOne ? 0 :
            upgrade == PrototypeWeaponUpgrade.SpecialTwo ? 85 : 180;
        int weaponCost;
        switch (weapon)
        {
            case PrototypePrimaryWeapon.PiercingLaser: weaponCost = 475; break;
            case PrototypePrimaryWeapon.LightningArc: weaponCost = 500; break;
            case PrototypePrimaryWeapon.ToxicSpitter: weaponCost = 450; break;
            case PrototypePrimaryWeapon.Flamethrower: weaponCost = 425; break;
            case PrototypePrimaryWeapon.ShotgunBurst: weaponCost = 400; break;
            default: weaponCost = 420; break;
        }

        return weaponCost + slotCost;
    }

    private static bool IsSpecialUpgrade(PrototypeWeaponUpgrade upgrade)
    {
        return upgrade == PrototypeWeaponUpgrade.SpecialOne ||
               upgrade == PrototypeWeaponUpgrade.SpecialTwo ||
               upgrade == PrototypeWeaponUpgrade.SpecialThree;
    }

    public static string GetWeaponDisplayName(PrototypePrimaryWeapon weapon)
    {
        return GetProfile(weapon).DisplayName;
    }

    public static string GetWeaponUpgradeName(PrototypePrimaryWeapon weapon, PrototypeWeaponUpgrade upgrade)
    {
        switch (upgrade)
        {
            case PrototypeWeaponUpgrade.Damage:
                return weapon == PrototypePrimaryWeapon.Flamethrower ? "Fuel Pressure" :
                    weapon == PrototypePrimaryWeapon.LightningArc ? "Voltage Amplifier" :
                    weapon == PrototypePrimaryWeapon.ToxicSpitter ? "Payload Strength" : "Damage Tuning";
            case PrototypeWeaponUpgrade.Range:
                return weapon == PrototypePrimaryWeapon.ShotgunBurst ? "Longer Barrel" :
                    weapon == PrototypePrimaryWeapon.Flamethrower ? "Extended Nozzle" :
                    weapon == PrototypePrimaryWeapon.LightningArc ? "Arc Reach" :
                    weapon == PrototypePrimaryWeapon.ToxicSpitter ? "Launch Pressure" : "Range Tuning";
            case PrototypeWeaponUpgrade.FireRate:
                return weapon == PrototypePrimaryWeapon.PiercingLaser ? "Rapid Pulse Lens" :
                    weapon == PrototypePrimaryWeapon.Flamethrower ? "Fuel Pump" :
                    weapon == PrototypePrimaryWeapon.LightningArc ? "Fast Discharge" :
                    weapon == PrototypePrimaryWeapon.ToxicSpitter ? "Auto-Loader" : "Fire Control";
            case PrototypeWeaponUpgrade.Cooling:
                return weapon == PrototypePrimaryWeapon.Flamethrower ? "Insulated Fuel Lines" :
                    weapon == PrototypePrimaryWeapon.PiercingLaser ? "Lens Coolant" : "Cooling System";
            default:
                return GetSpecialUpgradeName(weapon, upgrade);
        }
    }

    public static string GetWeaponUpgradeDescription(PrototypePrimaryWeapon weapon, PrototypeWeaponUpgrade upgrade)
    {
        switch (upgrade)
        {
            case PrototypeWeaponUpgrade.Damage: return "+15% damage per level for this weapon only.";
            case PrototypeWeaponUpgrade.Range: return "+10% range per level; visuals remain inside the range indicator.";
            case PrototypeWeaponUpgrade.FireRate: return "+10% firing speed per level for this weapon only.";
            case PrototypeWeaponUpgrade.Cooling: return "+12% heat capacity and +15% cooling per level.";
            default:
                return GetSpecialUpgradeDescription(weapon, upgrade);
        }
    }

    public string GetWeaponUpgradePreviewText(PrototypePrimaryWeapon weapon, PrototypeWeaponUpgrade upgrade)
    {
        int level = GetWeaponUpgradeLevel(weapon, upgrade);
        if (level >= GetWeaponUpgradeMaxLevel(upgrade))
            return "Maximum level reached.";

        WeaponProfile profile = GetProfile(weapon);
        int nextLevel = level + 1;
        switch (upgrade)
        {
            case PrototypeWeaponUpgrade.Damage:
                return string.Format("Damage {0:0.##} -> {1:0.##}",
                    GetWeaponDamageAtLevel(profile.Damage, weapon, level),
                    GetWeaponDamageAtLevel(profile.Damage, weapon, nextLevel));
            case PrototypeWeaponUpgrade.Range:
                return string.Format("Range {0:0.##} -> {1:0.##}",
                    GetWeaponRangeAtLevel(profile.Range, level),
                    GetWeaponRangeAtLevel(profile.Range, nextLevel));
            case PrototypeWeaponUpgrade.FireRate:
                return string.Format("Fire delay {0:0.###}s -> {1:0.###}s",
                    GetWeaponFireDelayAtLevel(profile.FireDelay, level),
                    GetWeaponFireDelayAtLevel(profile.FireDelay, nextLevel));
            case PrototypeWeaponUpgrade.Cooling:
                return string.Format("Heat cap {0:0.#} -> {1:0.#} | Cooling {2:0.#} -> {3:0.#}/s",
                    GetMaxHeatAtLevel(profile, level), GetMaxHeatAtLevel(profile, nextLevel),
                    GetCoolingAtLevel(profile, level), GetCoolingAtLevel(profile, nextLevel));
            default:
                return GetSpecialUpgradePreview(weapon, upgrade);
        }
    }

    public string GetAttachmentUpgradePreviewText(PrototypeAttachment attachment)
    {
        int level = GetAttachmentLevel(attachment);
        if (level >= GetAttachmentMaxLevel(attachment))
            return "Maximum level reached.";

        int nextLevel = level + 1;
        switch (attachment)
        {
            case PrototypeAttachment.MiniTurretSatellites:
                return string.Format("Orbiting shooters {0} -> {1}", level, nextLevel);
            case PrototypeAttachment.SpikeBarrier:
                return string.Format("Contact damage {0:0.#} -> {1:0.#} per hit", level * 5f, nextLevel * 5f);
            case PrototypeAttachment.OverchargeModule:
                return string.Format("Fire-rate boost +{0:0}% -> +{1:0}% | Duration {2:0.#}s -> {3:0.#}s",
                    level > 0 ? 35f + level * 10f : 0f, 35f + nextLevel * 10f,
                    level > 0 ? 3.5f + level * 0.5f : 0f, 3.5f + nextLevel * 0.5f);
            case PrototypeAttachment.FreezeBurst:
                return string.Format("Radius {0:0.##} -> {1:0.##} | Freeze {2:0.##}s -> {3:0.##}s",
                    level > 0 ? 4f + level * 0.45f : 0f, 4f + nextLevel * 0.45f,
                    level > 0 ? 0.65f + level * 0.25f : 0f, 0.65f + nextLevel * 0.25f);
            default:
                return string.Format("Bounce chance {0:0}% -> {1:0}%",
                    Mathf.Min(65f, level * 12f), Mathf.Min(65f, nextLevel * 12f));
        }
    }

    private static string GetSpecialUpgradeName(PrototypePrimaryWeapon weapon, PrototypeWeaponUpgrade upgrade)
    {
        switch (weapon)
        {
            case PrototypePrimaryWeapon.RapidBullets:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne) return "Armor Drill";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo) return "Tracer Cycle";
                return "Bullet Storm";
            case PrototypePrimaryWeapon.PiercingLaser:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne) return "Thermal Focus";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo) return "Wide Aperture";
                return "Disintegrator Lens";
            case PrototypePrimaryWeapon.ShotgunBurst:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne) return "Tight Choke";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo) return "Concussive Shells";
                return "Slug Chamber";
            case PrototypePrimaryWeapon.Flamethrower:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne) return "Long-Burn Fuel";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo) return "Wide Nozzle";
                return "Combustion";
            case PrototypePrimaryWeapon.LightningArc:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne) return "Extended Arc";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo) return "Static Lock";
                return "Feedback Loop";
            default:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne) return "Concentrated Venom";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo) return "Expanding Splat";
                return "Lingering Pool";
        }
    }

    private static string GetSpecialUpgradeDescription(PrototypePrimaryWeapon weapon,
        PrototypeWeaponUpgrade upgrade)
    {
        switch (weapon)
        {
            case PrototypePrimaryWeapon.RapidBullets:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne)
                    return "Every bullet drills through two additional slimes.";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo)
                    return "Every sixth bullet becomes a bright tracer that deals triple damage.";
                return "Every twentieth bullet fires an eight-direction bullet storm.";
            case PrototypePrimaryWeapon.PiercingLaser:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne)
                    return "Holding the beam on continuously ramps its damage up to +50%.";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo)
                    return "Widens both the damaging beam and its visible laser line.";
                return "The beam instantly disintegrates non-boss slimes below 15% health.";
            case PrototypePrimaryWeapon.ShotgunBurst:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne)
                    return "Tightens the pellet spread for much stronger mid-range hits.";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo)
                    return "Pellet impacts knock slimes away from the tower.";
                return "Adds one heavy center slug to every shotgun blast.";
            case PrototypePrimaryWeapon.Flamethrower:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne)
                    return "Burning lasts three times longer after a slime leaves the flames.";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo)
                    return "Widens the damaging flame cone and the authored particle effect.";
                return "Burning slimes combust on death and ignite nearby slimes.";
            case PrototypePrimaryWeapon.LightningArc:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne)
                    return "Adds three chain targets and increases the distance between jumps.";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo)
                    return "Every lightning hit briefly stuns its target.";
                return "The final arc loops back and shocks the first target again.";
            default:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne)
                    return "Poison damage over time is increased by 65%.";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo)
                    return "Poison splats and their visible splash grow 45% larger.";
                return "Every bomb leaves a damaging poison pool for 3.5 seconds.";
        }
    }

    private static string GetSpecialUpgradePreview(PrototypePrimaryWeapon weapon,
        PrototypeWeaponUpgrade upgrade)
    {
        switch (weapon)
        {
            case PrototypePrimaryWeapon.RapidBullets:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne) return "Targets per bullet 1 -> 3";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo) return "Every 6th bullet deals 300% damage";
                return "Every 20th bullet fires in 8 directions";
            case PrototypePrimaryWeapon.PiercingLaser:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne) return "Unlock sustained damage ramp up to +50%";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo) return "Beam width 0.1 -> 0.32";
                return "Instantly finish non-boss slimes below 15% health";
            case PrototypePrimaryWeapon.ShotgunBurst:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne) return "Pellet spread 38 degrees -> 23 degrees";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo) return "Unlock 0.55-unit knockback per pellet";
                return "Add one center slug at 220% pellet damage";
            case PrototypePrimaryWeapon.Flamethrower:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne) return "Burn duration 1s -> 3s";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo) return "Flame cone 48 degrees -> 72 degrees";
                return "Unlock 1.65-radius death combustion";
            case PrototypePrimaryWeapon.LightningArc:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne) return "Chain targets +3 | Jump range 3.4 -> 4.6";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo) return "Unlock a 0.3-second stun per hit";
                return "Final arc returns at 75% of its remaining damage";
            default:
                if (upgrade == PrototypeWeaponUpgrade.SpecialOne) return "Poison damage 100% -> 165%";
                if (upgrade == PrototypeWeaponUpgrade.SpecialTwo) return "Splash radius 2 -> 2.9";
                return "Unlock a 3.5-second damaging poison pool";
        }
    }

    public string GetActiveWeaponStatsText()
    {
        WeaponProfile profile = GetProfile(ActivePrimary);
        int pierce = GetPierceCount(profile);
        string pierceLabel;
        switch (ActivePrimary)
        {
            case PrototypePrimaryWeapon.Flamethrower: pierceLabel = "All targets in cone"; break;
            case PrototypePrimaryWeapon.LightningArc: pierceLabel = "Chain targets " + pierce; break;
            case PrototypePrimaryWeapon.ToxicSpitter: pierceLabel = "Splash damage"; break;
            case PrototypePrimaryWeapon.ShotgunBurst: pierceLabel = "Pierce/pellet " + pierce; break;
            default: pierceLabel = "Pierce " + pierce; break;
        }

        return string.Format("EQUIPPED: {0}\nDamage {1:0.#}   Range {2:0.#}   Fire delay {3:0.###}s\nHeat/shot {4:0.#}   Capacity {5:0.#}   Cooling {6:0.#}/s   Multishot {7:0.#}%   {8}",
            profile.DisplayName,
            GetWeaponDamage(profile.Damage), ApplyWeaponRange(profile.Range), GetFireDelay(profile.FireDelay),
            profile.HeatPerShot, GetMaxHeat(profile), GetCoolingPerSecond(profile, ActivePrimary),
            GetChance(StatNames.MultishotChance) * 100f, pierceLabel);
    }

    private float GetWeaponDamageAtLevel(float baseDamage, PrototypePrimaryWeapon weapon, int damageLevel)
    {
        return GetModifiedDamage(baseDamage) * (1f + Mathf.Max(0, damageLevel) * 0.15f);
    }

    private float GetWeaponRangeAtLevel(float baseRange, int rangeLevel)
    {
        return ApplyGlobalRange(baseRange) * (1f + Mathf.Max(0, rangeLevel) * 0.1f);
    }

    private float GetWeaponFireDelayAtLevel(float baseDelay, int fireRateLevel)
    {
        float speed = 1f + (GetPercent(StatNames.AttackSpeed) + GetPercent(StatNames.CooldownReduction)) / 100f;
        speed *= 1f + Mathf.Max(0, fireRateLevel) * 0.1f;
        return baseDelay / Mathf.Max(0.1f, speed);
    }

    private static float GetMaxHeatAtLevel(WeaponProfile profile, int coolingLevel)
    {
        return profile.MaxHeat * (1f + Mathf.Max(0, coolingLevel) * 0.12f);
    }

    private float GetCoolingAtLevel(WeaponProfile profile, int coolingLevel)
    {
        float reloadRecovery = 1f + GetPercent(StatNames.ReloadSpeed) / 100f;
        return profile.CoolingPerSecond * (1f + Mathf.Max(0, coolingLevel) * 0.15f) *
               Mathf.Max(0.1f, reloadRecovery);
    }

    public int GetAttachmentLevel(PrototypeAttachment attachment)
    {
        return attachmentLevels.TryGetValue(attachment, out int level) ? level : 0;
    }

    public int GetPrimaryCost(PrototypePrimaryWeapon weapon)
    {
        return ownedPrimaries.Contains(weapon)
            ? 0
            : GetPrimaryPurchaseCost(purchasedPrimaries.Count);
    }

    public static int GetAttachmentMaxLevel(PrototypeAttachment attachment)
    {
        return attachment == PrototypeAttachment.MiniTurretSatellites ? 4 : 5;
    }

    public int GetAttachmentCost(PrototypeAttachment attachment, int currentLevel)
    {
        int level = Mathf.Max(0, currentLevel);
        return level == 0
            ? GetAttachmentUnlockCost(GetUnlockedAttachmentCount())
            : GetAttachmentUpgradeCost(attachment, level);
    }

    private static int GetPrimaryPurchaseCost(int weaponsPurchased)
    {
        // Every unowned weapon uses the same purchase-order tier.
        int tier = Mathf.Max(0, weaponsPurchased);
        return GlobalData.ScaleUpgradePrice(200 + 250 * tier + 150 * tier * tier);
    }

    private static int GetAttachmentUnlockCost(int attachmentsUnlocked)
    {
        // Like weapons, every locked secondary tool shares the current tier.
        int tier = Mathf.Max(0, attachmentsUnlocked);
        return GlobalData.ScaleUpgradePrice(250 + 250 * tier + 150 * tier * tier);
    }

    private static int GetAttachmentUpgradeCost(PrototypeAttachment attachment, int currentLevel)
    {
        int ownedUpgradeLevels = Mathf.Max(0, currentLevel - 1);
        int baseCost;
        int costAddedPerLevel;
        float costMultiplierPerLevel;

        switch (attachment)
        {
            case PrototypeAttachment.MiniTurretSatellites:
                baseCost = 300;
                costAddedPerLevel = 100;
                costMultiplierPerLevel = 1.45f;
                break;
            case PrototypeAttachment.SpikeBarrier:
                baseCost = 200;
                costAddedPerLevel = 65;
                costMultiplierPerLevel = 1.25f;
                break;
            case PrototypeAttachment.OverchargeModule:
                baseCost = 275;
                costAddedPerLevel = 90;
                costMultiplierPerLevel = 1.35f;
                break;
            case PrototypeAttachment.FreezeBurst:
                baseCost = 300;
                costAddedPerLevel = 100;
                costMultiplierPerLevel = 1.42f;
                break;
            default:
                baseCost = 325;
                costAddedPerLevel = 110;
                costMultiplierPerLevel = 1.50f;
                break;
        }

        float linearCost = baseCost + costAddedPerLevel * ownedUpgradeLevels;
        int basePrice = Mathf.Max(0, Mathf.RoundToInt(
            linearCost * Mathf.Pow(costMultiplierPerLevel, ownedUpgradeLevels)));
        return GlobalData.ScaleUpgradePrice(basePrice);
    }

    private int GetUnlockedAttachmentCount()
    {
        int unlocked = 0;
        foreach (int level in attachmentLevels.Values)
        {
            if (level > 0)
                unlocked++;
        }

        return unlocked;
    }

    private int CalculateFullRefund()
    {
        int refund = 0;
        for (int purchaseIndex = 0; purchaseIndex < purchasedPrimaries.Count; purchaseIndex++)
            refund += GetPrimaryPurchaseCost(purchaseIndex);

        foreach (PrototypePrimaryWeapon weapon in Enum.GetValues(typeof(PrototypePrimaryWeapon)))
        {
            foreach (PrototypeWeaponUpgrade upgrade in Enum.GetValues(typeof(PrototypeWeaponUpgrade)))
            {
                int level = GetWeaponUpgradeLevel(weapon, upgrade);
                for (int purchasedLevel = 0; purchasedLevel < level; purchasedLevel++)
                    refund += GetWeaponUpgradeCost(weapon, upgrade, purchasedLevel);
            }
        }

        int unlockedAttachmentCount = GetUnlockedAttachmentCount();
        for (int unlockIndex = 0; unlockIndex < unlockedAttachmentCount; unlockIndex++)
            refund += GetAttachmentUnlockCost(unlockIndex);

        foreach (PrototypeAttachment attachment in Enum.GetValues(typeof(PrototypeAttachment)))
        {
            int level = GetAttachmentLevel(attachment);
            for (int purchasedLevel = 1; purchasedLevel < level; purchasedLevel++)
                refund += GetAttachmentUpgradeCost(attachment, purchasedLevel);
        }
        return refund;
    }

    private static void ClearLegacySavedProgress()
    {
        if (!PlayerPrefs.HasKey(LegacyProgressSaveKey))
            return;

        PlayerPrefs.DeleteKey(LegacyProgressSaveKey);
        PlayerPrefs.Save();
    }

    private static WeaponProfile GetProfile(PrototypePrimaryWeapon weapon)
    {
        switch (weapon)
        {
            case PrototypePrimaryWeapon.PiercingLaser:
                return new WeaponProfile("Piercing Laser", 7.5f, 5f, 0.09f, 110f, 6f, 27f, 6);
            case PrototypePrimaryWeapon.ShotgunBurst:
                return new WeaponProfile("Shotgun Burst", 25f, 3.6f, 0.72f, 90f, 23f, 26f, 0);
            case PrototypePrimaryWeapon.Flamethrower:
                return new WeaponProfile("Flamethrower", 2.4f, 3.9f, 0.1f, 125f, 4.5f, 23f, 0);
            case PrototypePrimaryWeapon.LightningArc:
                return new WeaponProfile("Lightning Arc", 36f, 4.6f, 0.62f, 100f, 18f, 25f, 5);
            case PrototypePrimaryWeapon.ToxicSpitter:
                return new WeaponProfile("Toxic Spitter", 27f, 4.1f, 0.85f, 105f, 22f, 24f, 0);
            default:
                return new WeaponProfile("Rapid Bullets", 14f, 4.5f, 0.085f, 100f, 4f, 30f, 0);
        }
    }

    private WeaponState GetWeaponState(PrototypePrimaryWeapon weapon)
    {
        if (!weaponStates.TryGetValue(weapon, out WeaponState state))
        {
            state = new WeaponState();
            weaponStates[weapon] = state;
        }
        return state;
    }

    private void UpdateWeaponHeat(bool activeTriggerHeld)
    {
        foreach (PrototypePrimaryWeapon weapon in Enum.GetValues(typeof(PrototypePrimaryWeapon)))
        {
            WeaponState state = GetWeaponState(weapon);
            WeaponProfile profile = GetProfile(weapon);
            float maxHeat = GetMaxHeat(profile, weapon);
            state.Heat = Mathf.Clamp(state.Heat, 0f, maxHeat);

            bool activelyFiring = weapon == ActivePrimary && activeTriggerHeld && !state.Overheated;
            if (!activelyFiring || state.Overheated)
            {
                float cooling = GetCoolingPerSecond(profile, weapon);
                state.Heat = Mathf.MoveTowards(state.Heat, 0f, cooling * Time.deltaTime);
            }

            if (state.Overheated && state.Heat <= 0f)
            {
                state.Heat = 0f;
                state.Overheated = false;
                if (weapon == ActivePrimary)
                {
                    BackgroundMusic.OverheatCooled();
                    UpdatePersistentVisuals();
                }
            }
        }
    }

    private void UpdateWeaponAudio(bool shouldPlay)
    {
        if (weaponLoopSource == null)
            return;

        bool usesLoop = ActivePrimary == PrototypePrimaryWeapon.RapidBullets ||
                        ActivePrimary == PrototypePrimaryWeapon.PiercingLaser ||
                        ActivePrimary == PrototypePrimaryWeapon.Flamethrower;
        if (!shouldPlay || !usesLoop)
        {
            StopWeaponAudio();
            return;
        }

        if (weaponLoopSource.clip == null || loopWeapon != ActivePrimary)
        {
            loopWeapon = ActivePrimary;
            if (!BackgroundMusic.ConfigurePrototypeWeaponLoop(weaponLoopSource, ActivePrimary))
                return;
        }

        if (!weaponLoopSource.isPlaying)
        {
            BackgroundMusic.PrototypeWeaponLoopStarted(ActivePrimary);
            weaponLoopSource.Play();
        }
    }

    private void StopWeaponAudio()
    {
        if (weaponLoopSource != null && weaponLoopSource.isPlaying)
        {
            weaponLoopSource.Stop();
            BackgroundMusic.PrototypeWeaponLoopStopped(loopWeapon);
        }
    }

    private void AddHeat(float amount)
    {
        WeaponState state = GetWeaponState(ActivePrimary);
        WeaponProfile profile = GetProfile(ActivePrimary);
        float maxHeat = GetMaxHeat(profile);
        state.Heat = Mathf.Min(maxHeat, state.Heat + Mathf.Max(0f, amount));
        if (!state.Overheated && state.Heat >= maxHeat)
        {
            state.Overheated = true;
            BackgroundMusic.Overheated(ActivePrimary == PrototypePrimaryWeapon.Flamethrower);
            CreateRadialParticles("Weapon Overheat", playerHealth.transform.position,
                new Color(1f, 0.18f, 0.04f), 26, 2.1f, 0.16f, 0.45f);
            UpdatePersistentVisuals();
        }
    }

    private float GetMaxHeat(WeaponProfile profile)
    {
        return GetMaxHeat(profile, ActivePrimary);
    }

    private float GetMaxHeat(WeaponProfile profile, PrototypePrimaryWeapon weapon)
    {
        int level = GetWeaponUpgradeLevel(weapon, PrototypeWeaponUpgrade.Cooling);
        return profile.MaxHeat * (1f + level * 0.12f);
    }

    private float GetCoolingPerSecond(WeaponProfile profile, PrototypePrimaryWeapon weapon)
    {
        int level = GetWeaponUpgradeLevel(weapon, PrototypeWeaponUpgrade.Cooling);
        float reloadRecovery = 1f + GetPercent(StatNames.ReloadSpeed) / 100f;
        return profile.CoolingPerSecond * (1f + level * 0.15f) * Mathf.Max(0.1f, reloadRecovery);
    }

    private int GetPierceCount(WeaponProfile profile)
    {
        if (ActivePrimary == PrototypePrimaryWeapon.RapidBullets &&
            HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialOne))
        {
            return profile.Pierce + 2;
        }

        if (ActivePrimary == PrototypePrimaryWeapon.LightningArc &&
            HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialOne))
        {
            return profile.Pierce + 3;
        }

        return profile.Pierce;
    }

    private float DealWeaponDamage(
        EnemyHealth enemy,
        float baseDamage,
        DamageNumberType damageType = DamageNumberType.Normal)
    {
        return DealDamage(enemy, baseDamage, true, damageType);
    }

    private float DealDamage(
        EnemyHealth enemy,
        float baseDamage,
        DamageNumberType damageType = DamageNumberType.Normal)
    {
        return DealDamage(enemy, baseDamage, false, damageType);
    }

    private float DealDamage(
        EnemyHealth enemy,
        float baseDamage,
        bool useWeaponUpgrade,
        DamageNumberType damageType)
    {
        if (enemy == null) return 0f;
        float damage = useWeaponUpgrade ? GetWeaponDamage(baseDamage) : GetModifiedDamage(baseDamage);
        float critChance = GetChance(StatNames.CritChance);
        if (UnityEngine.Random.value < critChance)
            damage *= 2f * (1f + GetChance(StatNames.CritDamage));
        enemy.TakeDamage(damage, damageType);

        float lifestealChance = GetChance(StatNames.LifestealChance);
        float lifestealAmount = Mathf.Max(0f, GetPercent(StatNames.LifestealAmount) / 100f);
        if (playerHealth != null && lifestealAmount > 0f && UnityEngine.Random.value < lifestealChance)
            playerHealth.Heal(enemy.MaxHealth * lifestealAmount);
        return damage;
    }

    private float GetWeaponDamage(float baseDamage)
    {
        int level = GetWeaponUpgradeLevel(ActivePrimary, PrototypeWeaponUpgrade.Damage);
        return GetModifiedDamage(baseDamage) * (1f + level * 0.15f);
    }

    private void TryReflect(EnemyHealth source, float damage)
    {
        int level = GetAttachmentLevel(PrototypeAttachment.ReflectorDome);
        if (level <= 0 || source == null || UnityEngine.Random.value >= Mathf.Min(0.65f, level * 0.12f))
            return;
        HashSet<EnemyHealth> excluded = new HashSet<EnemyHealth> { source };
        EnemyHealth target = FindNearestEnemyWithinIndicator(source.transform.position, ApplyGlobalRange(4f), excluded);
        if (target == null) return;
        target.TakeDamage(damage * 0.6f, DamageNumberType.Reflected);
        BackgroundMusic.ReflectorRicocheted();
        CreateTimedLine("Reflected Shot", source.transform.position, target.transform.position,
            new Color(0.75f, 0.35f, 1f), 0.045f, 0.12f);
        CreateLineParticles("Reflected Shot Particles", source.transform.position, target.transform.position,
            new Color(0.75f, 0.35f, 1f), 9, 0.08f, 0.14f);
    }

    private float GetModifiedDamage(float baseDamage)
    {
        return PlayerStats.instance != null
            ? PlayerStats.instance.GetModifiedValue(StatNames.AttackDamage, baseDamage)
            : baseDamage;
    }

    private float GetModifiedStat(string statName, float baseValue)
    {
        return PlayerStats.instance != null
            ? PlayerStats.instance.GetModifiedValue(statName, baseValue)
            : baseValue;
    }

    private float GetPercent(string statName)
    {
        return PlayerStats.instance != null ? PlayerStats.instance.GetPercentModifier(statName) : 0f;
    }

    private float GetChance(string statName)
    {
        if (PlayerStats.instance == null) return 0f;
        return Mathf.Clamp01((PlayerStats.instance.GetPercentModifier(statName) +
                              PlayerStats.instance.GetFlatModifier(statName)) / 100f);
    }

    private static bool RollMultishot(float chance)
    {
        return chance > 0f && UnityEngine.Random.value < chance;
    }

    private static Vector2 RotateDirection(Vector2 direction, float angleDegrees)
    {
        Vector2 rotated = Quaternion.Euler(0f, 0f, angleDegrees) * direction;
        return rotated.sqrMagnitude > 0.001f ? rotated.normalized : Vector2.right;
    }

    private float ApplyGlobalRange(float baseRange) =>
        baseRange * Mathf.Max(0.1f, 1f + GetPercent(StatNames.Range) / 100f);

    private float ApplyWeaponRange(float baseRange)
    {
        int level = GetWeaponUpgradeLevel(ActivePrimary, PrototypeWeaponUpgrade.Range);
        return ApplyGlobalRange(baseRange) * (1f + level * 0.1f);
    }

    private void HandlePlayerStatChanged(string statName, float oldValue, float newValue)
    {
        if (statName == StatNames.Range)
            UpdateRangeIndicatorForActiveWeapon();
    }

    private void UpdateRangeIndicatorForActiveWeapon()
    {
        if (rangeIndicator == null || rangeIndicator.BaseWorldRadius <= 0f)
            return;

        WeaponProfile profile = GetProfile(ActivePrimary);
        float permanentMultiplier = Mathf.Max(0.1f, 1f + GetPercent(StatNames.Range) / 100f);
        float desiredWorldRadius = ApplyWeaponRange(profile.Range);
        if (ActivePrimary == PrototypePrimaryWeapon.ToxicSpitter)
            desiredWorldRadius += GetToxicSplashRadius();

        float permanentBaseRadius = rangeIndicator.BaseWorldRadius * permanentMultiplier;
        rangeIndicator.SetRuntimeRangeMultiplier(desiredWorldRadius / Mathf.Max(0.01f, permanentBaseRadius));
    }

    private float GetWeaponRadius(float baseRange)
    {
        return ClampRadiusToIndicator(ApplyWeaponRange(baseRange));
    }

    private float GetWeaponDistance(float baseRange, Vector2 origin, Vector2 direction)
    {
        float desiredDistance = ApplyWeaponRange(baseRange);
        return rangeIndicator != null
            ? rangeIndicator.ClampDistanceToIndicator(origin, direction, desiredDistance)
            : desiredDistance;
    }

    private float ClampRadiusToIndicator(float desiredRadius)
    {
        float indicatorRadius = rangeIndicator != null ? rangeIndicator.WorldRadius : 0f;
        return indicatorRadius > 0f
            ? Mathf.Min(Mathf.Max(0f, desiredRadius), indicatorRadius)
            : Mathf.Max(0f, desiredRadius);
    }

    private float GetFireDelay(float baseDelay)
    {
        float speed = 1f + (GetPercent(StatNames.AttackSpeed) + GetPercent(StatNames.CooldownReduction)) / 100f;
        speed *= 1f + GetWeaponUpgradeLevel(ActivePrimary, PrototypeWeaponUpgrade.FireRate) * 0.1f;
        int overchargeLevel = GetAttachmentLevel(PrototypeAttachment.OverchargeModule);
        if (overchargeLevel > 0 && Time.time < overchargeEndsAt)
            speed *= 1.35f + overchargeLevel * 0.1f;
        return baseDelay / Mathf.Max(0.1f, speed);
    }

    private Vector2 GetAimDirection(Vector2 origin)
    {
        Vector2 direction = GetPrimaryAimPosition(origin) - origin;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
    }

    private Vector2 GetPrimaryAimPosition(Vector2 origin)
    {
        if (GlobalData.AutomaticPrimaryAiming && automaticPrimaryTarget != null)
            return automaticPrimaryTarget.transform.position;

        return GetMouseWorldPosition();
    }

    private Vector2 GetMouseWorldPosition()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return playerHealth.transform.position + Vector3.right;
        Vector3 screen = Input.mousePosition;
        screen.z = Mathf.Abs(mainCamera.transform.position.z - playerHealth.transform.position.z);
        return mainCamera.ScreenToWorldPoint(screen);
    }

    private EnemyHealth FindAimTarget(Vector2 origin, Vector2 direction, float range, float maxAngle,
        HashSet<EnemyHealth> excluded)
    {
        EnemyHealth best = null;
        float bestAngle = maxAngle;
        foreach (EnemyHealth enemy in FindEnemies(origin, range))
        {
            if (excluded != null && excluded.Contains(enemy))
                continue;
            float angle = Vector2.Angle(direction, (Vector2)enemy.transform.position - origin);
            if (angle >= bestAngle) continue;
            bestAngle = angle;
            best = enemy;
        }
        return best;
    }

    private EnemyHealth FindNearestEnemy(Vector2 origin, float range, HashSet<EnemyHealth> excluded)
    {
        EnemyHealth nearest = null;
        float bestDistance = float.PositiveInfinity;
        foreach (EnemyHealth enemy in FindEnemies(origin, range))
        {
            if (excluded != null && excluded.Contains(enemy)) continue;
            float distance = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            nearest = enemy;
        }
        return nearest;
    }

    private EnemyHealth FindNearestEnemyWithinIndicator(Vector2 origin, float range,
        HashSet<EnemyHealth> excluded, HashSet<EnemyHealth> secondaryExcluded = null)
    {
        EnemyHealth nearest = null;
        float bestDistance = float.PositiveInfinity;
        float indicatorRadius = rangeIndicator != null ? rangeIndicator.WorldRadius : 0f;
        Vector2 indicatorCenter = rangeIndicator != null ? rangeIndicator.WorldCenter : Vector2.zero;

        foreach (EnemyHealth enemy in FindEnemies(origin, range))
        {
            if ((excluded != null && excluded.Contains(enemy)) ||
                (secondaryExcluded != null && secondaryExcluded.Contains(enemy)))
                continue;

            Vector2 enemyPosition = enemy.transform.position;
            if (indicatorRadius > 0f &&
                (enemyPosition - indicatorCenter).sqrMagnitude > indicatorRadius * indicatorRadius)
            {
                continue;
            }

            float distance = (enemyPosition - origin).sqrMagnitude;
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            nearest = enemy;
        }

        return nearest;
    }

    private List<RaycastHit2D> GetRaycastHits(Vector2 origin, Vector2 direction, float distance)
    {
        raycastHits.Clear();
        Physics2D.Raycast(origin, direction, physicsContactFilter, raycastHits, distance);
        return raycastHits;
    }

    private List<RaycastHit2D> GetCircleCastHits(Vector2 origin, Vector2 direction,
        float distance, float radius)
    {
        raycastHits.Clear();
        Physics2D.CircleCast(origin, radius, direction, physicsContactFilter, raycastHits, distance);
        return raycastHits;
    }

    private List<EnemyHealth> FindEnemies(Vector2 origin, float radius)
    {
        overlapColliders.Clear();
        uniqueEnemySearchResults.Clear();
        enemySearchResults.Clear();
        Physics2D.OverlapCircle(origin, radius, physicsContactFilter, overlapColliders);
        for (int i = 0; i < overlapColliders.Count; i++)
        {
            Collider2D collider = overlapColliders[i];
            EnemyHealth enemy = collider != null ? collider.GetComponentInParent<EnemyHealth>() : null;
            if (enemy != null && uniqueEnemySearchResults.Add(enemy))
                enemySearchResults.Add(enemy);
        }
        return enemySearchResults;
    }

    private void CreateSatellite()
    {
        GameObject satellite = CreateCircleObject("Mini-Turret Satellite", new Color(0.25f, 1f, 0.8f), 0.22f, 15);
        satellites.Add(satellite);
        runtimeObjects.Add(satellite);
    }

    private void UpdateWeaponVisual()
    {
        if (equippedWeaponSprite == null)
        {
            GameObject visual = new GameObject("Equipped Pixel Weapon");
            equippedWeaponSprite = visual.AddComponent<SpriteRenderer>();
            SpriteRenderer playerSprite = playerHealth.GetComponent<SpriteRenderer>();
            if (playerSprite != null)
                equippedWeaponSprite.sortingLayerID = playerSprite.sortingLayerID;
            equippedWeaponSprite.sortingOrder = 25;
            runtimeObjects.Add(visual);
        }
        if (equippedWeaponSprite.sprite == null || displayedWeapon != ActivePrimary)
        {
            displayedWeapon = ActivePrimary;
            equippedWeaponSprite.sprite = Resources.Load<Sprite>("WeaponIcons/" + ActivePrimary);
            if (equippedWeaponSprite.sprite != null)
                equippedWeaponSprite.transform.localScale = Vector3.one *
                    (1.5f / Mathf.Max(0.01f, equippedWeaponSprite.sprite.bounds.size.x));
        }
        Vector2 spriteOrigin = playerHealth.transform.position;
        Vector2 spriteDirection = GetAimDirection(spriteOrigin);
        equippedWeaponSprite.transform.position = (Vector3)(spriteOrigin + spriteDirection * 0.65f);
        equippedWeaponSprite.transform.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(spriteDirection.y, spriteDirection.x) * Mathf.Rad2Deg);
        equippedWeaponSprite.flipY = spriteDirection.x < 0f;
        if (weaponVisual != null) weaponVisual.enabled = false;
    }

    private void UpdatePersistentVisuals()
    {
        if (weaponVisual != null)
        {
            Color color;
            if (GetWeaponState(ActivePrimary).Overheated)
            {
                color = new Color(1f, 0.08f, 0.03f);
            }
            else switch (ActivePrimary)
            {
                case PrototypePrimaryWeapon.PiercingLaser: color = new Color(0.2f, 0.95f, 1f); break;
                case PrototypePrimaryWeapon.ShotgunBurst: color = new Color(1f, 0.65f, 0.2f); break;
                case PrototypePrimaryWeapon.Flamethrower: color = new Color(1f, 0.25f, 0.04f); break;
                case PrototypePrimaryWeapon.LightningArc: color = new Color(1f, 0.85f, 0.12f); break;
                case PrototypePrimaryWeapon.ToxicSpitter: color = new Color(0.45f, 1f, 0.08f); break;
                default: color = new Color(1f, 0.9f, 0.25f); break;
            }
            weaponVisual.startColor = color;
            weaponVisual.endColor = color;
        }

        int spikeLevel = GetAttachmentLevel(PrototypeAttachment.SpikeBarrier);
        if (spikeLevel > 0 && spikeVisual == null)
            spikeVisual = CreateCircleLine("Spike Barrier", new Color(1f, 0.2f, 0.14f, 0.8f), 1.35f, true, 8);
        if (spikeVisual != null) spikeVisual.gameObject.SetActive(spikeLevel > 0);

        int reflectorLevel = GetAttachmentLevel(PrototypeAttachment.ReflectorDome);
        if (reflectorLevel > 0 && reflectorVisual == null)
            reflectorVisual = CreateCircleLine("Reflector Dome", new Color(0.7f, 0.25f, 1f, 0.55f), 1.65f, false, 7);
        if (reflectorVisual != null) reflectorVisual.gameObject.SetActive(reflectorLevel > 0);

        bool flamethrowerEquipped = ActivePrimary == PrototypePrimaryWeapon.Flamethrower;
        if (!flamethrowerEquipped && originalFlamethrower != null)
            originalFlamethrower.StopParticleEffect(true);
        if (originalFlamethrowerObject != null && originalFlamethrowerObject.activeSelf != flamethrowerEquipped)
            originalFlamethrowerObject.SetActive(flamethrowerEquipped);
    }

    private void UpdateOriginalFlamethrowerVisual(bool shouldPlay)
    {
        if (originalFlamethrower == null || originalFlamethrowerObject == null)
            return;

        bool flamethrowerEquipped = ActivePrimary == PrototypePrimaryWeapon.Flamethrower;
        if (originalFlamethrowerObject.activeSelf != flamethrowerEquipped)
            originalFlamethrowerObject.SetActive(flamethrowerEquipped);
        if (!flamethrowerEquipped)
            return;

        Vector2 origin = playerHealth.transform.position;
        Vector2 direction = GetAimDirection(origin);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        originalFlamethrowerObject.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        WeaponProfile profile = GetProfile(PrototypePrimaryWeapon.Flamethrower);
        float visibleDistance = GetWeaponDistance(profile.Range, origin, direction);
        float rangeMultiplier = visibleDistance / Mathf.Max(0.01f, profile.Range);
        float spreadMultiplier = HasWeaponUpgrade(PrototypeWeaponUpgrade.SpecialTwo) ? 1.5f : 1f;
        originalFlamethrower.SetExternalVisual(shouldPlay, rangeMultiplier, spreadMultiplier);
    }

    private LineRenderer CreatePersistentLine(string objectName, float startWidth, float endWidth, int sortingOrder)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(transform, false);
        runtimeObjects.Add(lineObject);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = startWidth;
        line.endWidth = endWidth;
        line.sharedMaterial = GetLineMaterial();
        line.sortingOrder = sortingOrder;
        return line;
    }

    private LineRenderer CreateCircleLine(string objectName, Color color, float radius, bool spikes, int sortingOrder)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(playerHealth.transform, false);
        runtimeObjects.Add(lineObject);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = spikes ? 32 : 48;
        line.startWidth = line.endWidth = 0.045f;
        line.startColor = line.endColor = color;
        line.sharedMaterial = GetLineMaterial();
        line.sortingOrder = sortingOrder;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / line.positionCount;
            float pointRadius = spikes && i % 2 == 0 ? radius + 0.16f : radius;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * pointRadius);
        }
        return line;
    }

    private void UpdateEffectPools()
    {
        float now = Time.time;
        for (int i = 0; i < lineEffectPool.Count; i++)
        {
            TimedLineEffect effect = lineEffectPool[i];
            if (effect.Object.activeSelf && now >= effect.ReleaseAt)
                effect.Object.SetActive(false);
        }

        for (int i = 0; i < particleEffectPool.Count; i++)
        {
            TimedParticleEffect effect = particleEffectPool[i];
            if (!effect.Object.activeSelf || !effect.ReleaseScheduled || now < effect.ReleaseAt)
                continue;

            effect.Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.ReleaseScheduled = false;
            effect.Object.SetActive(false);
        }
    }

    private TimedLineEffect AcquireLineEffect(string objectName, float lifetime)
    {
        TimedLineEffect effect = null;
        for (int i = 0; i < lineEffectPool.Count; i++)
        {
            if (!lineEffectPool[i].Object.activeSelf)
            {
                effect = lineEffectPool[i];
                break;
            }
        }

        if (effect == null)
        {
            GameObject lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(transform, false);
            LineRenderer renderer = lineObject.AddComponent<LineRenderer>();
            effect = new TimedLineEffect { Object = lineObject, Renderer = renderer };
            lineEffectPool.Add(effect);
            runtimeObjects.Add(lineObject);
        }

        effect.Object.name = objectName;
        effect.Object.transform.localScale = Vector3.one;
        effect.Object.SetActive(true);
        effect.ReleaseAt = Time.time + Mathf.Max(0.01f, lifetime);
        return effect;
    }

    private void CreateTimedLine(string objectName, Vector2 start, Vector2 end, Color color, float width, float lifetime)
    {
        TimedLineEffect effect = AcquireLineEffect(objectName, lifetime);
        LineRenderer line = effect.Renderer;
        line.loop = false;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = width;
        line.endWidth = width * 0.55f;
        line.startColor = Color.Lerp(Color.white, color, 0.35f);
        line.endColor = color;
        line.sharedMaterial = GetLineMaterial();
        line.sortingOrder = 18;
    }

    private ParticleSystem CreateParticleSystem(string objectName, Color color, float size, float lifetime, int maxParticles)
    {
        TimedParticleEffect effect = null;
        for (int i = 0; i < particleEffectPool.Count; i++)
        {
            if (!particleEffectPool[i].Object.activeSelf)
            {
                effect = particleEffectPool[i];
                break;
            }
        }

        if (effect == null)
        {
            GameObject effectObject = new GameObject(objectName);
            effectObject.SetActive(false);
            effectObject.transform.SetParent(transform, false);
            ParticleSystem createdParticles = effectObject.AddComponent<ParticleSystem>();
            effect = new TimedParticleEffect { Object = effectObject, Particles = createdParticles };
            particleEffectPool.Add(effect);
            runtimeObjects.Add(effectObject);
        }

        effect.Object.name = objectName;
        effect.ReleaseScheduled = false;
        effect.Object.SetActive(true);
        ParticleSystem particles = effect.Particles;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startColor = color;
        main.maxParticles = Mathf.Max(1, maxParticles);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = GetParticleGradient(color);

        ParticleSystemRenderer particleRenderer = effect.Object.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = GetLineMaterial();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sortingOrder = 19;
        return particles;
    }

    private Gradient GetParticleGradient(Color color)
    {
        Color32 key = color;
        if (particleGradients.TryGetValue(key, out Gradient cached))
            return cached;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(color, 0.22f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(color.a, 0f),
                new GradientAlphaKey(color.a, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        particleGradients[key] = gradient;
        return gradient;
    }

    private void ReleaseParticleSystemAfter(ParticleSystem particles, float delay)
    {
        if (particles == null)
            return;

        for (int i = 0; i < particleEffectPool.Count; i++)
        {
            TimedParticleEffect effect = particleEffectPool[i];
            if (effect.Particles != particles)
                continue;

            effect.ReleaseScheduled = true;
            effect.ReleaseAt = Time.time + Mathf.Max(0.01f, delay);
            return;
        }
    }

    private static void EmitParticle(ParticleSystem particles, Vector2 position, Vector2 velocity, float sizeScale)
    {
        if (particles == null)
            return;

        ParticleSystem.EmitParams parameters = new ParticleSystem.EmitParams
        {
            position = position,
            velocity = velocity,
            startSize = particles.main.startSize.constant * Mathf.Max(0.05f, sizeScale),
            startLifetime = particles.main.startLifetime.constant
        };
        particles.Emit(parameters, 1);
    }

    private void CreateDirectionalParticles(string objectName, Vector2 origin, Vector2 direction, Color color,
        int count, float speed, float size, float lifetime, float spreadDegrees)
    {
        ParticleSystem particles = CreateParticleSystem(objectName, color, size, lifetime, count + 4);
        for (int i = 0; i < count; i++)
        {
            float angle = UnityEngine.Random.Range(-spreadDegrees, spreadDegrees);
            Vector2 velocityDirection = Quaternion.Euler(0f, 0f, angle) * direction;
            float velocity = speed * UnityEngine.Random.Range(0.6f, 1.1f);
            EmitParticle(particles, origin + direction * 0.12f, velocityDirection * velocity,
                UnityEngine.Random.Range(0.7f, 1.3f));
        }
        particles.Play();
        ReleaseParticleSystemAfter(particles, lifetime + 0.08f);
    }

    private void CreateLineParticles(string objectName, Vector2 start, Vector2 end, Color color,
        int count, float size, float lifetime)
    {
        ParticleSystem particles = CreateParticleSystem(objectName, color, size, lifetime, count + 4);
        Vector2 line = end - start;
        Vector2 perpendicular = line.sqrMagnitude > 0.001f
            ? new Vector2(-line.y, line.x).normalized
            : Vector2.up;
        for (int i = 0; i < count; i++)
        {
            float t = count <= 1 ? 0f : i / (count - 1f);
            Vector2 position = Vector2.Lerp(start, end, t) + perpendicular * UnityEngine.Random.Range(-0.045f, 0.045f);
            if (rangeIndicator != null)
                position = rangeIndicator.ClampPointToIndicator(position);
            Vector2 velocity = perpendicular * UnityEngine.Random.Range(-0.28f, 0.28f) * Mathf.Sin(t * Mathf.PI);
            EmitParticle(particles, position, velocity, UnityEngine.Random.Range(0.65f, 1.25f));
        }
        particles.Play();
        ReleaseParticleSystemAfter(particles, lifetime + 0.08f);
    }

    private void CreateRadialParticles(string objectName, Vector2 origin, Color color,
        int count, float speed, float size, float lifetime)
    {
        ParticleSystem particles = CreateParticleSystem(objectName, color, size, lifetime, count + 4);
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / Mathf.Max(1, count) + UnityEngine.Random.Range(-0.12f, 0.12f);
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            EmitParticle(particles, origin, direction * speed * UnityEngine.Random.Range(0.55f, 1.15f),
                UnityEngine.Random.Range(0.7f, 1.35f));
        }
        particles.Play();
        ReleaseParticleSystemAfter(particles, lifetime + 0.08f);
    }

    private void CreateRingEffect(Vector2 position, float radius, Color color, float lifetime)
    {
        TimedLineEffect effect = AcquireLineEffect("Armory Pulse", lifetime);
        effect.Object.transform.position = position;
        LineRenderer line = effect.Renderer;
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 48;
        line.startWidth = line.endWidth = 0.08f;
        line.startColor = line.endColor = color;
        line.sharedMaterial = GetLineMaterial();
        line.sortingOrder = 17;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
    }

    private static GameObject CreateCircleObject(string objectName, Color color, float size, int sortingOrder)
    {
        GameObject circle = new GameObject(objectName);
        SpriteRenderer renderer = circle.AddComponent<SpriteRenderer>();
        renderer.sprite = GetCircleSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        circle.transform.localScale = Vector3.one * size;
        return circle;
    }

    private static Material GetLineMaterial()
    {
        if (sharedLineMaterial != null) return sharedLineMaterial;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        sharedLineMaterial = shader != null ? new Material(shader) : null;
        if (sharedLineMaterial != null)
        {
            sharedLineMaterial.name = "Prototype Armory Lines";
            sharedLineMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
        return sharedLineMaterial;
    }

    private static Sprite GetCircleSprite()
    {
        if (sharedCircleSprite != null) return sharedCircleSprite;
        const int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(7.5f, 7.5f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = Vector2.Distance(new Vector2(x, y), center) <= 7f ? Color.white : Color.clear;
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        sharedCircleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        sharedCircleSprite.name = "Prototype Armory Circle";
        sharedCircleSprite.hideFlags = HideFlags.HideAndDontSave;
        return sharedCircleSprite;
    }

    private void OnDestroy()
    {
        StopWeaponAudio();
        ClearCombustionTracking();
        if (originalFlamethrower != null)
            originalFlamethrower.StopParticleEffect(true);
        PlayerStats.OnStatChanged -= HandlePlayerStatChanged;
        if (rangeIndicator != null)
            rangeIndicator.SetRuntimeRangeMultiplier(1f);

        for (int i = 0; i < runtimeObjects.Count; i++)
            if (runtimeObjects[i] != null) Destroy(runtimeObjects[i]);
    }
}
