using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public Transform target;
    public float speed = 3f;
    public void Start()
    {
        ApplyDifficulty();
        GameObject playerObj = GameObject.FindWithTag("Player");

        if (playerObj != null)
        {
            target = playerObj.transform;
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
            float step = speed * Time.deltaTime;

            // Move the current object's position towards the target's position
            if (Vector3.Distance(target.position, transform.position) > 1.5f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target.position, step);
            }
        }
    }
    void ApplyDifficulty()
    {
        switch (GlobalData.Instance.currentDifficulty)
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