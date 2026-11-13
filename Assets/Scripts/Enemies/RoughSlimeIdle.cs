using UnityEngine;

/// <summary>Subtle visual wobble on the child sprite; the enemy hurtbox stays still.</summary>
[DisallowMultipleComponent]
public sealed class RoughSlimeIdle : MonoBehaviour
{
    [SerializeField] private float speed = 4.5f;
    [SerializeField] private float stretch = 0.045f;
    [SerializeField] private float bob = 0.025f;

    private Vector3 restingPosition;
    private Vector3 restingScale;
    private float phase;

    private void Awake()
    {
        restingPosition = transform.localPosition;
        restingScale = transform.localScale;
        phase = (GetInstanceID() & 255) * 0.024f;
    }

    private void Update()
    {
        float wave = Mathf.Sin(Time.time * speed + phase);
        transform.localPosition = restingPosition + Vector3.up * (wave * bob);
        transform.localScale = new Vector3(
            restingScale.x * (1f + wave * stretch),
            restingScale.y * (1f - wave * stretch),
            restingScale.z);
    }
}
