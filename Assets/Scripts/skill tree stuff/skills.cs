using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ModifierType
{
    Flat,
    Percent
}

[Serializable]
public class SkillEffect
{
    [Tooltip("Must match the key gameplay code asks for, e.g. StatNames.Damage")]
    public string statName;
    public ModifierType modifierType = ModifierType.Flat;

    [Tooltip("Amount granted per level (or the whole amount, if perLevel is off).")]
    public float value = 0f;

    [Tooltip("On: total = value * level.  Off: total = value as soon as level >= 1.")]
    public bool perLevel = true;

    public float TotalAt(int level)
    {
        if (level <= 0) return 0f;
        return perLevel ? value * level : value;
    }
}

[Serializable]
public class SkillRequirement
{
    public skills skill;
    [Min(1)] public int requiredLevel = 1;
}

/// <summary>
/// One node in the tree. Sits on the same GameObject as its Button.
/// </summary>
[RequireComponent(typeof(Button))]
public class skills : MonoBehaviour
{
    /// <summary>Raised whenever any node's level changes. The tree listens to this.</summary>
    public static event Action<skills> OnSkillChanged;

    [Header("Identity")]
    [Tooltip("Unique save key. Auto-filled from the GameObject name if left blank.")]
    public string skillId = "";
    public string displayName = "";
    [TextArea(2, 4)] public string description = "";

    [Header("Progression")]
    public int level = 0;
    [Min(1)] public int maxLevel = 1;
    [Min(0)] public int baseCost = 10;
    [Tooltip("Added to the cost for each level already owned. 10 + 5 => 10, 15, 20, ...")]
    public int costAddPerLevel = 0;
    [Tooltip("Multiplied into the cost for each level already owned. 1.5 => 10, 15, 22, ...")]
    public float costMultiplierPerLevel = 1f;

    [Header("Requirements")]
    public SkillRequirement[] requirements;

    [Header("Effects")]
    public SkillEffect[] effects;

    [Header("UI")]
    [Tooltip("Wires Buy() to the Button in Awake. If ON, do NOT also add Buy() to the Button's OnClick list in the Inspector, or every click buys twice.")]
    public bool autoWireButton = true;
    public Image iconImage;
    public TMP_Text levelText;
    public TMP_Text costText;
    public string maxLevelLabel = "MAX";

    [Header("Colors")]
    public Color lockColor = Color.gray;
    public Color affordableColor = Color.green;
    public Color unaffordableColor = new Color(0.35f, 0.5f, 0.35f);
    public Color maxColor = Color.yellow;

    public bool IsUnlocked { get; private set; }
    public bool IsMaxed => level >= maxLevel;
    public SkillTreeScript Tree { get; set; }

    private Button _button;

    private void Reset()
    {
        skillId = gameObject.name;
        displayName = gameObject.name;
    }

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (string.IsNullOrEmpty(skillId)) skillId = gameObject.name;
        if (autoWireButton && _button != null) _button.onClick.AddListener(Buy);
    }

    private void Start()
    {
        // Tree.Start() also refreshes; harmless either way, and covers trees with no manager.
        Refresh();
    }

    private void OnDestroy()
    {
        if (Upgrades.Instance != null) Upgrades.Instance.RemoveSource(this);
    }

    // ----------------------------------------------------------------- cost

    /// <summary>Cost of buying the Nth level (1-based).</summary>
    public int GetCostForLevel(int targetLevel)
    {
        int owned = Mathf.Max(0, targetLevel - 1);
        float mult = Mathf.Pow(Mathf.Max(0.01f, costMultiplierPerLevel), owned);
        float cost = (baseCost + costAddPerLevel * owned) * mult;
        return Mathf.Max(0, Mathf.RoundToInt(cost));
    }

    public int NextCost => IsMaxed ? 0 : GetCostForLevel(level + 1);

    public int TotalSpent
    {
        get
        {
            int t = 0;
            for (int i = 1; i <= level; i++) t += GetCostForLevel(i);
            return t;
        }
    }

    // --------------------------------------------------------------- unlock

    public bool CheckUnlock()
    {
        if (requirements == null || requirements.Length == 0) return true;

        foreach (SkillRequirement r in requirements)
        {
            if (r == null || r.skill == null) return false;
            if (r.skill.level < Mathf.Max(1, r.requiredLevel)) return false;
        }
        return true;
    }

    // ------------------------------------------------------------ buy/sell

    public void Buy()
    {
        if (!IsUnlocked)
        {
            Debug.Log($"[{name}] locked - requirements not met.");
            return;
        }
        if (IsMaxed)
        {
            Debug.Log($"[{name}] already at max level.");
            return;
        }
        if (!CurrencyBridge.IsReady)
        {
            Debug.LogError("[skills] No CurrencyManager in the scene.");
            return;
        }

        int cost = NextCost;
        if (!CurrencyBridge.Spend(cost))
        {
            Debug.Log($"[{name}] not enough currency ({CurrencyBridge.Current}/{cost}).");
            Refresh();
            return;
        }

        level++;
        ApplyEffects();
        OnSkillChanged?.Invoke(this);
    }

    /// <summary>Gives one level back. Blocked if a purchased child still depends on this level.</summary>
    public bool RefundOneLevel(float refundRate = 1f)
    {
        if (level <= 0) return false;
        if (Tree != null && !Tree.CanRefundLevel(this))
        {
            Debug.Log($"[{name}] can't refund - another purchased skill depends on it.");
            return false;
        }

        int back = Mathf.RoundToInt(GetCostForLevel(level) * Mathf.Clamp01(refundRate));
        level--;
        ApplyEffects();
        CurrencyBridge.Refund(back);
        OnSkillChanged?.Invoke(this);
        return true;
    }

    /// <summary>Used by save/load and by debug tooling. silent = don't raise the change event.</summary>
    public void SetLevel(int newLevel, bool silent = false)
    {
        level = Mathf.Clamp(newLevel, 0, maxLevel);
        ApplyEffects();
        if (!silent) OnSkillChanged?.Invoke(this);
    }

    // -------------------------------------------------------------- effects

    /// <summary>
    /// Re-pushes this node's totals into Upgrades. Idempotent - safe to call any time.
    /// </summary>
    public void ApplyEffects()
    {
        if (Upgrades.Instance == null) return;

        Upgrades.Instance.RemoveSource(this);
        if (level <= 0 || effects == null) return;

        foreach (SkillEffect e in effects)
        {
            if (e == null || string.IsNullOrEmpty(e.statName)) continue;
            Upgrades.Instance.SetModifier(e.statName, this, e.modifierType, e.TotalAt(level));
        }
    }

    /// <summary>Human-readable effect list, handy for tooltips.</summary>
    public string GetEffectSummary(bool showNextLevel = true)
    {
        if (effects == null || effects.Length == 0) return "";
        int shown = showNextLevel ? Mathf.Min(level + 1, maxLevel) : level;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (SkillEffect e in effects)
        {
            if (e == null || string.IsNullOrEmpty(e.statName)) continue;
            float v = e.TotalAt(shown);
            string suffix = e.modifierType == ModifierType.Percent ? "%" : "";
            sb.AppendLine($"{e.statName} {(v >= 0 ? "+" : "")}{v:0.##}{suffix}");
        }
        return sb.ToString().TrimEnd();
    }

    // -------------------------------------------------------------- display

    public void Refresh()
    {
        IsUnlocked = CheckUnlock();
        bool canAfford = !IsMaxed && CurrencyBridge.Current >= NextCost;

        if (_button != null)
        {
            _button.interactable = IsUnlocked && !IsMaxed;

            ColorBlock c = _button.colors;
            c.normalColor = !IsUnlocked ? lockColor
                          : IsMaxed ? maxColor
                          : canAfford ? affordableColor
                          : unaffordableColor;
            c.disabledColor = IsMaxed ? maxColor : lockColor;
            _button.colors = c;
        }

        if (levelText != null) levelText.text = $"{level}/{maxLevel}";
        if (costText != null) costText.text = IsMaxed ? maxLevelLabel : NextCost.ToString();

        if (iconImage != null)
        {
            Color ic = iconImage.color;
            ic.a = IsUnlocked ? 1f : 0.4f;
            iconImage.color = ic;
        }
    }

    private void OnValidate()
    {
        maxLevel = Mathf.Max(1, maxLevel);
        level = Mathf.Clamp(level, 0, maxLevel);
        costMultiplierPerLevel = Mathf.Max(0.01f, costMultiplierPerLevel);
    }
}