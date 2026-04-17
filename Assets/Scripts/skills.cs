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
    public bool isUnlock = false;
    public int level = 0;
    public int maxLevel = 1;
    public int costPerlevel;

    public Skills[] requireSkills;

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
            Debug.Log("max level");
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
            Debug.Log("Skill bought. Current level: " + level);
        }
        else
        {
            Debug.Log("Not enough money");
        }
    }
}