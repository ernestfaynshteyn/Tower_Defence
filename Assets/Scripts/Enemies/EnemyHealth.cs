using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
public class EnemyHealth : MonoBehaviour
{
    [Header("Stats")]
    public float health = 100f;
    public float moveSpeed = 0f;
    [Header("Resistances")]
    [Range(0f, 1f)] public float slowResistance = 0f;
    [Range(0f, 1f)] public float stunResistance = 0f;
    [Range(0f, 1f)] public float burnResistance = 0f;
    public int moneyReward = 10;
    [Tooltip("Boss enemies use the boss-defeat sound when they die.")]
    public bool isBoss;
    [Tooltip("Enable only while diagnosing damage calculations. Per-hit logging is expensive.")]
    public bool logDamage;
    private float baseSpeed;
    private float slowMultiplier = 1f;
    private bool isStunned;
    private float stunnedUntil;
    private Coroutine stunCoroutine;
    private bool isDead;
    private bool recordedBurn;
    private bool recordedPoison;
    private bool recordedSlow;
    private bool recordedStun;
    private bool recordedShock;
    private readonly Dictionary<DamageNumberType, DamageNumberPopup> damageNumberPopups =
        new Dictionary<DamageNumberType, DamageNumberPopup>();
    public float burnDuration;
    public float burnDamage = 5f;
    private float burnTickTimer;
    private const float burnTickInterval = 0.33f;
    public Animator animator;
    public event Action<EnemyHealth, float> Damaged;
    public event Action<EnemyHealth> Died;
    public float MaxHealth { get; private set; }
    public float CurrentHealth => Mathf.Max(0f, health);
    public float CurrentMoveSpeed => isDead || isStunned ? 0f : baseSpeed * slowMultiplier;

    private void Start()
    {
        ApplyDifficulty();
        ApplyWaveScaling();
        MaxHealth = health;
        baseSpeed = moveSpeed;
        RefreshMoveSpeed();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (isBoss)
            WaveManager.Instance?.RegisterBoss(this);
    }
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, DamageNumberType.Normal);
    }

    public void TakeDamage(float damage, DamageNumberType damageType)
    {
        ApplyDamage(damage, damageType);
    }

    public void TakeBurnDamage(float damage)
    {
        ApplyDamage(damage, DamageNumberType.Burn);
    }

    private void ApplyDamage(float damage, DamageNumberType damageType)
    {
        if (isDead) return;
        float appliedDamage = Mathf.Min(damage, Mathf.Max(health, 0f));
        health -= damage;
        if (appliedDamage > 0f)
        {
            GameRecords.RecordDamage(appliedDamage);
            if (damageType == DamageNumberType.Poison)
                RecordStatusOnce(StatusEffectRecord.Poisoned);
            else if (damageType == DamageNumberType.ElectricChain)
                RecordStatusOnce(StatusEffectRecord.Shocked);

            Damaged?.Invoke(this, appliedDamage);

            if (!damageNumberPopups.TryGetValue(damageType, out DamageNumberPopup popup) || popup == null)
            {
                popup = DamageNumberPopup.Create(transform, damageType);
                damageNumberPopups[damageType] = popup;
            }
            if (popup != null)
                popup.AddDamage(appliedDamage);
        }

        if (damage > 0f && health > 0f) BackgroundMusic.EnemyHit(false);
        if (logDamage)
            Debug.Log(gameObject.name + " took " + damage + " damage. Health: " + health);
        CheckForHealth();
    }
    private void CheckForHealth()
    {
        if (health <= 75f)
        {
            // animator.SetTrigger("HurtPhase");
        }
        if (health <= 0f)
        {
            Die();
        }
    }
    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Died?.Invoke(this);
        if (isBoss)
            BackgroundMusic.BossDefeated();
        else
            BackgroundMusic.EnemyHit(true);
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddMoney(moneyReward);
        }
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.enemyleft -= 1;
        }
        if (animator != null && HasAnimatorParameter(animator, "Die"))
        {
            animator.SetTrigger("Die");
        }
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
        moveSpeed = 0f;
        Destroy(gameObject, 1f);
    }

    private static bool HasAnimatorParameter(Animator targetAnimator, string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in targetAnimator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == parameterName)
                return true;
        }

        return false;
    }
    private void Update()
    {
        if (burnDuration > 0)
        {
            burnDuration -= Time.deltaTime;
            burnTickTimer -= Time.deltaTime;
            if (burnTickTimer <= 0f)
            {
                TakeBurnDamage(burnDamage);
                burnTickTimer = burnTickInterval;
            }
        }
        else
        {
            burnTickTimer = 0f; // Reset when burn ends
            burnDamage = 0f;    // Clear so the next ApplyBurn starts clean
        }
    }

    /// <summary>
    /// Applies (or refreshes) a burn effect, scaled by this enemy's burnResistance.
    /// Duration always refreshes to the longer value; damage only upgrades,
    /// so a weaker burn source (e.g. Flamethrower) can't downgrade a stronger
    /// one (e.g. Molotov) that's already active.
    /// </summary>
    public void ApplyBurn(float duration, float damagePerTick)
    {
        if (isDead) return;

        float finalDuration = duration * (1f - burnResistance);
        float finalDamage = damagePerTick * (1f - burnResistance);

        if (finalDuration <= 0f || finalDamage <= 0f)
            return; // fully resistant to burn

        RecordStatusOnce(StatusEffectRecord.Burned);

        bool isFreshBurn = burnDuration <= 0f;

        // Refresh duration (whoever applies burn last keeps it going)
        burnDuration = Mathf.Max(burnDuration, finalDuration);

        // A fresh burn must use its own damage, rather than the serialized
        // value left over from a previous effect. Active burns only upgrade.
        if (isFreshBurn)
            burnDamage = finalDamage;
        else if (finalDamage > burnDamage)
            burnDamage = finalDamage;

        // If this is a fresh burn (timer was at 0), start ticking immediately
        if (burnTickTimer <= 0f)
            burnTickTimer = burnTickInterval;
    }

    public void ApplySlow(float multiplier)
    {
        if (isDead) return;
        float resisted = Mathf.Lerp(1f, Mathf.Clamp01(multiplier), 1f - slowResistance);
        if (resisted < 0.999f)
            RecordStatusOnce(StatusEffectRecord.Slowed);
        slowMultiplier = resisted;
        RefreshMoveSpeed();
    }
    public void RemoveSlow()
    {
        if (isDead) return;
        slowMultiplier = 1f;
        RefreshMoveSpeed();
    }

    /// <summary>Moves this enemy away from an origin. Reusable by pulse-style abilities.</summary>
    public void PushAway(Vector2 origin, float distance)
    {
        if (isDead || distance <= 0f)
            return;

        Vector2 direction = (Vector2)transform.position - origin;
        if (direction.sqrMagnitude < 0.0001f)
            direction = UnityEngine.Random.insideUnitCircle.normalized;
        else
            direction.Normalize();

        transform.position += (Vector3)(direction * distance);
    }

    public void Stun(float duration)
    {
        if (isDead) return;
        float resistedDuration = duration * (1f - stunResistance);
        if (resistedDuration <= 0f) return;
        RecordStatusOnce(StatusEffectRecord.Stunned);

        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + resistedDuration);
        if (stunCoroutine == null)
            stunCoroutine = StartCoroutine(StunRoutine());
    }

    public void RecordStatusEffect(StatusEffectRecord effect)
    {
        if (!isDead)
            RecordStatusOnce(effect);
    }
    private IEnumerator StunRoutine()
    {
        isStunned = true;
        RefreshMoveSpeed();

        while (Time.time < stunnedUntil && !isDead)
            yield return null;

        isStunned = false;
        stunCoroutine = null;
        if (!isDead)
        {
            RefreshMoveSpeed();
        }
    }

    private void RefreshMoveSpeed()
    {
        moveSpeed = CurrentMoveSpeed;
    }

    private void RecordStatusOnce(StatusEffectRecord effect)
    {
        switch (effect)
        {
            case StatusEffectRecord.Burned:
                if (recordedBurn) return;
                recordedBurn = true;
                break;
            case StatusEffectRecord.Poisoned:
                if (recordedPoison) return;
                recordedPoison = true;
                break;
            case StatusEffectRecord.Slowed:
                if (recordedSlow) return;
                recordedSlow = true;
                break;
            case StatusEffectRecord.Stunned:
                if (recordedStun) return;
                recordedStun = true;
                break;
            case StatusEffectRecord.Shocked:
                if (recordedShock) return;
                recordedShock = true;
                break;
        }

        GameRecords.RecordStatusEffect(effect);
    }
    private void ApplyDifficulty()
    {
        float healthMultiplier = 1f;
        float speedMultiplier = 1f;

        switch (GlobalData.ActiveDifficulty)
        {
            case Difficulty.Easy:
                healthMultiplier = 0.75f;
                speedMultiplier = 0.8f;
                break;
            case Difficulty.Normal:
                break;
            case Difficulty.Hard:
                healthMultiplier = 1.25f;
                speedMultiplier = 1.15f;
                break;
            case Difficulty.Extreme:
                healthMultiplier = 1.5f;
                speedMultiplier = 1.3f;
                break;
        }

        // Scale each prefab's authored stats so blue/green/red enemies and
        // bosses keep their intended differences on every difficulty.
        health = Mathf.Max(1f, health * healthMultiplier);
        moveSpeed = Mathf.Max(0f, moveSpeed * speedMultiplier);
    }

    private void ApplyWaveScaling()
    {
        WaveManager waves = WaveManager.Instance;
        if (waves == null)
            return;

        health = Mathf.Max(1f, health * waves.GetEnemyHealthMultiplierForCurrentWave());
        moveSpeed = Mathf.Max(0f, moveSpeed * waves.GetEnemySpeedMultiplierForCurrentWave());
        slowResistance = waves.GetEnemyResistanceForCurrentWave(slowResistance);
        stunResistance = waves.GetEnemyResistanceForCurrentWave(stunResistance);
        burnResistance = waves.GetEnemyResistanceForCurrentWave(burnResistance);
    }
}
