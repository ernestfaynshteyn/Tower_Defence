using UnityEngine;
using CodeMonkey.Utils;
using UnityEngine.UIElements;
public class SmoothMouseFollow : MonoBehaviour
{
    private Transform aimTransform;

    
    Vector3 mousePosition;


    private void Awake()
    {
        aimTransform = transform.Find("Aim");
    }

    private void Update()
    {
        HandlingAiming();
    }

    private void HandlingAiming()
    {

        mousePosition = UtilsClass.GetMouseWorldPosition();

        Vector3 aimDirection = (mousePosition - transform.position).normalized;
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        aimTransform.eulerAngles = new Vector3(0, 0, angle);

        transform.eulerAngles = new Vector3(0, 0, angle);
    }

  
}