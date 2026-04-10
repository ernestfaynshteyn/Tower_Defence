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
        SkillName = gameObject.name;
        img = GetComponent<Image>();
        button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError("No Button component on " + gameObject.name);
            return;
        }

        button.onClick.RemoveAllListeners();
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
            if (skill == null)
            {
                Debug.LogError(SkillName + " has a NULL entry in RequiredSkills!");
                return false;
            }

            if (skill.Level <= 0)
            {
                Debug.Log(SkillName + " locked by: " + skill.SkillName);
                return false;
            }
        }

        return true;
    }

    bool CanAfford()
    {
        if (PurchaseWith == PurchaseType.Money)
        {
            if (CurrencyManager.Instance == null)
            {
                Debug.LogError("CurrencyManager.Instance is null!");
                return false;
            }
            return CurrencyManager.Instance.GetMoney() >= Cost;
        }

        if (SkillTreeScript.skillTree == null)
        {
            Debug.LogError("SkillTreeScript.skillTree is null!");
            return false;
        }

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

        if (Effects == null || Effects.Length == 0)
        {
            Debug.LogWarning(SkillName + " has no Effects assigned.");
            return;
        }

        foreach (var effect in Effects)
        {
            if (effect == null || string.IsNullOrEmpty(effect.statName))
                continue;

            if (effect.modifierType == ModifierType.Flat)
            {
                PlayerStats.instance.AddFlatModifier(effect.statName, effect.value);
                Debug.Log("Applied FLAT " + effect.statName + " = " + effect.value);
            }
            else
            {
                PlayerStats.instance.AddPercentModifier(effect.statName, effect.value);
                Debug.Log("Applied PERCENT " + effect.statName + " = " + effect.value);
            }
        }
    }

    public void ResetEffects()
    {
        if (PlayerStats.instance == null) return;

        if (Effects == null || Effects.Length == 0) return;

        foreach (var effect in Effects)
        {
            if (effect == null || string.IsNullOrEmpty(effect.statName))
                continue;

            if (effect.modifierType == ModifierType.Flat)
                PlayerStats.instance.AddFlatModifier(effect.statName, -effect.value);
            else
                PlayerStats.instance.AddPercentModifier(effect.statName, -effect.value);
        }
    }

    public void UpdateUI()
    {
        if (img == null || button == null)
            return;

        bool reqMet = RequirementsMet();
        bool affordable = CanAfford();

        if (Level >= SkillCap)
        {
            img.color = MaxedColor;
            button.interactable = false;
        }
        else if (!reqMet)
        {
            img.color = LockedColor;
            button.interactable = false;
        }
        else if (affordable)
        {
            img.color = AvailableColor;
            button.interactable = true;
        }
        else
        {
            img.color = Color.white;
            button.interactable = false;
        }

        if (TitleText != null)
            TitleText.text = SkillName;

        if (DescriptionText != null)
            DescriptionText.text = Description;
    }

    public void Buy()
    {
        Debug.Log("Clicked buy on " + SkillName);


        if (Level >= SkillCap)
        {
            Debug.Log(SkillName + ": Buy failed: already maxed, level: " + Level + ", " + SkillCap);
            return;
        }

        else if (!CanAfford())
        {
            Debug.Log(SkillName + ": Buy failed: cannot afford");
            return;
        }
        else if (!RequirementsMet())
        {
            Debug.Log(SkillName + ": Buy failed: requirements not met");
            return;
        }
        else
        {
            SpendCost();
            //Level++;
            Debug.Log("Bought " + SkillName + ". New level: " + Level);

            ApplyEffects();

            if (SkillTreeScript.skillTree != null)
                SkillTreeScript.skillTree.UpdateAllSkillUI();
        }
    }
}