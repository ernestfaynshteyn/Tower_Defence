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
        foreach (var skill in SkillHolder.GetComponentsInChildren<Skills>(true))
        {
            SkillList.Add(skill);
        }

        UpdateAllSkillUI();
    }

    public void UpdateAllSkillUI()
    {
        foreach (var skill in SkillList)
        {
            skill.UpdateUI();
        }
    }
}