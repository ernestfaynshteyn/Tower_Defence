using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
public sealed class IceSlimeBehaviour : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float minimumSlowResistance = 0.9f;

    private void Awake()
    {
        EnemyHealth health = GetComponent<EnemyHealth>();
        health.slowResistance = Mathf.Max(health.slowResistance, minimumSlowResistance);
    }
}
