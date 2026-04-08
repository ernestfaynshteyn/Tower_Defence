using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats instance;

    private Dictionary<string, float> flatModifiers = new Dictionary<string, float>();
    private Dictionary<string, float> percentModifiers = new Dictionary<string, float>();

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void AddFlatModifier(string statName, float value)
    {
        if (!flatModifiers.ContainsKey(statName))
            flatModifiers[statName] = 0f;

        flatModifiers[statName] += value;
    }

    public void AddPercentModifier(string statName, float value)
    {
        if (!percentModifiers.ContainsKey(statName))
            percentModifiers[statName] = 0f;

        percentModifiers[statName] += value;
    }

    public float GetFlatModifier(string statName)
    {
        if (!flatModifiers.ContainsKey(statName))
            return 0f;

        return flatModifiers[statName];
    }

    public float GetPercentModifier(string statName)
    {
        if (!percentModifiers.ContainsKey(statName))
            return 0f;

        return percentModifiers[statName];
    }

    public float GetModifiedValue(string statName, float baseValue)
    {
        float flat = GetFlatModifier(statName);
        float percent = GetPercentModifier(statName);
        return (baseValue + flat) * (1f + percent);
    }
}