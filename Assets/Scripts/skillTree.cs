using System.Collections.Generic;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

public class SkillTreeScript : MonoBehaviour
{

    public static SkillTreeScript skillTree;
    private void Awake() => skillTree = this;

    public int[] SkillLevels;
    public int[] SkillCaps;
    public string[] SkillNames;
    public string[] SkillDescription;

    public List<Skills> SkillList;
    public GameObject SkillHolder;

    public List<GameObject> ConnectorList;
    public GameObject ConnecterHolder;

    public int SkillPoint;

    private void Start()
    {
        SkillPoint = 20;

        SkillLevels = new int[6];
        SkillCaps = new int[] {1, 5, 5, 2, 10, 10};

        SkillNames = new string[] {"upgrade 1", "upgrade 2", "upgrade 3", "upgrade 4", "upgrade 5", "upgrade 6"};
        SkillDescription = new string[]
        {
            "does a thing",
            "does a cool thing",
            "does a really cool thing",
            "Does an awesome thing",
            "does this math thing",
            "does this compound thing",
        };

        foreach (var skill in SkillHolder.GetComponentsInChildren<Skills>() SkillList.Add(skill);
        foreach (var connecter :RectTransform in SkillHolder.GetComponentsInChildren<RectTransform>() ConnectorList.Add(connecter);

        for (var i = 0; i < SkillList.Count; i++) SkillList[1].id = i;

        UpdateAllSkillUI();
    }
    public void UpdateAllSkillUI()
    {
        foreach (var Skills in SkillList) Skills.UpdateUI();
    }
}
