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

    private bool canShoot = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        HandlingShooting();
    }

    private void HandlingShooting()
    {
        if (Input.GetMouseButton(0) && canShoot)
        {
            canShoot = false;
            for (int i = 0; i < bulletPerShot; i++)
            {
                Shoot();
            }
            Debug.Log("Pew Pew");
            Invoke("Reload", firingrate);
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
    private void Reload()
    {
        canShoot = true;
    }
}
