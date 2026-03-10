using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Skills : MonoBehaviour
{
    [Header("General Info")]
    public int id;
    public int TreeID = 0;          // Which tree this skill belongs to
    public string SkillName;
    public string Description;
    public int SkillCap = 1;
    public int Cost = 1;             // Variable SP cost

    [Header("Requirements")]
    public int[] RequiredSkills;      // IDs of skills required
    public int[] ConnectedSkills;     // IDs of skills this unlocks

    [Header("UI References")]
    public TMP_Text TitleText;
    public TMP_Text DescriptionText;
    public Color DimColor = new Color(0.5f, 0.5f, 0.5f, 1f); // dim for locked

    private Image img;
    private Button button;

    private void Awake()
    {
        img = GetComponent<Image>();
        button = GetComponent<Button>();
    }

    // Check if all required skills are unlocked
    private bool RequirementsMet()
    {
        var tree = SkillTreeScript.skillTree;

        foreach (int req in RequiredSkills)
        {
            if (tree.SkillLevels[req] <= 0)
                return false;
        }
        return true;
    }

    public void UpdateUI()
    {
        var tree = SkillTreeScript.skillTree;

        bool reqMet = RequirementsMet();
        if (tree.SkillPoint[TreeID] >= Cost)
        {

        }
        bool affordable = tree.SkillPoint[TreeID] >= Cost;

        if (tree.SkillLevels[id] >= SkillCap)
            img.color = Color.yellow;          // maxed out
        else if (!reqMet)
            img.color = DimColor;              // requirements not met ? dimmed
        else if (affordable)
            img.color = Color.green;           // available and can buy
        else
            img.color = Color.white;           // affordable but no skill points

        // Button interactable
        if (button != null)
            button.interactable = reqMet && affordable && tree.SkillLevels[id] < SkillCap;

        // Show connected skills only if this skill bought
        foreach (int connected in ConnectedSkills)
        {
            tree.SkillList[connected].gameObject.SetActive(tree.SkillLevels[id] > 0);
            if (tree.ConnectorList.Count > connected)
                tree.ConnectorList[connected].SetActive(tree.SkillLevels[id] > 0);
        }
    }

    public void Buy()
    {
        var tree = SkillTreeScript.skillTree;

        Debug.Log($"Trying to buy skill {id}, level {tree.SkillLevels[id]}, cost {Cost}, SP available {tree.SkillPoints[TreeID]}");

        if (tree.SkillLevels[id] >= SkillCap || tree.SkillPoints[TreeID] < Cost || !RequirementsMet())
        {
            Debug.Log("Cannot buy: maxed, not enough SP, or requirements not met");
            return;
        }

        tree.SkillPoints[TreeID] -= Cost;
        tree.SkillLevels[id]++;
        Debug.Log($"Bought skill {id}. New level: {tree.SkillLevels[id]}");

        tree.UpdateAllSkillUI();
    }
}