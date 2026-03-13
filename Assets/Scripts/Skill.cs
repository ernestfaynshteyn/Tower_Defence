using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Skills : MonoBehaviour
{
    [Header("Info")]
    public string SkillName;
    public string Description;

    public int Level = 0;
    public int SkillCap = 1;
    public int Cost = 1;

    [Header("Dependencies")]
    public Skills[] RequiredSkills;

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
    }

    bool RequirementsMet()
    {
        if (RequiredSkills == null || RequiredSkills.Length == 0)
            return true;

        foreach (var skill in RequiredSkills)
        {
            if (skill.Level <= 0)
                return false;
        }

        return true;
    }

    public void UpdateUI()
    {
        var tree = SkillTreeScript.skillTree;

        bool reqMet = RequirementsMet();
        bool affordable = tree.SkillPoint >= Cost;

        if (Level >= SkillCap)
            img.color = MaxedColor;
        else if (!reqMet)
            img.color = LockedColor;
        else if (affordable)
            img.color = AvailableColor;
        else
            img.color = Color.white;

        button.interactable = reqMet && affordable && Level < SkillCap;
    }

    public void Buy()
    {
        var tree = SkillTreeScript.skillTree;

        if (!RequirementsMet()) return;
        if (Level >= SkillCap) return;
        if (tree.SkillPoint < Cost) return;

        tree.SkillPoint -= Cost;
        Level++;

        tree.UpdateAllSkillUI();
    }
}