using UnityEngine;

public sealed class ToxicSlimePoisonTrail : MonoBehaviour
{
    private PlayerHealth target;
    private float lifetime;
    private float radius;
    private float damage;
    private float tickInterval;
    private float nextTickTime;

    public void Initialize(
        PlayerHealth playerHealth,
        SpriteRenderer sourceRenderer,
        float trailLifetime,
        float poisonRadius,
        float poisonDamage,
        float damageInterval)
    {
        target = playerHealth;
        lifetime = Mathf.Max(0.1f, trailLifetime);
        radius = Mathf.Max(0.1f, poisonRadius);
        damage = Mathf.Max(0f, poisonDamage);
        tickInterval = Mathf.Max(0.1f, damageInterval);

        if (sourceRenderer != null && sourceRenderer.sprite != null)
        {
            SpriteRenderer trailRenderer = gameObject.AddComponent<SpriteRenderer>();
            trailRenderer.sprite = sourceRenderer.sprite;
            trailRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
            trailRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            trailRenderer.sortingOrder = sourceRenderer.sortingOrder - 1;
            Color trailColor = sourceRenderer.color;
            trailColor.a = 0.4f;
            trailRenderer.color = trailColor;
            transform.localScale = new Vector3(radius * 1.35f, radius * 0.35f, 1f);
        }
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (target == null || damage <= 0f || Time.time < nextTickTime)
            return;

        if ((target.transform.position - transform.position).sqrMagnitude <= radius * radius)
        {
            target.TakeDamage(damage, "5");
            nextTickTime = Time.time + tickInterval;
        }
    }
}
