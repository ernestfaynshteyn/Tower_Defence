using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum ModifierType
{
    Flat,
    Percent
}

public enum PurchaseType
{
    SkillPoints,
    Money
}

[System.Serializable]
public class SkillEffect
{
    public string statName;
    public ModifierType modifierType;
    public float value;
}

public class Skills : MonoBehaviour
{
    [Header("Info")]
    public string SkillName;
    public string Description;

    public int Level = 0;
    public int SkillCap = 1;
    public int Cost = 1;
    public PurchaseType PurchaseWith = PurchaseType.SkillPoints;

    [Header("Dependencies")]
    public Skills[] RequiredSkills;

    [Header("Effects")]
    public SkillEffect[] Effects;

    [Header("UI")]
    public TMP_Text TitleText;
    public TMP_Text DescriptionText;

    public Color LockedColor = Color.gray;
    public Color AvailableColor = Color.green;
    public Color MaxedColor = Color.yellow;

    private Image img;
    private Button button;

    void Awake()
    {
        img = GetComponent<Image>();
        button = GetComponent<Button>();
        button.onClick.AddListener(Buy);
    }

    void Start()
    {
        UpdateUI();
    }

    bool RequirementsMet()
    {
        if (RequiredSkills == null || RequiredSkills.Length == 0)
            return true;

        foreach (var skill in RequiredSkills)
        {
            if (skill == null || skill.Level <= 0)
                return false;
        }

        return true;
    }

    bool CanAfford()
    {
        if (PurchaseWith == PurchaseType.Money)
        {
            if (CurrencyManager.Instance == null)
                return false;

            return CurrencyManager.Instance.GetMoney() >= Cost;
        }

        if (SkillTreeScript.skillTree == null)
            return false;

        return SkillTreeScript.skillTree.SkillPoint >= Cost;
    }

    void SpendCost()
    {
        if (PurchaseWith == PurchaseType.Money)
        {
            CurrencyManager.Instance.SpendMoney(Cost);
        }
        else
        {
            SkillTreeScript.skillTree.SkillPoint -= Cost;
        }
    }

    void ApplyEffects()
    {
        if (PlayerStats.instance == null)
        {
            Debug.LogWarning("PlayerStats instance not found.");
            return;
        }

        if (Effects == null)
            return;

        foreach (var effect in Effects)
        {
            if (effect == null || string.IsNullOrEmpty(effect.statName))
                continue;

            if (effect.modifierType == ModifierType.Flat)
                PlayerStats.instance.AddFlatModifier(effect.statName, effect.value);
            else
                PlayerStats.instance.AddPercentModifier(effect.statName, effect.value);
        }
    }

    public void UpdateUI()
    {
        bool reqMet = RequirementsMet();
        bool affordable = CanAfford();

        if (Level >= SkillCap)
        {
            img.color = MaxedColor;
        }
        else if (!reqMet)
        {
            img.color = LockedColor;
        }
        else if (affordable)
        {
            img.color = AvailableColor;
        }
        else
        {
            img.color = Color.white;
        }

        button.interactable = reqMet && affordable && Level < SkillCap;

        if (TitleText != null)
            TitleText.text = SkillName;

        if (DescriptionText != null)
            DescriptionText.text = Description;
    }

    public void Buy()
    {
        if (!RequirementsMet())
            return;

        if (Level >= SkillCap)
            return;

        if (!CanAfford())
            return;

        SpendCost();
        Level++;
        ApplyEffects();

        if (SkillTreeScript.skillTree != null)
            SkillTreeScript.skillTree.UpdateAllSkillUI();
    }
}