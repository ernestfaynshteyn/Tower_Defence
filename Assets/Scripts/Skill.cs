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

        // Auto-hook button click (so you don’t rely on inspector)
        button.onClick.AddListener(Buy);
    }

    void Start()
    {
        UpdateUI(); // ensure correct state at start
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

        if (tree == null)
        {
            Debug.LogError("SkillTreeScript not found!");
            return;
        }

        bool reqMet = RequirementsMet();
        bool affordable = tree.SkillPoint >= Cost;

        if (Level >= SkillCap)
        {
            img.color = MaxedColor;
        }
        else if (!reqMet)
        {
            Color c = LockedColor;
            c.a = LockedColor.a;
            img.color = c;
        }
        else if (affordable)
        {
            img.color = AvailableColor;
        }
        else
        {
            img.color = Color.white;
        }
        Debug.Log("reqMet" + reqMet);
        Debug.Log("affordable" + affordable);
        Debug.Log("Level < SkillCap"+ (Level < SkillCap));
        button.interactable = reqMet && affordable && Level < SkillCap;

        // Optional UI text updates
        if (TitleText != null)
            TitleText.text = SkillName;

        if (DescriptionText != null)
            DescriptionText.text = Description;

        // Debug
        Debug.Log($"{SkillName} | Interactable: {button.interactable} | Level: {Level}");
    }

    public void Buy()
    {
        var tree = SkillTreeScript.skillTree;

        if (tree == null)
        {
            Debug.LogError("SkillTreeScript missing!");
            return;
        }

        Debug.Log($"Trying to buy {SkillName}");

        if (!RequirementsMet()) { Debug.Log("Requirements not met"); return; }
        if (Level >= SkillCap) { Debug.Log("Already maxed"); return; }
        if (tree.SkillPoint < Cost) { Debug.Log("Not enough points"); return; }

        tree.SkillPoint -= Cost;
        Level++;

        Debug.Log($"Bought {SkillName}, new level: {Level}");

        tree.UpdateAllSkillUI();
    }
}