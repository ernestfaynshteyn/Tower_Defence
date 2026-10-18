using UnityEngine;
using UnityEngine.UI;

public class GunOverheat : MonoBehaviour
{
    [Header("Heat Settings")]
    public Slider slider;
    public float currentHeat = 0f;
    public float maxHeat = 100f;

    [Tooltip("Heat added per shot")]
    public float heatPerShot = 10f;

    [Tooltip("Extra heat per second while holding fire")]
    public float holdHeatPerSecond = 30f;

    [Tooltip("Heat removed per second when not firing")]
    public float coolingRate = 25f;

    [Header("State")]
    public bool isOverheated = false;
    private bool isHoldingFire = false;

    void Start()
    {
        slider.minValue = 0f;
        slider.maxValue = maxHeat;
        slider.value = currentHeat;
    }

    void Update()
    {
        // Example input (left mouse button)
        isHoldingFire = Input.GetMouseButton(0);

        if (isHoldingFire && !isOverheated)
        {
            currentHeat += holdHeatPerSecond * Time.deltaTime;
        }
        else
        {
            currentHeat -= coolingRate * Time.deltaTime;
        }

        // Clamp heat value
        currentHeat = Mathf.Clamp(currentHeat, 0f, maxHeat);

        if (currentHeat >= maxHeat)
        {
            isOverheated = true;
        }
        else if (currentHeat <= maxHeat * 0.3f) // cooldown threshold
        {
            isOverheated = false;
        }

        slider.value = currentHeat;
    }

    // Call this when a shot is fired
    public void AddShotHeat()
    {
        if (isOverheated) return;

        currentHeat += heatPerShot;
        currentHeat = Mathf.Clamp(currentHeat, 0f, maxHeat);
    }
}
