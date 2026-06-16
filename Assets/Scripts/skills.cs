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
    public ModifierType modifierType = ModifierType.Flat;
    public float value = 0f;
}

public class Skills : MonoBehaviour
{
    [Header("Skill Level")]
    public bool isUnlock = false;
    public int level = 0;
    public int maxLevel = 1;
    public int costPerlevel = 1;

    [Header("Requirements")]
    public Skills[] requireSkills;

    [Header("Effects")]
    public SkillEffect[] skillEffects;
    public PlayerStats playerStats;

    [Header("Button Colors")]
    public Color lockColor = Color.gray;
    public Color unlockColor = Color.green;
    public Color maxColor = Color.yellow;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Update()
    {
        isUnlock = CheckUnlock();
        UpdateButtonColor();
    }

    public bool CheckUnlock()
    {
        if (requireSkills == null || requireSkills.Length == 0)
        {
            return true;
        }

        foreach (Skills skill in requireSkills)
        {
            if (skill == null)
            {
                return false;
            }

            if (skill.level <= 0)
            {
                return false;
            }
        }

        return true;
    }

    private void UpdateButtonColor()
    {
        if (button == null)
        {
            return;
        }

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
            Debug.Log("Not unlock yet");
            return;
        }

        if (level >= maxLevel)
        {
            Debug.Log("Max level");
            return;
        }

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("CurrencyManager.Instance is null");
            return;
        }

        if (CurrencyManager.Instance.SpendMoney(costPerlevel))
        {
            level++;
            ApplyEffects();

            Debug.Log("Skill bought. Current level: " + level);
        }
        else
        {
            Debug.Log("Not enough money");
        }
    }

    private void ApplyEffects()
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats is not assigned on " + gameObject.name);
            return;
        }

        foreach (SkillEffect effect in skillEffects)
        {
            if (effect == null)
            {
                continue;
            }

            switch (effect.statName)
            {
                case "Damage":
                    ApplyModifier(ref playerStats.damage, effect);
                    break;

                case "MaxHealth":
                    ApplyModifier(ref playerStats.maxHealth, effect);
                    break;

                case "MoveSpeed":
                    ApplyModifier(ref playerStats.moveSpeed, effect);
                    break;

                case "FireRate":
                    ApplyModifier(ref playerStats.fireRate, effect);
                    break;

                default:
                    Debug.LogWarning("Unknown stat name: " + effect.statName);
                    break;
            }
        }
    }

    private void ApplyModifier(ref float stat, SkillEffect effect)
    {
        if (effect.modifierType == ModifierType.Flat)
        {
            stat += effect.value;
        }
        else if (effect.modifierType == ModifierType.Percent)
        {
            stat *= 1f + effect.value;
        }
    }
}