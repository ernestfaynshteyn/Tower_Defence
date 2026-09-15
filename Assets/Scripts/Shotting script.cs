using UnityEngine;

public class Shottingscript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firingPoint;
    [SerializeField] private UseGrenade usingGrenades;

    [Header("Gun Stats")]
    [SerializeField] private float damage = 50f;

    [Range(0.00000000001f, 3f)]
    [SerializeField] private float firingrate = 0.1f;

    [Range(0f, 10f)]
    [SerializeField] private float spread = 0.5f;

    [Range(1f, 12f)]
    [SerializeField] private int bulletPerShot = 1;

    private float MaxHeat = 100;

    [Range(0, 100f)]
    [SerializeField] private float currentHeat = 0;

    [SerializeField] private float HeatPerSec;
    [SerializeField] private float coolingRate;

    [Range(0, 100)]
    [SerializeField] private int maxMagSize;

    [SerializeField] private int currentMag = 0;
    [SerializeField] private float reloadTime = 1;

    private bool overheated = false;
    private bool canShoot = true;
    private bool reloading = false;

    private void Start()
    {
        currentMag = maxMagSize;

        if (usingGrenades == null)
        {
            usingGrenades = GetComponentInParent<UseGrenade>();
        }

        if (firingPoint == null)
        {
            firingPoint = transform;
            Debug.LogWarning($"[{name}] has no firing point assigned; using the weapon transform instead.", this);
        }
    }

    private void Update()
    {
        HandlingShooting();
    }

    private float GetModifiedDamage()
    {
        if (PlayerStats.instance == null)
        {
            return damage;
        }

        return PlayerStats.instance.GetModifiedValue(StatNames.AttackDamage, damage);
    }

    private float GetPercentModifier(string statName)
    {
        return PlayerStats.instance != null ? PlayerStats.instance.GetPercentModifier(statName) : 0f;
    }

    private float GetFiringDelay()
    {
        float speedBonus = GetPercentModifier(StatNames.AttackSpeed) +
                           GetPercentModifier(StatNames.CooldownReduction);
        return firingrate / Mathf.Max(0.01f, 1f + speedBonus / 100f);
    }

    private float GetReloadTime()
    {
        float reloadBonus = GetPercentModifier(StatNames.ReloadSpeed);
        return reloadTime / Mathf.Max(0.01f, 1f + reloadBonus / 100f);
    }

    private void HandlingShooting()
    {
        if (usingGrenades != null && usingGrenades.isEquipped)
        {
            return;
        }

        if (Input.GetMouseButton(0) && canShoot && !overheated && !reloading)
        {
            canShoot = false;

            for (int i = 0; i < bulletPerShot; i++)
            {
                currentHeat += HeatPerSec;

                if (currentHeat >= MaxHeat)
                {
                    currentHeat = MaxHeat;
                    overheated = true;
                    return;
                }

                Shoot();
                currentMag -= 1;

                if (currentMag <= 0)
                {
                    reloading = true;
                    Invoke(nameof(Reload), GetReloadTime());
                    break;
                }
            }

            Invoke(nameof(CanShot), GetFiringDelay());
        }

        if (!Input.GetMouseButton(0) && currentHeat > 0)
        {
            currentHeat -= coolingRate * Time.deltaTime;
            currentHeat = Mathf.Clamp(currentHeat, 0, MaxHeat);
        }

        if (overheated && currentHeat <= 0)
        {
            overheated = false;
            canShoot = true;
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError($"[{name}] cannot shoot because no bullet prefab is assigned.", this);
            return;
        }

        GameObject bullet = Instantiate(bulletPrefab, firingPoint.position, Quaternion.identity);

        Vector3 directionOffset = new Vector3(
            Random.Range(-spread, spread),
            Random.Range(-spread, spread),
            0
        );

        Bullet bulletScript = bullet.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            float rangeMultiplier = 1f + GetPercentModifier(StatNames.Range) / 100f;
            float critChance = Mathf.Clamp01(GetPercentModifier(StatNames.CritChance) / 100f);
            float critMultiplier = 2f * (1f + GetPercentModifier(StatNames.CritDamage) / 100f);
            float lifestealChance = Mathf.Clamp01(GetPercentModifier(StatNames.LifestealChance) / 100f);
            float lifestealAmount = Mathf.Max(0f, GetPercentModifier(StatNames.LifestealAmount) / 100f);

            bulletScript.Setup(
                GetModifiedDamage(),
                bulletScript.BaseSpeed,
                bulletScript.BaseMaxDistance * Mathf.Max(0.01f, rangeMultiplier),
                critChance,
                critMultiplier,
                lifestealChance,
                lifestealAmount,
                GetComponentInParent<PlayerHealth>(),
                transform
            );
            bulletScript.direction = transform.right + directionOffset;
        }

        bullet.transform.right = transform.right;
    }

    private void CanShot()
    {
        canShoot = true;
    }

    private void Reload()
    {
        reloading = false;
        currentMag = maxMagSize;
    }

    public float GetDamage()
    {
        return GetModifiedDamage();
    }

    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
}
