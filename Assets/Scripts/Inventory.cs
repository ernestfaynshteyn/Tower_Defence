using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory instance;

    public List<GrenadeType> grenades = new List<GrenadeType>();

    private void Awake()
    {
        instance = this;
    }

    public void AddGrenade(GrenadeType type)
    {
        grenades.Add(type);
        Debug.Log(type + " added to inventory");
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