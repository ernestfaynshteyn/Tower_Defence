using System.Collections;
using UnityEngine;

public class GrenadeDamage : MonoBehaviour
{
    [Header("Grenade Type")]
    public GrenadeType grenadeType;

    [Header("Damage Settings")]
    public float explosionRadius = 2f;
    public float damage = 50f;
    public LayerMask enemyLayer;

    [Header("Explosion Effect")]
    public GameObject explosion;

    [Header("Flashbang Cube Effect")]
    public float flashCubeFadeDuration = 3f;

    private bool hasExploded = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Explosion();
    }

    public void Explosion()
    {
        if (hasExploded) return;

        hasExploded = true;

        Collider2D[] enemiesHit = Physics2D.OverlapCircleAll(
            transform.position,
            explosionRadius,
            enemyLayer
        );

        foreach (Collider2D enemyCollider in enemiesHit)
        {
            EnemyHealth enemyHealth = enemyCollider.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
        }

        if (grenadeType == GrenadeType.Flash)
        {
            SpawnFlashCube();
        }

        GameObject explosionEffect = SpawnExplosionEffect();
        if (grenadeType == GrenadeType.Frag)
        {
            FragExplosionEffect.Play(explosionEffect);
        }

        Destroy(gameObject);
    }

    public GameObject SpawnExplosionEffect()
    {
        if (explosion != null)
            return Instantiate(explosion, transform.position, Quaternion.identity);

        return null;
    }

    /// <summary>
    /// Plays the authored particle systems stored in the frag explosion prefab.
    /// Particle timings and appearance live in the prefab so they remain editable
    /// in Unity's Inspector.
    /// </summary>
    public static void ConfigureFragExplosionEffect(GameObject effectInstance)
    {
        if (effectInstance == null)
            return;

        foreach (ParticleSystem particles in effectInstance.GetComponentsInChildren<ParticleSystem>(true))
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        ParticleSystem rootParticles = effectInstance.GetComponent<ParticleSystem>();
        if (rootParticles != null)
        {
            rootParticles.Play(true);
        }
        else
        {
            foreach (ParticleSystem particles in effectInstance.GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Play(true);
            }
        }

        Destroy(effectInstance, 1.95f);
    }

    private void SpawnFlashCube()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        cube.transform.position = transform.position;
        cube.transform.localScale = new Vector3(3f, 3f, 3f);

        Collider cubeCollider = cube.GetComponent<Collider>();

        if (cubeCollider != null)
        {
            Destroy(cubeCollider);
        }

        Renderer renderer = cube.GetComponent<Renderer>();

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Transparent");
        }

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        Material mat = new Material(shader);

        mat.SetColor("_Color", Color.white);

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", Color.white);
        }

        mat.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0);
        mat.renderQueue = 3000;

        renderer.material = mat;

        FlashCubeFade fade = cube.AddComponent<FlashCubeFade>();
        fade.StartFade(mat, flashCubeFadeDuration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}

public class FlashCubeFade : MonoBehaviour
{
    public void StartFade(Material mat, float fadeDuration)
    {
        StartCoroutine(FadeOutAndDestroy(mat, fadeDuration));
    }

    private IEnumerator FadeOutAndDestroy(Material mat, float fadeDuration)
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);

            Color fadeColor = new Color(1f, 1f, 1f, alpha);

            mat.SetColor("_Color", fadeColor);

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", fadeColor);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
