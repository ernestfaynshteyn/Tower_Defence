using UnityEngine;

public class MouseFollow2D : MonoBehaviour
{

    void Update()
    {
        // Get the mouse position in world space
        // We set the Z value to a distance from the camera (e.g., 10f, assuming camera Z is -10)
        // so ScreenToWorldPoint can correctly calculate the 2D world position.
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));

        // Calculate the direction vector from the object to the mouse
        Vector3 direction = mouseWorldPosition - transform.position;

        // Use Mathf.Atan2 to find the angle in degrees
        // Atan2 returns the angle whose tangent is direction.y/direction.x 
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Apply the rotation to the object's Z-axis
        // The angle might need an offset depending on which way your sprite's "forward" is oriented.
        // For sprites facing right by default, this angle works well.
        // If your sprite faces up, you might add '-90f' to the angle.
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        Debug.Log(angle);
    }
}
