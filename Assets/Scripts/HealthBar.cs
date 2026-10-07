using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
public class HealthBar : MonoBehaviour
{
    public Slider slider;

    [Header("Value Label")]
    [SerializeField] private TMP_Text valueLabel;
    [SerializeField] private Vector2 labelOffset = new Vector2(12f, 0f);
    [SerializeField] private Vector2 labelSize = new Vector2(120f, 32f);
    [Min(1f)] [SerializeField] private float labelFontSize = 18f;
    [SerializeField] private Color labelColor = Color.white;

    private float displayedValue = float.NaN;
    private float displayedMaxValue = float.NaN;

    /// <summary>The single HUD bar used by the currently equipped weapon.</summary>
    public static HealthBar OverheatBar { get; private set; }

    private void Awake()
    {
        FindReferences();
        ApplyLabelAppearanceAndLayout();

        if (gameObject.name == "overheat Bar")
            OverheatBar = this;

        RefreshLabel();
    }

    private void OnEnable()
    {
        FindReferences();
        ApplyLabelAppearanceAndLayout();
        RefreshLabel();
    }

    private void OnValidate()
    {
        labelFontSize = Mathf.Max(1f, labelFontSize);
        labelSize.x = Mathf.Max(1f, labelSize.x);
        labelSize.y = Mathf.Max(1f, labelSize.y);

        FindReferences();
        ApplyLabelAppearanceAndLayout();
        RefreshLabel();
    }

    private void LateUpdate()
    {
        if (slider == null || valueLabel == null)
            return;

        if (!Mathf.Approximately(displayedValue, slider.value) ||
            !Mathf.Approximately(displayedMaxValue, slider.maxValue))
        {
            RefreshLabel();
        }
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
        if (slider == null || valueLabel == null)
            return;

        displayedValue = slider.value;
        displayedMaxValue = slider.maxValue;
        valueLabel.SetText("{0:0.#}/{1:0.#}", displayedValue, displayedMaxValue);
    }

    private void FindReferences()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        if (valueLabel == null)
        {
            Transform labelTransform = transform.Find("ValueLabel");
            if (labelTransform != null)
                valueLabel = labelTransform.GetComponent<TMP_Text>();
        }
    }

    private void ApplyLabelAppearanceAndLayout()
    {
        if (slider == null || valueLabel == null)
            return;

        RectTransform labelRect = valueLabel.rectTransform;
        if (labelRect.parent == slider.transform)
        {
            labelRect.anchorMin = new Vector2(1f, 0.5f);
            labelRect.anchorMax = new Vector2(1f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);

            float scaleX = Mathf.Max(0.0001f, Mathf.Abs(slider.transform.localScale.x));
            float scaleY = Mathf.Max(0.0001f, Mathf.Abs(slider.transform.localScale.y));
            labelRect.anchoredPosition = new Vector2(labelOffset.x / scaleX, labelOffset.y / scaleY);
            labelRect.sizeDelta = labelSize;
            labelRect.localScale = new Vector3(1f / scaleX, 1f / scaleY, 1f);
        }

        valueLabel.alignment = TextAlignmentOptions.MidlineLeft;
        valueLabel.fontSize = labelFontSize;
        valueLabel.color = labelColor;
        valueLabel.raycastTarget = false;
        valueLabel.overflowMode = TextOverflowModes.Overflow;
    }
}
