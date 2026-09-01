using System;
using System.Collections.Generic;
using UnityEngine;

// -----------------------------------------------------------------------------
// Percent modifiers now use the Upgrades convention, where 10 means +10%
// (the old file treated 0.1 as +10%). Existing AddPercentModifier() call sites
// must be multiplied by 100. SkillEffect values already use this convention.
// -----------------------------------------------------------------------------

/// <summary>
/// Authored base value for one stat, before any skill-tree modifiers.
/// </summary>
[Serializable]
public class StatDefinition
{
    [Tooltip("Must match the statName used by SkillEffect, e.g. \"Damage\".")]
    public string statName = "";

    [Tooltip("The value with zero skill levels bought.")]
    public float baseValue = 0f;

    [Tooltip("Floor the final value. Useful for cooldowns that must never reach 0.")]
    public bool clampMin = false;
    public float minValue = 0f;

    [Tooltip("Ceiling the final value. Useful for crit chance / dodge caps.")]
    public bool clampMax = false;
    public float maxValue = 100f;
}

/// <summary>
/// Holds the base values that skill-tree modifiers are applied to, and caches
/// the resulting final values so gameplay can read a stat without recomputing.
///
/// This does NOT store modifiers - Upgrades already owns those, keyed by source
/// node, which is what makes refunds work. This layer adds the two things
/// Upgrades has no opinion about: where the base value comes from, and what the
/// stat was before it changed.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    // Kept lowercase to match the previous file so existing call sites compile.
    public static PlayerStats instance;

    /// <summary>Raised once per frame per stat whose final value actually moved. (statName, oldValue, newValue)</summary>
    public static event Action<string, float, float> OnStatChanged;

    [Header("Base values")]
    [Tooltip("One entry per stat. A stat queried but not listed here is treated as base 0.")]
    public List<StatDefinition> statDefinitions = new List<StatDefinition>();

    [Header("Behaviour")]
    [Tooltip("Values smaller than this are not treated as a change. Stops float noise from spamming the event.")]
    public float changeEpsilon = 0.0001f;

    [Tooltip("Logs every broadcast change. Turn off for builds.")]
    public bool logChanges = false;

    // statName -> definition
    private readonly Dictionary<string, StatDefinition> _defs =
        new Dictionary<string, StatDefinition>();

    // statName -> last broadcast final value. This is the stored stat.
    private readonly Dictionary<string, float> _values =
        new Dictionary<string, float>();

    private readonly HashSet<string> _dirty = new HashSet<string>();
    private readonly List<string> _flushBuffer = new List<string>();

    // Running totals for modifiers added by code rather than by a skill node.
    // Pushed into Upgrades under a single source token so refunds still work.
    private readonly Dictionary<string, float> _externalFlat = new Dictionary<string, float>();
    private readonly Dictionary<string, float> _externalPercent = new Dictionary<string, float>();

    private bool _seeded;
    private bool _sawUpgrades;

    // ----------------------------------------------------------------- lifecycle

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // Destroy the component, not the GameObject - the old file killed the
            // whole object, which takes any sibling components with it.
            Debug.LogWarning("[PlayerStats] Duplicate instance found - destroying the new component.");
            Destroy(this);
            return;
        }
        instance = this;

        RebuildDefinitions();
    }

    private void OnEnable()
    {
        Upgrades.OnStatChanged += HandleUpgradeChanged;
    }

    private void OnDisable()
    {
        Upgrades.OnStatChanged -= HandleUpgradeChanged;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (Upgrades.Instance != null) Upgrades.Instance.RemoveSource(this);
    }

    private void Start()
    {
        SeedAll();
    }

    private void LateUpdate()
    {
        // Upgrades may spawn after us. When it appears, everything needs recomputing
        // because none of the skill nodes could push their modifiers before now.
        bool haveUpgrades = Upgrades.Instance != null;
        if (haveUpgrades && !_sawUpgrades)
        {
            _sawUpgrades = true;
            MarkAllDirty();
        }
        else if (!haveUpgrades && _sawUpgrades)
        {
            _sawUpgrades = false;
            MarkAllDirty();
        }

        Flush();
    }

    // ---------------------------------------------------------------- definitions

    /// <summary>Call after editing statDefinitions at runtime.</summary>
    public void RebuildDefinitions()
    {
        _defs.Clear();
        if (statDefinitions == null) return;

        foreach (StatDefinition d in statDefinitions)
        {
            if (d == null || string.IsNullOrEmpty(d.statName)) continue;
            if (_defs.ContainsKey(d.statName))
            {
                Debug.LogWarning($"[PlayerStats] duplicate statName '{d.statName}' - the first entry wins.");
                continue;
            }
            _defs[d.statName] = d;
        }
    }

    public float GetBaseValue(string statName)
    {
        return _defs.TryGetValue(statName, out StatDefinition d) ? d.baseValue : 0f;
    }

    /// <summary>Changes the pre-modifier value, e.g. on a weapon swap. Fires the change event next frame.</summary>
    public void SetBaseValue(string statName, float newBase)
    {
        if (string.IsNullOrEmpty(statName)) return;

        if (!_defs.TryGetValue(statName, out StatDefinition d))
        {
            d = new StatDefinition { statName = statName };
            statDefinitions.Add(d);
            _defs[statName] = d;
        }

        if (Mathf.Approximately(d.baseValue, newBase)) return;
        d.baseValue = newBase;
        MarkDirty(statName);
    }

    // --------------------------------------------------------------------- reads

    /// <summary>
    /// The current final value. Reads the cache when clean, recomputes when a change
    /// is pending, so this is always current even mid-frame.
    /// </summary>
    public float Get(string statName)
    {
        if (string.IsNullOrEmpty(statName)) return 0f;

        if (!_dirty.Contains(statName) && _values.TryGetValue(statName, out float cached))
            return cached;

        return Compute(statName);
    }

    /// <summary>The value as of the last broadcast. Use inside an OnStatChanged handler to see what it was.</summary>
    public float GetCached(string statName)
    {
        return _values.TryGetValue(statName, out float v) ? v : 0f;
    }

    public bool HasStat(string statName)
    {
        return !string.IsNullOrEmpty(statName) && _defs.ContainsKey(statName);
    }

    private float Compute(string statName)
    {
        _defs.TryGetValue(statName, out StatDefinition d);
        float baseValue = d != null ? d.baseValue : 0f;

        float v = Upgrades.Instance != null
            ? Upgrades.Instance.GetValue(statName, baseValue)
            : baseValue;

        if (d != null)
        {
            if (d.clampMin) v = Mathf.Max(d.minValue, v);
            if (d.clampMax) v = Mathf.Min(d.maxValue, v);
        }
        return v;
    }

    // ------------------------------------------------------------- invalidation

    private void HandleUpgradeChanged(string statName)
    {
        MarkDirty(statName);
    }

    public void MarkDirty(string statName)
    {
        if (!string.IsNullOrEmpty(statName)) _dirty.Add(statName);
    }

    public void MarkAllDirty()
    {
        foreach (string key in _defs.Keys) _dirty.Add(key);
        foreach (string key in _values.Keys) _dirty.Add(key);
    }

    /// <summary>Seeds the cache without firing events. Called on Start.</summary>
    public void SeedAll()
    {
        foreach (string key in _defs.Keys) _values[key] = Compute(key);
        _dirty.Clear();
        _seeded = true;
        _sawUpgrades = Upgrades.Instance != null;
    }

    /// <summary>Recomputes everything now and fires events for anything that moved.</summary>
    public void RecomputeAll()
    {
        MarkAllDirty();
        Flush();
    }

    private void Flush()
    {
        if (_dirty.Count == 0) return;

        _flushBuffer.Clear();
        foreach (string stat in _dirty) _flushBuffer.Add(stat);
        _dirty.Clear();

        for (int i = 0; i < _flushBuffer.Count; i++)
        {
            string stat = _flushBuffer[i];
            float newValue = Compute(stat);

            bool had = _values.TryGetValue(stat, out float oldValue);
            _values[stat] = newValue;

            // Suppress the first-ever value so listeners only ever see real deltas.
            if (!had && !_seeded) continue;
            if (had && Mathf.Abs(newValue - oldValue) <= changeEpsilon) continue;

            if (logChanges)
                Debug.Log($"[PlayerStats] {stat}: {(had ? oldValue : 0f):0.###} -> {newValue:0.###}");

            OnStatChanged?.Invoke(stat, had ? oldValue : 0f, newValue);
        }
    }

    // ------------------------------------------------------- external modifiers

    /// <summary>
    /// Flat modifier from something other than a skill node (buff, pickup, difficulty).
    /// Accumulates, so passing -value later removes it.
    /// </summary>
    public void AddFlatModifier(string statName, float value)
    {
        if (string.IsNullOrEmpty(statName)) return;

        _externalFlat.TryGetValue(statName, out float total);
        total += value;
        _externalFlat[statName] = total;
        PushExternal(statName);
    }

    /// <summary>Percent modifier from code. 10 means +10%.</summary>
    public void AddPercentModifier(string statName, float value)
    {
        if (string.IsNullOrEmpty(statName)) return;

        _externalPercent.TryGetValue(statName, out float total);
        total += value;
        _externalPercent[statName] = total;
        PushExternal(statName);
    }

    public void ClearExternalModifiers(string statName)
    {
        if (string.IsNullOrEmpty(statName)) return;
        _externalFlat.Remove(statName);
        _externalPercent.Remove(statName);
        PushExternal(statName);
    }

    private void PushExternal(string statName)
    {
        if (Upgrades.Instance == null)
        {
            Debug.LogWarning($"[PlayerStats] no Upgrades instance - '{statName}' external modifier not applied.");
            return;
        }

        _externalFlat.TryGetValue(statName, out float flat);
        _externalPercent.TryGetValue(statName, out float percent);

        // Upgrades keys one modifier per (stat, source), so flat and percent from
        // this component need separate source tokens to coexist on the same stat.
        Upgrades.Instance.SetModifier(statName, FlatSourceFor(statName), ModifierType.Flat, flat);
        Upgrades.Instance.SetModifier(statName, PercentSourceFor(statName), ModifierType.Percent, percent);
        MarkDirty(statName);
    }

    private readonly Dictionary<string, object> _flatTokens = new Dictionary<string, object>();
    private readonly Dictionary<string, object> _percentTokens = new Dictionary<string, object>();

    private object FlatSourceFor(string statName)
    {
        if (!_flatTokens.TryGetValue(statName, out object token))
        {
            token = new object();
            _flatTokens[statName] = token;
        }
        return token;
    }

    private object PercentSourceFor(string statName)
    {
        if (!_percentTokens.TryGetValue(statName, out object token))
        {
            token = new object();
            _percentTokens[statName] = token;
        }
        return token;
    }

    // -------------------------------------------------- backward-compatible API

    public float GetFlatModifier(string statName)
    {
        return Upgrades.Instance != null ? Upgrades.Instance.GetFlat(statName) : 0f;
    }

    /// <summary>Sum of percent modifiers as a raw number. 10 means +10%.</summary>
    public float GetPercentModifier(string statName)
    {
        return Upgrades.Instance != null ? Upgrades.Instance.GetPercent(statName) : 0f;
    }

    /// <summary>Applies modifiers to a base value you supply, ignoring the stored base.</summary>
    public float GetModifiedValue(string statName, float baseValue)
    {
        return Upgrades.Instance != null
            ? Upgrades.Instance.GetValue(statName, baseValue)
            : baseValue;
    }

    /// <summary>Applies modifiers to a base value you supply, ignoring the stored base.</summary>
    public float GetFinalStat(string statName, float baseValue)
    {
        return GetModifiedValue(statName, baseValue);
    }

    // ------------------------------------------------------------------- editor

    private void OnValidate()
    {
        changeEpsilon = Mathf.Max(0f, changeEpsilon);
        if (Application.isPlaying) RebuildDefinitions();
    }
}