using UnityEngine;
using System.Collections;
public class EnemyHealth : MonoBehaviour
    {

        [Header("Stats")]
        public float health = 100f;
        public float moveSpeed = 0f;

        [Header("Resistances")]
        [Range(0f, 1f)] public float slowResistance = 0f; // 0 = full effect
        [Range(0f, 1f)] public float stunResistance = 0f;

        public int moneyReward = 10;

        float baseSpeed;
        bool isStunned;



        public Animator animator;

        public void Start()
        {
            ApplyDifficulty();
            animator = GetComponent<Animator>();
        }

        public void TakeDamage(float damage)
        {
            health -= damage;
            CheckForHealth();

        }
        public void CheckForHealth()
        {
            if (health <= 75f)
            {
                //animator.SetTrigger("HurtPhase");
            }
            if (health <= 0f)
            {
                Debug.Log("CurrencyManager Instance: " + CurrencyManager.Instance);
                CurrencyManager.Instance.AddMoney(moneyReward);

                WaveManager.Instance.enemyleft = WaveManager.Instance.enemyleft - 1;
                Destroy(gameObject);
                Die();
            }
        }
        void Die()
        {
            //CurrencyManager.Instance.AddMoney(moneyReward);
        

            animator.SetTrigger("Die");
            // Disable the enemy
            GetComponent<Collider2D>().enabled = false;
            this.enabled = false;
        }

        public void ApplySlow(float multiplier)
        {
            float resisted = Mathf.Lerp(1f, multiplier, 1f - slowResistance);
            moveSpeed = baseSpeed * resisted;
        }

        public void RemoveSlow()
        {
            moveSpeed = baseSpeed;
        }

        // =====================
        // STUN (FLASH)
        // =====================
        public void Stun(float duration)
        {
            if (isStunned) return;

            float resistedDuration = duration * (1f - stunResistance);
            StartCoroutine(StunRoutine(resistedDuration));
        }

        System.Collections.IEnumerator StunRoutine(float duration)
        {
            isStunned = true;
            yield return new WaitForSeconds(duration);
            isStunned = false;
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