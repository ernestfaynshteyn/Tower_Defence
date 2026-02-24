using TMPro;
using UnityEngine;
using static SkillTree;
public class Skill : MonoBehaviour
{
    public int id;

    public TMP_Text TitleText;
    public TMP_Text DescriptionText;

    public int[] Connectedskills;

    public void UpdateUI()
    {
        TitleText.text = $"{skillTree.skillLevels[id]}/{skillTree.SkillCaps[id]}\n{skillTree.skillNames[id]}";
        DescriptionText.text = $"{skillTree.SkillDescription[id]}\nCost:{skillTree.SkillPoint}/1 SP";

        GetComponent<Image>().color = skillTree.SkillLevels[id] >= skillTree.SkillCaps[id] ? Color.yellow
            : skillTree.SkillPoint > 0 ? Color.green : Color.white;
    }

    public void Buy()
    {

    }
}
