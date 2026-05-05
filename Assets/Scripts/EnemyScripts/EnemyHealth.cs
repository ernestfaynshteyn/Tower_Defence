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

    public int moneyReward = 10;

    float baseSpeed;
    bool isStunned;
    bool isDead;

    public Animator animator;

    public void Start()
    {
        ApplyDifficulty();

        baseSpeed = moveSpeed;

        animator = GetComponent<Animator>();
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        health -= damage;

        Debug.Log(gameObject.name + " took " + damage + " damage. Health: " + health);

        CheckForHealth();
    }

    public void CheckForHealth()
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

    void Die()
    {
        if (isDead) return;

        isDead = true;

        CurrencyManager.Instance.AddMoney(moneyReward);
        WaveManager.Instance.enemyleft -= 1;

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

    IEnumerator StunRoutine(float duration)
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

    void ApplyDifficulty()
    {
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