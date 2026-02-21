using UnityEngine;

public class ZoomInOut : MonoBehaviour
{
    public RectTransform target;
    public float zoomSpeed = 1f;
    public float minZoom = 0.5f;
    public float maxZoom = 3f;

    private float currentZoom = 1f;

    void Update()
    {
        float scroll = Input.mouseScrollDelta.y;

        if (scroll != 0)
        {
            currentZoom += scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

            target.localScale = new Vector3(currentZoom, currentZoom, 1f);
        }
    }
}