using UnityEngine;

public class SkillTree : MonoBehaviour
{

    public static SkillTree skillTree;
    private void Awake() => skillTree = this;

    public int[] skillLevels;
    public int[] skillCap;
    public string[] skillNames;
    public string[] skillDescriptions;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
