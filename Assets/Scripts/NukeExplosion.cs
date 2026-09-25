using System.Collections;
using UnityEngine;

/// <summary>
/// Visual impact for the Nuke prefab. The blast intentionally damages every
/// active EnemyHealth so it clears the current screen/wave reliably.
/// </summary>
[DisallowMultipleComponent]
public class NukeExplosion : MonoBehaviour
{
    [Header("Impact")]
    [SerializeField, Min(0f)] private float fallHeight = 12f;
    [SerializeField, Min(0.05f)] private float fallDuration = 0.65f;
    [SerializeField, Min(0.1f)] private float blastVisualRadius = 12f;
    [SerializeField, Min(0.05f)] private float blastVisualDuration = 0.65f;

    [Header("Explosion Effect")]
    [SerializeField] private GameObject explosionEffect;
    [SerializeField, Min(0.1f)] private float explosionEffectScale = 7f;

    private bool hasLaunched;
    private bool hasDetonated;
    private Material blastMaterial;
    private SpriteRenderer spriteRenderer;

    public void Launch(Vector2 targetPosition)
    {
        if (hasLaunched)
            return;

        hasLaunched = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
        StartCoroutine(FallAndDetonate(targetPosition));
    }

    private void Start()
    {
        if (!hasLaunched)
            Launch(transform.position);
    }

    private IEnumerator FallAndDetonate(Vector2 targetPosition)
    {
        Vector2 startPosition = targetPosition + Vector2.up * fallHeight;
        transform.position = startPosition;

        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fallDuration);
            transform.position = Vector2.Lerp(startPosition, targetPosition, progress * progress);
            yield return null;
        }

        transform.position = targetPosition;
        Detonate();
    }

    private void Detonate()
    {
        if (hasDetonated)
            return;

        hasDetonated = true;
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        SpawnExplosionEffect();
        DestroyAllActiveEnemies();
        StartCoroutine(PlayBlastRing());
    }

    private void SpawnExplosionEffect()
    {
        if (explosionEffect == null)
            return;

        GameObject effect = Instantiate(explosionEffect, transform.position, Quaternion.identity);
        effect.transform.localScale *= explosionEffectScale;
        FragExplosionEffect.Play(effect);
    }

    private static void DestroyAllActiveEnemies()
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
                enemy.TakeDamage(float.MaxValue);
        }
    }

    private IEnumerator PlayBlastRing()
    {
        LineRenderer blastRing = CreateBlastRing();
        float elapsed = 0f;

        while (elapsed < blastVisualDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / blastVisualDuration);
            float radius = Mathf.Lerp(0.35f, blastVisualRadius, progress);
            float alpha = 1f - progress;

            blastRing.transform.localScale = Vector3.one * radius;
            Color color = Color.Lerp(new Color(1f, 0.95f, 0.55f, alpha), new Color(1f, 0.12f, 0.02f, 0f), progress);
            blastRing.startColor = color;
            blastRing.endColor = color;
            blastRing.startWidth = 0.16f / Mathf.Max(0.35f, radius);
            blastRing.endWidth = blastRing.startWidth;
            yield return null;
        }

        Destroy(gameObject);
    }

    private LineRenderer CreateBlastRing()
    {
        GameObject ringObject = new GameObject("Nuke Blast Ring");
        ringObject.transform.SetParent(transform, false);

        LineRenderer ring = ringObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.alignment = LineAlignment.View;
        ring.numCapVertices = 2;
        ring.sortingOrder = 20;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

        if (shader != null)
        {
            blastMaterial = new Material(shader);
            ring.material = blastMaterial;
        }

        const int segments = 48;
        ring.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
        }

        return ring;
    }

    private void OnDestroy()
    {
        if (blastMaterial != null)
            Destroy(blastMaterial);
    }
}
