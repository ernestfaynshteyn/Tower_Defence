using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public Transform target;
    public float speed = 3f;
    private EnemyHealth enemyHealth;
    private Collider2D bodyCollider;
    private Collider2D targetCollider;

    public void Start()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        bodyCollider = GetComponent<Collider2D>();
        if (enemyHealth == null)
            ApplyDifficulty();

        GameObject playerObj = GameObject.FindWithTag("Player");

        if (playerObj != null)
        {
            target = playerObj.transform;
            targetCollider = playerObj.GetComponent<Collider2D>();
        }
        else
        {
            Debug.LogWarning("EnemyMovement: No object with tag 'Player' found. Using self as fallback.");
            target = transform; // Fallback (you can change this)
        }
    }

    void Update()
    {
        if (target != null)
        {
            // Calculate the step to move this frame    
            float activeSpeed = enemyHealth != null ? enemyHealth.CurrentMoveSpeed : speed;
            float step = activeSpeed * Time.deltaTime;

            // Move the current object's position towards the target's position
            // Approach the actual tower hurtbox, not a fixed centre distance.
            // Small slimes previously stopped outside it, especially on the axes.
            if (targetCollider == null || bodyCollider == null ||
                !bodyCollider.Distance(targetCollider).isOverlapped)
            {
                transform.position = Vector3.MoveTowards(transform.position, target.position, step);
            }
        }
    }
    void ApplyDifficulty()
    {
        switch (GlobalData.ActiveDifficulty)
        {
            case Difficulty.Easy:
                speed = 2f;
                break;

            case Difficulty.Normal:
                speed = 3f;
                break;

            case Difficulty.Hard:
                speed = 3.5f;
                break;

            case Difficulty.Extreme:
                speed = 4f;
                break;
        }
    }
}
