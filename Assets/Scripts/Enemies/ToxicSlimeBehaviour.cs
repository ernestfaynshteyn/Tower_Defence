using UnityEngine;

[DisallowMultipleComponent]
public sealed class ToxicSlimeBehaviour : MonoBehaviour
{
    [Header("Poison Trail")]
    [SerializeField, Min(0.25f)] private float trailSpacing = 1.5f;
    [SerializeField, Min(0.1f)] private float trailLifetime = 3f;
    [SerializeField, Min(0.1f)] private float poisonRadius = 1.25f;
    [SerializeField, Min(0f)] private float poisonDamage = 4f;
    [SerializeField, Min(0.1f)] private float damageInterval = 0.75f;

    private SpriteRenderer sourceRenderer;
    private PlayerHealth playerHealth;
    private Vector3 lastTrailPosition;

    private void Start()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();

        lastTrailPosition = transform.position;
        SpawnTrail();
    }

    private void Update()
    {
        if ((transform.position - lastTrailPosition).sqrMagnitude < trailSpacing * trailSpacing)
            return;

        lastTrailPosition = transform.position;
        SpawnTrail();
    }

    private void SpawnTrail()
    {
        BackgroundMusic.ToxicSlimeTrailed();
        var trailObject = new GameObject("Toxic Slime Poison Trail");
        trailObject.transform.position = transform.position;
        var trail = trailObject.AddComponent<ToxicSlimePoisonTrail>();
        trail.Initialize(
            playerHealth,
            sourceRenderer,
            trailLifetime,
            poisonRadius,
            poisonDamage,
            damageInterval);
    }
}
