    

using UnityEngine;
using UnityEngine.UI; // Required for accessing UI components
using TMPro;

public class HealthBar : MonoBehaviour
{
    public Slider slider;

    [SerializeField] private TMP_Text valueLabel;

    /// <summary>The single HUD bar used by the currently equipped weapon.</summary>
    public static HealthBar OverheatBar { get; private set; }

    private void Awake()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        if (valueLabel == null)
            valueLabel = GetComponentInChildren<TMP_Text>(true);

        if (gameObject.name == "overheat Bar")
            OverheatBar = this;

        RefreshLabel();
    }

    private void OnDestroy()
    {
        if (OverheatBar == this)
            OverheatBar = null;
    }

    public void SetMaxHealth(float health)
    {
        if (slider == null)
            return;

        slider.maxValue = health;
        slider.value = Mathf.Clamp(slider.value, slider.minValue, slider.maxValue);
        RefreshLabel();
    }

    public void SetHealth(float health)
    {
        if (slider == null)
            return;

        slider.value = health;
        RefreshLabel();
    }

    public static void SetOverheat(float currentHeat, float maxHeat)
    {
        if (OverheatBar == null)
            return;

        OverheatBar.SetValueAndMax(currentHeat, maxHeat);
    }

    private void SetValueAndMax(float value, float maxValue)
    {
        if (slider == null)
            return;

        slider.maxValue = maxValue;
        slider.value = Mathf.Clamp(value, slider.minValue, slider.maxValue);
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (slider != null && valueLabel != null)
            valueLabel.SetText("{0:0.#}/{1:0.#}", slider.value, slider.maxValue);
    }
}
