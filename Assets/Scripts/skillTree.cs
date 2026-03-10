using System.Collections.Generic;
using UnityEngine;

public class SkillTreeScript : MonoBehaviour
{
    public static SkillTreeScript skillTree;
    private void Awake() => skillTree = this;

    public int[] SkillLevels;
    public int[] SkillCaps;
    public string[] SkillNames;
    public string[] SkillDescription;

    public List<Skills> SkillList = new List<Skills>();
    public GameObject SkillHolder;

    public List<GameObject> ConnectorList = new List<GameObject>();
    public GameObject ConnecterHolder;

    public int SkillPoint;

    private void Start()
    {
        SkillPoint = 20;

        SkillLevels = new int[6];
        SkillCaps = new int[] { 1, 5, 5, 2, 10, 10 };

        SkillNames = new string[]
        {
            "upgrade 1",
            "upgrade 2",
            "upgrade 3",
            "upgrade 4",
            "upgrade 5",
            "upgrade 6"
        };

        SkillDescription = new string[]
        {
            "does a thing",
            "does a cool thing",
            "does a really cool thing",
            "does an awesome thing",
            "does this math thing",
            "does this compound thing"
        };

        foreach (var skill in SkillHolder.GetComponentsInChildren<Skills>(true))
        {
            SkillList.Add(skill);
        }

        foreach (RectTransform connecter in ConnecterHolder.GetComponentsInChildren<RectTransform>())
        {
            ConnectorList.Add(connecter.gameObject);
        }

        for (int i = 0; i < SkillList.Count; i++)
        {
            SkillList[i].id = i;
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