using System;
using System.Collections.Generic;
using UnityEngine;


public class Upgrades : MonoBehaviour
{
    public static Upgrades Instance { get; private set; }

    /// <summary>Fired whenever a stat's total may have changed. Hook this up to refresh UI/gameplay.</summary>
    public static event Action<string> OnStatChanged;

    private struct Modifier
    {
        public ModifierType type;
        public float value;
    }

    // statName -> (source -> modifier). "source" is the skills node object, boxed as object
    // so this class doesn't need to know about the skills type.
    private readonly Dictionary<string, Dictionary<object, Modifier>> _modifiers =
        new Dictionary<string, Dictionary<object, Modifier>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[Upgrades] Duplicate instance found - destroying the new one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Sets (or overwrites) the modifier that <paramref name="source"/> contributes to <paramref name="statName"/>.</summary>
    public void SetModifier(string statName, object source, ModifierType type, float value)
    {
        if (string.IsNullOrEmpty(statName) || source == null) return;

        if (!_modifiers.TryGetValue(statName, out Dictionary<object, Modifier> sources))
        {
            sources = new Dictionary<object, Modifier>();
            _modifiers[statName] = sources;
        }

        sources[source] = new Modifier { type = type, value = value };
        OnStatChanged?.Invoke(statName);
    }

    /// <summary>Removes every modifier <paramref name="source"/> contributes, across all stats. Safe to call even if it never contributed anything.</summary>
    public void RemoveSource(object source)
    {
        if (source == null) return;

        List<string> touched = null;
        foreach (KeyValuePair<string, Dictionary<object, Modifier>> kvp in _modifiers)
        {
            if (kvp.Value.Remove(source))
            {
                touched ??= new List<string>();
                touched.Add(kvp.Key);
            }
        }

        if (touched != null)
            foreach (string stat in touched)
                OnStatChanged?.Invoke(stat);
    }

    /// <summary>
    /// Final value of a stat: all flat modifiers add to the base first,
    /// then the result is scaled by (1 + sum of percent modifiers / 100).
    /// e.g. base 10, +5 flat, +50% => (10 + 5) * 1.5 = 22.5
    /// </summary>
    public float GetValue(string statName, float baseValue = 0f)
    {
        if (!_modifiers.TryGetValue(statName, out Dictionary<object, Modifier> sources) || sources.Count == 0)
            return baseValue;

        float flatSum = 0f;
        float percentSum = 0f;

        foreach (Modifier m in sources.Values)
        {
            if (m.type == ModifierType.Flat) flatSum += m.value;
            else percentSum += m.value;
        }

        return (baseValue + flatSum) * (1f + percentSum / 100f);
    }

    /// <summary>Sum of flat modifiers only for a stat (0 if none).</summary>
    public float GetFlat(string statName)
    {
        if (!_modifiers.TryGetValue(statName, out Dictionary<object, Modifier> sources)) return 0f;
        float sum = 0f;
        foreach (Modifier m in sources.Values)
            if (m.type == ModifierType.Flat) sum += m.value;
        return sum;
    }

    /// <summary>Sum of percent modifiers only for a stat, as a raw number (10 means +10%, not 0.1).</summary>
    public float GetPercent(string statName)
    {
        if (!_modifiers.TryGetValue(statName, out Dictionary<object, Modifier> sources)) return 0f;
        float sum = 0f;
        foreach (Modifier m in sources.Values)
            if (m.type == ModifierType.Percent) sum += m.value;
        return sum;
    }
}