using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats instance;

    Dictionary<string, float> statModifiers = new Dictionary<string, float>();

    void Awake()
    {
        instance = this;
    }

    public void AddModifier(string statName, float value)
    {
        if (!statModifiers.ContainsKey(statName))
            statModifiers[statName] = 0;

        statModifiers[statName] += value;
    }

    public float GetFinalStat(string statName, float baseValue)
    {
        if (!statModifiers.ContainsKey(statName))
            return baseValue;

        return baseValue + statModifiers[statName];
    }
}