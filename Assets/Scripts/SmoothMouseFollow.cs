using UnityEngine;
using CodeMonkey.Utils;
using UnityEngine.UIElements;
public class SmoothMouseFollow : MonoBehaviour
{
    private Transform aimTransform;

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firingPoint;
    [Range(0.01f, 3f)]
    [SerializeField] private float firingrate = 0.1f;
    [Range(0f, 1f)]
    [SerializeField] private float spread = 0.5f;
    [Range(1f, 12f)]
    [SerializeField] private int bulletPerShot = 1;

    private bool canShoot = true;
    Vector3 mousePosition;


    private void Awake()
    {
        aimTransform = transform.Find("Aim");
    }

    private void Update()
    {
        HandlingAiming();
        HandlingShooting();
    }

    private void HandlingAiming()
    {

        mousePosition = UtilsClass.GetMouseWorldPosition();

        Vector3 aimDirection = (mousePosition - transform.position).normalized;
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        aimTransform.eulerAngles = new Vector3(0, 0, angle);

        transform.eulerAngles = new Vector3(0, 0, angle);
    }

    private void HandlingShooting()
    {
        if(Input.GetMouseButton(0) && canShoot)
        {
            canShoot = false;
            for(int i=0; i<bulletPerShot; i++)
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
        Vector3 directionOffset = new Vector3(Random.Range(-spread, spread), Random.Range(-spread, spread),0);
        bullet.GetComponent<Bullet>().direction = transform.right + directionOffset;
        bullet.gameObject.transform.right = transform.up;
    }
    private void Reload()
    {
        canShoot = true;
    }
}