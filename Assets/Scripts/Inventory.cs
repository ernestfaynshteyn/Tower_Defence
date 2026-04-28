using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class Inventory : MonoBehaviour
{
    public static Inventory instance;

    public List<GrenadeType> grenades = new List<GrenadeType>();

    public TextMeshProUGUI numberOfFrag;
    public TextMeshProUGUI numberOfSmoke;
    public TextMeshProUGUI numberOfFlash;
    public TextMeshProUGUI numberOfMolotov;
    private void Awake()
    {
        instance = this;
    }

    public void AddGrenade(GrenadeType type)
    {
        grenades.Add(type);
        Debug.Log(type + " added to inventory");
        int newNumberOfFrag = 0;
        int newNumberOfSmoke = 0;
        int newNumberOfFlash = 0;
        int newNumberOfMolotov = 0;
        for (int i = 0; i < grenades.Count;i++)
        {
            Debug.Log("grenades[i]"+ grenades[i]);
            if (grenades[i] == GrenadeType.Frag)
            {
                newNumberOfFrag = newNumberOfFrag + 1;
            }else if(grenades[i] == GrenadeType.Smoke)
            {
                newNumberOfSmoke = newNumberOfSmoke + 1;
            }
            else if (grenades[i] == GrenadeType.Flash)
            {
                newNumberOfFlash = newNumberOfFlash + 1;
            }
            else if (grenades[i] == GrenadeType.Molotov)
            {
                newNumberOfMolotov = newNumberOfMolotov + 1;
            }
        }
        numberOfFrag.text = "newNumberOfFrag";
        numberOfSmoke.text = "newNumberOfSmoke" ;
        numberOfFlash.text = "newNumberOfFlash";
        numberOfMolotov.text = "newNumberOfMolotov" ;
    }

    public bool HasGrenade(GrenadeType type)
    {
        return grenades.Contains(type);
    }

    public bool UseGrenade(GrenadeType type)
    {
        if (grenades.Contains(type))
        {
            grenades.Remove(type);
            return true;
        }

        Debug.Log("You do not have " + type);
        return false;
    }
}   