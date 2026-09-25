using UnityEngine;

/// <summary>
/// Player-owned screen-clear ability. The skill tree controls its unlock and
/// cooldown bonus; the projectile prefab owns the visual and enemy clear.
/// </summary>
[DisallowMultipleComponent]
public class NukeAbility : MonoBehaviour
{
    [Header("Nuke")]
    [SerializeField] private GameObject nukePrefab;
    [SerializeField] private KeyCode activationKey = KeyCode.N;

    [Header("Cooldown")]
    [SerializeField, Min(1f)] private float baseCooldown = 45f;
    [SerializeField, Min(0.1f)] private float minimumCooldown = 6f;

    private Camera mainCamera;
    private bool isUnlocked;
    private float nextReadyTime;

    public bool IsUnlocked => isUnlocked;
    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        PlayerStats.OnStatChanged += HandleStatChanged;
    }

    private void OnDisable()
    {
        PlayerStats.OnStatChanged -= HandleStatChanged;
    }

    private void Start()
    {
        RefreshUnlock();
    }

    private void Update()
    {
        if (!isUnlocked || Time.time < nextReadyTime || !Input.GetKeyDown(activationKey))
            return;

        LaunchNuke();
    }

    private void HandleStatChanged(string statName, float oldValue, float newValue)
    {
        if (statName == StatNames.NukeUnlocked)
            RefreshUnlock();
    }

    private void RefreshUnlock()
    {
        isUnlocked = PlayerStats.instance != null &&
            PlayerStats.instance.GetModifiedValue(StatNames.NukeUnlocked, 0f) > 0.5f;
    }

    private void LaunchNuke()
    {
        if (nukePrefab == null)
        {
            Debug.LogWarning("NukeAbility needs a Nuke prefab assigned.", this);
            return;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        Vector2 targetPosition = GetTargetPosition();
        GameObject nuke = Instantiate(nukePrefab, targetPosition, Quaternion.identity);
        NukeExplosion nukeExplosion = nuke.GetComponent<NukeExplosion>();

        if (nukeExplosion == null)
        {
            Debug.LogWarning("The Nuke prefab needs a NukeExplosion component.", nuke);
            Destroy(nuke);
            return;
        }

        nukeExplosion.Launch(targetPosition);
        nextReadyTime = Time.time + GetCooldown();
    }

    private Vector2 GetTargetPosition()
    {
        if (mainCamera == null)
            return transform.position;

        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
        return new Vector2(worldPosition.x, worldPosition.y);
    }

    private float GetCooldown()
    {
        float reloadSpeedPercent = PlayerStats.instance != null
            ? PlayerStats.instance.GetPercentModifier(StatNames.ThrowableReloadSpeed)
            : 0f;

        // "Faster reload" stacks as a speed bonus: 35% gives 45 / 1.35 seconds.
        float cooldown = baseCooldown / (1f + Mathf.Max(0f, reloadSpeedPercent) / 100f);
        return Mathf.Max(minimumCooldown, cooldown);
    }
}
