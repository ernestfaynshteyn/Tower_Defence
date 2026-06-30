using UnityEngine;
using System.Collections;
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
    private float baseSpeed;
    private bool isStunned;
    private bool isDead;
    public float burnDuration;
    public float burnDamage = 5f;
    private float burnTickTimer;
    private const float burnTickInterval = 0.33f;
    public Animator animator;
    private void Start()
    {
        ApplyDifficulty();
        baseSpeed = moveSpeed;
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        health -= damage;
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
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddMoney(moneyReward);
        }
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.enemyleft -= 1;
        }
        if (animator != null)
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
    private void Update()
    {
        if (burnDuration > 0)
        {
            burnDuration -= Time.deltaTime;
            burnTickTimer -= Time.deltaTime;
            if (burnTickTimer <= 0f)
            {
                TakeDamage(burnDamage);
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

        // Refresh duration (whoever applies burn last keeps it going)
        burnDuration = Mathf.Max(burnDuration, finalDuration);

        // Only upgrade damage per tick, never downgrade it
        if (finalDamage > burnDamage)
            burnDamage = finalDamage;

        // If this is a fresh burn (timer was at 0), start ticking immediately
        if (burnTickTimer <= 0f)
            burnTickTimer = burnTickInterval;
    }

    public void ApplySlow(float multiplier)
    {
        if (isDead) return;
        float resisted = Mathf.Lerp(1f, multiplier, 1f - slowResistance);
        moveSpeed = baseSpeed * resisted;
    }
    public void RemoveSlow()
    {
        if (isDead) return;
        moveSpeed = baseSpeed;
    }
    public void Stun(float duration)
    {
        if (isDead) return;
        if (isStunned) return;
        float resistedDuration = duration * (1f - stunResistance);
        StartCoroutine(StunRoutine(resistedDuration));
    }
    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        moveSpeed = 0f;
        yield return new WaitForSeconds(duration);
        isStunned = false;
        if (!isDead)
        {
            moveSpeed = baseSpeed;
        }
    }
    private void ApplyDifficulty()
    {
        if (GlobalData.Instance == null)
        {
            return;
        }
        switch (GlobalData.Instance.currentDifficulty)
        {
            case Difficulty.Easy:
                health = 75f;
                moveSpeed = 2f;
                break;
            case Difficulty.Normal:
                health = 100f;
                moveSpeed = 2.5f;
                break;
            case Difficulty.Hard:
                health = 125f;
                moveSpeed = 3f;
                break;
            case Difficulty.Extreme:
                health = 150f;
                moveSpeed = 2f;
                break;
        }
    }
}