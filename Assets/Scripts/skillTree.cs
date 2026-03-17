using System.Collections.Generic;
using UnityEngine;

public class SkillTreeScript : MonoBehaviour
{
    public static SkillTreeScript skillTree;

    private void Awake()
    {
        skillTree = this;
    }

    public int SkillPoint = 20;

    public List<Skills> SkillList = new List<Skills>();
    public GameObject SkillHolder;

    void Start()
    {
        SkillList.Clear();

        foreach (var skill in SkillHolder.GetComponentsInChildren<Skills>(true))
        {
            SkillList.Add(skill);
        }

        
        Invoke(nameof(UpdateAllSkillUI), 0.05f);
    }

    public void UpdateAllSkillUI()
    {
        foreach (var skill in SkillList)
        {
            if (skill != null)
                skill.UpdateUI();
        }
    }
}