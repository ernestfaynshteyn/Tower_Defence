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
    public int maxLevel = 3;

    public int costPerlevel;

    public Skills[] requireSkills;

    public Color lockColor = Color.gray;
    public Color unlockColor = Color.green;
    public Color maxColor = Color.yellow;

    Button button;

    private void Awake()
    {
        button = gameObject.GetComponent<Button>();
    }

    private void FixedUpdate()
    {
        isUnlock = CheckUnlock();

        ColorBlock colors = button.colors;

        if (isUnlock)
        {
            if (maxLevel >= level) {
                colors.normalColor = maxColor;
            }
            else
            {
                colors.normalColor = unlockColor;
            }
        }
        else
        {
            colors.normalColor = lockColor;
        }

        button.colors = colors;
    }

    public bool CheckUnlock()
    {
        foreach (Skills skill in requireSkills)
        {
            if (skill.isUnlock == false)
            {
                return false;
            }
        }
        return true;
    }

    public void Buy()
    {
        if(CurrencyManager.Instance.money >= costPerlevel)
        {

        }

    }
}