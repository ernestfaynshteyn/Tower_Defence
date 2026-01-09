using UnityEngine;

public class Shottingscript : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firingPoint;
    [Range(0.00000000001f, 3f)]
    [SerializeField] private float firingrate = 0.1f;
    [Range(0f, 10f)]
    [SerializeField] private float spread = 0.5f;
    [Range(1f, 12f)]
    [SerializeField] private int bulletPerShot = 1;
    private float MaxHeat=100;
    [Range(0, 100f)]
    [SerializeField] private float currentHeat = 0;
    [SerializeField] private float HeatPerSec;
    [SerializeField] private float coolingRate; 
    [Range(0, 100)]
    [SerializeField] private int maxMagSize;
    [SerializeField] private int currentMag = 0;
    [SerializeField] private float reloadTime=1;

    private bool overheated = false;
    private bool canShoot = true;
    private bool reloading = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentMag = maxMagSize;
    }

    // Update is called once per frame
    void Update()
    {
        HandlingShooting();
    }

    private void HandlingShooting()
    {
        // Shooting
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

                if(currentMag <= 0)
                {
                    reloading = true;
                    Invoke(nameof(Reload), reloadTime);
                }
            }

            Invoke(nameof(CanShot), firingrate);
        }

        // Cooling
        if (!Input.GetMouseButton(0) && currentHeat > 0)
        {
            currentHeat -= coolingRate * Time.deltaTime;
            currentHeat = Mathf.Clamp(currentHeat, 0, MaxHeat);
        }

        // Recover from overheat
        if (overheated && currentHeat <= 0)
        {
            overheated = false;
            canShoot = true;
        }
    }
    private void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, firingPoint.position, Quaternion.identity);
        Vector3 directionOffset = new Vector3(Random.Range(-spread, spread), Random.Range(-spread, spread), 0);
        bullet.GetComponent<Bullet>().direction = transform.right + directionOffset;
        bullet.GetComponent<Bullet>().playerTransform = transform;
        bullet.gameObject.transform.right = transform.up;
    }
    private void CanShot()
    {
        canShoot = true;
    }
    private void Reload( )
    {
        reloading = false;
        currentMag = maxMagSize;
    }
}
