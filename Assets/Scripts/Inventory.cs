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
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        RefreshUI();
    }

    public void AddGrenade(GrenadeType type)
    {
        grenades.Add(type);
        Debug.Log(type + " added to inventory");
        RefreshUI();
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
            RefreshUI();
            return true;
        }

        Debug.Log("You do not have " + type);
        return false;
    }

    private void RefreshUI()
    {
        int frag = 0;
        int smoke = 0;
        int flash = 0;
        int molotov = 0;

        foreach (GrenadeType grenade in grenades)
        {
            switch (grenade)
            {
                case GrenadeType.Frag: frag++; break;
                case GrenadeType.Smoke: smoke++; break;
                case GrenadeType.Flash: flash++; break;
                case GrenadeType.Molotov: molotov++; break;
            }
        }

        if (numberOfFrag != null) numberOfFrag.text = frag.ToString();
        if (numberOfSmoke != null) numberOfSmoke.text = smoke.ToString();
        if (numberOfFlash != null) numberOfFlash.text = flash.ToString();
        if (numberOfMolotov != null) numberOfMolotov.text = molotov.ToString();
    }
}
