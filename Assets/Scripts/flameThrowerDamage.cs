using UnityEngine;

public class flameThrowerDamage : MonoBehaviour
{
    [Range(1, 10)]
    [SerializeField] private float flameDamage;

    [SerializeField] private float maxHeat = 100f;
    [SerializeField] private float currentHeat = 0f;
    [SerializeField] private float heatPerSecond = 25f;
    [SerializeField] private float coolingRate = 40f;

    [SerializeField] private ParticleSystem particle;

    private bool overheated = false;

    void Update()
    {
        HandleFlamethrower();
    }

    private void HandleFlamethrower()
    {
        bool firing = Input.GetMouseButton(0);

        // 🔥 FIRING
        if (firing && !overheated)
        {
            if (!particle.isPlaying)
                particle.Play();

            currentHeat += heatPerSecond * Time.deltaTime;

            if (currentHeat >= maxHeat)
            {
                currentHeat = maxHeat;
                overheated = true;
                particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
        // 🧊 COOLING
        else
        {
            if (particle.isPlaying)
                particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            currentHeat -= coolingRate * Time.deltaTime;
            currentHeat = Mathf.Clamp(currentHeat, 0, maxHeat);
        }

        // ♻ RECOVER FROM OVERHEAT
        if (overheated && currentHeat <= 0)
        {
            overheated = false;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!Input.GetMouseButton(0) || overheated)
            return;

        EnemyHealth enemy = collision.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(flameDamage * Time.deltaTime);
        }
    }
}
