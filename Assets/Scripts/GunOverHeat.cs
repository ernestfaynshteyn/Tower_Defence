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

    void Update()
    {
        slider.value = currentHeat;
    }
}
