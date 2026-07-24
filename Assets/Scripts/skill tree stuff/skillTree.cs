using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Owns one tree's worth of nodes. Put it on the panel that parents the skill buttons.
/// </summary>
public class SkillTreeScript : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Unique per tree - used as the save key. e.g. 'Ship', 'Pilot'.")]
    public string treeId = "MainTree";
    public bool collectChildrenOnAwake = true;
    public List<skills> nodes = new List<skills>();

    [Header("Refund / Respec")]
    [Range(0f, 1f)] public float refundRate = 1f;

    [Header("Saving")]
    public bool saveToPlayerPrefs = true;
    public bool loadOnStart = true;

    [Header("UI (optional)")]
    public TMP_Text totalSpentText;

    [Tooltip("Seconds between affordability re-colors. Set to 0 and call RefreshAll() from your currency event instead if you prefer zero polling.")]
    public float refreshInterval = 0.2f;

    private float _timer;
    private bool _suppressEvents;

    private string SaveKey => "skilltree." + treeId;

    // ------------------------------------------------------------ lifecycle

    private void Awake()
    {
        if (collectChildrenOnAwake)
        {
            skills[] found = GetComponentsInChildren<skills>(true);
            foreach (skills s in found)
                if (!nodes.Contains(s)) nodes.Add(s);
        }

        HashSet<string> seen = new HashSet<string>();
        foreach (skills n in nodes)
        {
            if (n == null) continue;
            n.Tree = this;
            if (string.IsNullOrEmpty(n.skillId)) n.skillId = n.gameObject.name;
            if (!seen.Add(n.skillId))
                Debug.LogWarning($"[SkillTree {treeId}] duplicate skillId '{n.skillId}' - saving will be wrong.");
        }
    }

    private void OnEnable() { skills.OnSkillChanged += HandleSkillChanged; }
    private void OnDisable() { skills.OnSkillChanged -= HandleSkillChanged; }

    private void Start()
    {
        if (loadOnStart) Load();
        else ReapplyAllEffects();
        RefreshAll();
    }

    private void Update()
    {
        if (refreshInterval <= 0f) return;
        _timer += Time.unscaledDeltaTime;
        if (_timer < refreshInterval) return;
        _timer = 0f;
        RefreshAll();
    }

    private void HandleSkillChanged(skills changed)
    {
        if (_suppressEvents) return;
        if (changed != null && !nodes.Contains(changed)) return; // another tree's node
        RefreshAll();
        Save();
    }

    // -------------------------------------------------------------- display

    public void RefreshAll()
    {
        foreach (skills n in nodes)
            if (n != null) n.Refresh();

        if (totalSpentText != null) totalSpentText.text = TotalSpent.ToString();
    }

    public int TotalSpent
    {
        get
        {
            int t = 0;
            foreach (skills n in nodes)
                if (n != null) t += n.TotalSpent;
            return t;
        }
    }

    // ----------------------------------------------------------- dependency

    /// <summary>Every node that lists <paramref name="node"/> as a requirement.</summary>
    public IEnumerable<skills> GetDependents(skills node)
    {
        foreach (skills n in nodes)
        {
            if (n == null || n == node || n.requirements == null) continue;
            foreach (SkillRequirement r in n.requirements)
            {
                if (r != null && r.skill == node) { yield return n; break; }
            }
        }
    }

    /// <summary>False if dropping one level would orphan an already-purchased child.</summary>
    public bool CanRefundLevel(skills node)
    {
        if (node == null || node.level <= 0) return false;
        int after = node.level - 1;

        foreach (skills dep in GetDependents(node))
        {
            if (dep.level <= 0) continue;
            foreach (SkillRequirement r in dep.requirements)
            {
                if (r == null || r.skill != node) continue;
                if (after < Mathf.Max(1, r.requiredLevel)) return false;
            }
        }
        return true;
    }

    // ---------------------------------------------------------------- respec

    /// <summary>Full respec: peels levels off leaves first until nothing is refundable.</summary>
    public void RefundAll()
    {
        _suppressEvents = true;
        bool changed = true;
        int guard = 0;

        while (changed && guard++ < 10000)
        {
            changed = false;
            foreach (skills n in nodes)
            {
                if (n == null || n.level <= 0) continue;
                if (!CanRefundLevel(n)) continue;
                if (n.RefundOneLevel(refundRate)) changed = true;
            }
        }

        _suppressEvents = false;
        RefreshAll();
        Save();
    }

    /// <summary>Wipes levels without giving currency back. For New Game / debug.</summary>
    public void ResetTreeNoRefund()
    {
        _suppressEvents = true;
        foreach (skills n in nodes)
            if (n != null) n.SetLevel(0, true);
        _suppressEvents = false;

        RefreshAll();
        Save();
    }

    // ----------------------------------------------------------- save / load

    [Serializable] private class Entry { public string id; public int level; }
    [Serializable] private class SaveData { public List<Entry> entries = new List<Entry>(); }

    public void Save()
    {
        if (!saveToPlayerPrefs) return;

        SaveData data = new SaveData();
        foreach (skills n in nodes)
            if (n != null) data.entries.Add(new Entry { id = n.skillId, level = n.level });

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public void Load()
    {
        if (!saveToPlayerPrefs || !PlayerPrefs.HasKey(SaveKey))
        {
            ReapplyAllEffects();
            return;
        }

        SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
        _suppressEvents = true;

        if (data != null && data.entries != null)
        {
            foreach (Entry e in data.entries)
            {
                skills node = Find(e.id);
                if (node != null) node.SetLevel(e.level, true);
            }
        }

        _suppressEvents = false;
        RefreshAll();
    }

    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
    }

    /// <summary>Pushes current levels into Upgrades without changing anything.</summary>
    public void ReapplyAllEffects()
    {
        foreach (skills n in nodes)
            if (n != null) n.ApplyEffects();
    }

    public skills Find(string id)
    {
        foreach (skills n in nodes)
            if (n != null && n.skillId == id) return n;
        return null;
    }
}