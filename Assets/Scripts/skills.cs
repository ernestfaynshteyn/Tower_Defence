using UnityEngine;
using UnityEngine.UI;

public enum ModifierType
{
    Flat,
    Percent
}

[System.Serializable]
public class SkillEffect
{
    public string statName;
    public ModifierType modifierType = ModifierType.Flat;
    public float value = 0f;
}

public class skills : MonoBehaviour
{
    public bool isUnlock = false;

    public int level = 0;
    public int maxLevel = 1;
    public int costPerLevel = 10;

    public skills[] requireSkills;

    public SkillEffect[] effects;

    public Color lockColor = Color.gray;
    public Color unlockColor = Color.green;
    public Color maxColor = Color.yellow;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError("Skills script requires a Button component.");
        }
    }

    private void Update()
    {
        isUnlock = CheckUnlock();
        UpdateButtonColor();
    }

    public bool CheckUnlock()
    {
        if (requireSkills == null || requireSkills.Length == 0)
            return true;

        foreach (skills skill in requireSkills)
        {
            if (skill == null)
                return false;

            if (skill.level <= 0)
                return false;
        }

        return true;
    }

    private void UpdateButtonColor()
    {
        if (button == null) return;

        ColorBlock colors = button.colors;

        if (!isUnlock)
        {
            colors.normalColor = lockColor;
        }
        else if (level >= maxLevel)
        {
            colors.normalColor = maxColor;
        }
        else
        {
            colors.normalColor = unlockColor;
        }

        button.colors = colors;
    }

    public void Buy()
    {
        if (!isUnlock)
        {
            Debug.Log("Skill not unlocked yet");
            return;
        }

        if (level >= maxLevel)
        {
            Debug.Log("Skill already max level");
            return;
        }

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("CurrencyManager.Instance is null");
            return;
        }

        if (CurrencyManager.Instance.SpendMoney(costPerLevel))
        {
            level++;

            Debug.Log("Skill bought. Level: " + level);

            ApplyEffects();
        }
        else
        {
            Debug.Log("Not enough money");
        }
    }

    private void ApplyEffects()
    {
        if (effects == null) return;

        foreach (SkillEffect effect in effects)
        {
            Debug.Log($"Applying {effect.statName}: {effect.value} ({effect.modifierType})");
            // Hook into your stat system here
        }
    }
}