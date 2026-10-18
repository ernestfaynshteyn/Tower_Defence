using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
public sealed class SlimeCombatVfx : MonoBehaviour
{
    public enum AttackEffect
    {
        Basic,
        Ice,
        Acid,
        Electric,
        Toxic,
        Mega
    }

    [Header("Slime VFX")]
    [Tooltip("Color used by this slime's hit, attack, and death particles.")]
    [SerializeField] private Color effectColor = new Color(0.15f, 1f, 0.25f, 1f);
    [Tooltip("Controls the shape and intensity of this slime's attack effect.")]
    [SerializeField] private AttackEffect attackEffect = AttackEffect.Basic;

    private EnemyHealth enemyHealth;

    private static Material sharedParticleMaterial;
    private static Texture2D sharedParticleTexture;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        enemyHealth.Damaged += HandleDamaged;
        enemyHealth.Died += HandleDied;
    }

    private void OnDisable()
    {
        if (enemyHealth == null)
            return;

        enemyHealth.Damaged -= HandleDamaged;
        enemyHealth.Died -= HandleDied;
    }

    private void HandleDamaged(EnemyHealth _, float damage)
    {
        if (damage <= 0f)
            return;

        CreateBurst(
            "Slime Hit Particles",
            transform.position,
            effectColor,
            5,
            new Vector2(0.18f, 0.35f),
            new Vector2(0.45f, 0.9f),
            new Vector2(0.06f, 0.12f),
            0f,
            0.08f);
    }

    private void HandleDied(EnemyHealth _)
    {
        Vector3 deathPosition = transform.position;

        if (attackEffect == AttackEffect.Ice)
            BackgroundMusic.IceSlimeShattered();

        CreateBurst(
            "Slime Death Splash",
            deathPosition,
            effectColor,
            18,
            new Vector2(0.65f, 1.15f),
            new Vector2(0.65f, 1.6f),
            new Vector2(0.09f, 0.24f),
            0.22f,
            0.12f);

        // A few almost-stationary particles form a temporary splat at the
        // death position after the airborne droplets disappear.
        CreateBurst(
            "Slime Death Splat",
            deathPosition,
            effectColor,
            7,
            new Vector2(2.2f, 3.2f),
            new Vector2(0f, 0.08f),
            new Vector2(0.28f, 0.62f),
            0f,
            0.18f);
    }

    public void PlayAttackEffect(Vector3 impactPosition)
    {
        switch (attackEffect)
        {
            case AttackEffect.Ice:
                PlayIceImpact(impactPosition);
                break;
            case AttackEffect.Acid:
                BackgroundMusic.AcidSlimeAttacked();
                PlayAcidImpact(impactPosition);
                break;
            case AttackEffect.Electric:
                BackgroundMusic.ElectricSlimeAttacked();
                PlayElectricImpact(impactPosition);
                break;
            case AttackEffect.Toxic:
                PlayToxicImpact(impactPosition);
                break;
            case AttackEffect.Mega:
                PlayMegaImpact(impactPosition);
                break;
            default:
                PlayBasicImpact(impactPosition);
                break;
        }
    }

    private void PlayBasicImpact(Vector3 position)
    {
        CreateBurst("Slime Attack Impact", position, effectColor, 7,
            new Vector2(0.2f, 0.38f), new Vector2(0.35f, 0.8f),
            new Vector2(0.07f, 0.14f), 0f, 0.06f);
    }

    private void PlayIceImpact(Vector3 position)
    {
        CreateBurst("Ice Slime Attack", position, effectColor, 11,
            new Vector2(0.35f, 0.65f), new Vector2(0.5f, 1.15f),
            new Vector2(0.05f, 0.13f), 0f, 0.08f);
    }

    private void PlayAcidImpact(Vector3 position)
    {
        CreateBurst("Acid Slime Attack", position, effectColor, 13,
            new Vector2(0.45f, 0.9f), new Vector2(0.4f, 1.25f),
            new Vector2(0.07f, 0.18f), 0.35f, 0.1f);
    }

    private void PlayElectricImpact(Vector3 position)
    {
        CreateLightningBolt(transform.position, position);
        CreateBurst("Electric Slime Sparks", position, effectColor, 16,
            new Vector2(0.16f, 0.42f), new Vector2(0.8f, 1.8f),
            new Vector2(0.04f, 0.1f), 0f, 0.05f);
    }

    private void PlayToxicImpact(Vector3 position)
    {
        CreateBurst("Toxic Slime Cloud", position, effectColor, 14,
            new Vector2(0.65f, 1.15f), new Vector2(0.12f, 0.45f),
            new Vector2(0.12f, 0.3f), -0.04f, 0.15f);
    }

    private void PlayMegaImpact(Vector3 position)
    {
        CreateBurst("Mega Slime Attack", position, effectColor, 22,
            new Vector2(0.5f, 1f), new Vector2(0.65f, 1.6f),
            new Vector2(0.1f, 0.26f), 0.12f, 0.14f);
    }

    private void CreateLightningBolt(Vector3 start, Vector3 end)
    {
        GameObject boltObject = new GameObject("Electric Slime Lightning");
        LineRenderer line = boltObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 7;
        line.startWidth = 0.07f;
        line.endWidth = 0.025f;
        line.startColor = Color.Lerp(Color.white, effectColor, 0.45f);
        line.endColor = effectColor;
        line.numCapVertices = 2;
        line.sortingOrder = 8;
        line.sharedMaterial = GetParticleMaterial();

        Vector3 direction = end - start;
        Vector3 perpendicular = direction.sqrMagnitude > 0.0001f
            ? Vector3.Cross(direction.normalized, Vector3.forward)
            : Vector3.up;

        for (int i = 0; i < line.positionCount; i++)
        {
            float t = i / (line.positionCount - 1f);
            Vector3 point = Vector3.Lerp(start, end, t);
            if (i > 0 && i < line.positionCount - 1)
                point += perpendicular * Random.Range(-0.14f, 0.14f);
            line.SetPosition(i, point);
        }

        Destroy(boltObject, 0.14f);
    }

    private static void CreateBurst(
        string objectName,
        Vector3 position,
        Color color,
        int count,
        Vector2 lifetimeRange,
        Vector2 speedRange,
        Vector2 sizeRange,
        float gravity,
        float radius)
    {
        GameObject particleObject = new GameObject(objectName);
        particleObject.transform.position = position;

        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.1f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeRange.x, lifetimeRange.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedRange.x, speedRange.y);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startColor = color;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(1, count);
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = radius > 0f;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.72f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = fade;

        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = GetParticleMaterial();
        particleRenderer.sortingOrder = 7;

        particles.Emit(count);
        particles.Play();
    }

    private static Material GetParticleMaterial()
    {
        if (sharedParticleMaterial != null)
            return sharedParticleMaterial;

        Shader shader = Shader.Find("TowerDefense/SlimeRecolor");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
            return null;

        sharedParticleMaterial = new Material(shader)
        {
            name = "Runtime Slime VFX Material",
            hideFlags = HideFlags.HideAndDontSave
        };

        sharedParticleTexture = CreateParticleTexture();
        sharedParticleMaterial.mainTexture = sharedParticleTexture;
        return sharedParticleMaterial;
    }

    private static Texture2D CreateParticleTexture()
    {
        const int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime Slime Particle",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.45f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * size + x] = distance <= radius ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }
}
