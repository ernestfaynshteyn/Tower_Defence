using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
public sealed class MegaSlimeBehaviour : MonoBehaviour
{
    [Header("Split On Death")]
    [SerializeField] private GameObject[] miniSlimePrefabs;
    [SerializeField, Min(1)] private int miniSlimeCount = 6;
    [SerializeField, Range(0.1f, 1f)] private float miniScaleMultiplier = 0.55f;
    [SerializeField, Range(0.1f, 1f)] private float miniHealthMultiplier = 0.45f;
    [SerializeField, Range(0.1f, 1f)] private float miniDamageMultiplier = 0.6f;
    [SerializeField, Min(0.1f)] private float miniSpeedMultiplier = 1.15f;

    private EnemyHealth enemyHealth;
    private bool hasSplit;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        enemyHealth.isBoss = true;
    }

    private void OnEnable()
    {
        if (enemyHealth != null)
            enemyHealth.Died += SplitIntoMiniSlimes;
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
            enemyHealth.Died -= SplitIntoMiniSlimes;
    }

    private void SplitIntoMiniSlimes(EnemyHealth defeatedSlime)
    {
        if (hasSplit || miniSlimePrefabs == null || miniSlimePrefabs.Length == 0)
            return;

        hasSplit = true;
        int spawned = 0;
        int requestedCount = Mathf.Max(1, miniSlimeCount);

        for (int i = 0; i < requestedCount; i++)
        {
            GameObject prefab = miniSlimePrefabs[i % miniSlimePrefabs.Length];
            if (prefab == null)
                continue;

            float angle = i * Mathf.PI * 2f / requestedCount;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.45f;
            GameObject mini = Instantiate(prefab, transform.position + offset, Quaternion.identity);
            mini.transform.localScale *= miniScaleMultiplier;

            EnemyHealth miniHealth = mini.GetComponent<EnemyHealth>();
            if (miniHealth != null)
            {
                miniHealth.health = Mathf.Max(1f, miniHealth.health * miniHealthMultiplier);
                miniHealth.moveSpeed = Mathf.Max(0.1f, miniHealth.moveSpeed * miniSpeedMultiplier);
            }

            EnemyDamage miniDamage = mini.GetComponent<EnemyDamage>();
            if (miniDamage != null)
                miniDamage.damageAmount = Mathf.Max(1, Mathf.RoundToInt(miniDamage.damageAmount * miniDamageMultiplier));

            spawned++;
        }

        if (WaveManager.Instance != null)
            WaveManager.Instance.enemyleft += spawned;
    }
}
