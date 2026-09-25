using System;
using System.Collections.Generic;

[Serializable]
public class SkillSeed
{
    public string id;
    public string displayName;
    public string description;
    public int baseCost;
    public int costAddPerLevel;
    public float costMultiplierPerLevel;
    public int maxLevel;
    public string[] requirementIds;
    public int[] requirementLevels; // parallel array, defaults to 1 for each if left null
    public SkillEffect[] effects;

    public SkillSeed(string id, string displayName, string description, int baseCost, int maxLevel,
        string[] requirementIds, params SkillEffect[] effects)
    {
        this.id = id;
        this.displayName = displayName;
        this.description = description;
        this.baseCost = baseCost;
        this.costAddPerLevel = Math.Max(0, baseCost / 2);
        this.costMultiplierPerLevel = 1.12f;
        this.maxLevel = maxLevel;
        this.requirementIds = requirementIds ?? Array.Empty<string>();
        this.requirementLevels = null;
        this.effects = effects ?? Array.Empty<SkillEffect>();
    }
}
public static class SkillSeedData
{
    private static SkillEffect E(string stat, ModifierType type, float value, bool perLevel = true) =>
        new SkillEffect { statName = stat, modifierType = type, value = value, perLevel = perLevel };

    private static string[] R(params string[] ids) => ids;

    public static List<SkillSeed> AttacksTree()
    {
        return new List<SkillSeed>
        {
            new SkillSeed("attacks", "Combat Training", "Unlocks combat upgrades.", 0, 1, R()),

            // left column: cooldown / attack speed spine, bottom to top
            new SkillSeed("attack_speed", "Attack Speed", "10% faster attacks per level.", 10, 5,
                R("attacks"), E(StatNames.AttackSpeed, ModifierType.Percent, 10)),
            new SkillSeed("faster_attack", "Faster Attacks", "10% faster attacks per level.", 20, 5,
                R("attack_speed"), E(StatNames.AttackSpeed, ModifierType.Percent, 10)),
            new SkillSeed("faster_cooldown", "Faster Cooldown", "10% shorter cooldown per level.", 30, 5,
                R("faster_attack"), E(StatNames.CooldownReduction, ModifierType.Percent, 10)),
            new SkillSeed("reload_cooldown", "Faster Reload", "3% faster reload per level.", 25, 10,
                R("faster_cooldown"), E(StatNames.ReloadSpeed, ModifierType.Percent, 3)),

            // middle column: range spine, bottom to top
            new SkillSeed("progression_upgrade", "Offensive Mastery", "Unlocks damage and range.", 0, 1,
                R("attacks")),
            new SkillSeed("attack_damage", "Attack Damage", "10% more damage per level.", 15, 10,
                R("progression_upgrade", "attack_speed"), E(StatNames.AttackDamage, ModifierType.Percent, 10)),
            new SkillSeed("range10", "Extended Range", "10% more range per level.", 20, 5,
                R("attack_damage"), E(StatNames.Range, ModifierType.Percent, 10)),
            new SkillSeed("more_range", "Greater Range", "2% more range per level.", 25, 10,
                R("range10"), E(StatNames.Range, ModifierType.Percent, 2)),

            // apex capstone joining both spines
            new SkillSeed("more_speeeed", "Maximum Speed", "20% faster attacks, cooldowns, and reloads.", 100, 1,
                R("faster_cooldown", "more_range"),
                E(StatNames.AttackSpeed, ModifierType.Percent, 20, false),
                E(StatNames.CooldownReduction, ModifierType.Percent, 20, false),
                E(StatNames.ReloadSpeed, ModifierType.Percent, 20, false)),

            // horizontal damage/multishot spine
            new SkillSeed("more_damage", "More Damage", "2% more damage per level.", 20, 20,
                R("attack_damage"), E(StatNames.AttackDamage, ModifierType.Percent, 2)),
            new SkillSeed("multishot", "Multishot", "5% higher multishot chance per level.", 40, 10,
                R("more_damage"), E(StatNames.MultishotChance, ModifierType.Percent, 5)),
            new SkillSeed("better_chances", "Better Multishot Chance", "3% higher multishot chance per level.", 45, 10,
                R("multishot"), E(StatNames.MultishotChance, ModifierType.Percent, 3)),

            // crit cluster
            new SkillSeed("crit_chance", "Critical Chance", "10% higher crit chance per level.", 35, 5,
                R("multishot"), E(StatNames.CritChance, ModifierType.Percent, 10)),
            new SkillSeed("crit_damage", "Critical Damage", "10% more crit damage per level.", 40, 5,
                R("crit_chance"), E(StatNames.CritDamage, ModifierType.Percent, 10)),
            new SkillSeed("more_crit_percent", "Greater Critical Chance", "3% higher crit chance per level.", 40, 10,
                R("crit_damage"), E(StatNames.CritChance, ModifierType.Percent, 3)),
            new SkillSeed("more_crit_damage", "Greater Critical Damage", "3% more crit damage per level.", 40, 10,
                R("crit_damage"), E(StatNames.CritDamage, ModifierType.Percent, 3)),
            new SkillSeed("max_crit", "Critical Mastery", "20% higher crit chance and damage.", 120, 1,
                R("more_crit_percent", "more_crit_damage", "better_chances"),
                E(StatNames.CritChance, ModifierType.Percent, 20, false),
                E(StatNames.CritDamage, ModifierType.Percent, 20, false)),

            // mystery / lifesteal cluster
            new SkillSeed("what_is_this", "Vampiric Arts", "Unlocks lifesteal upgrades.", 5, 1,
                R("crit_chance")),
            new SkillSeed("lifesteal_q", "Lifesteal Chance", "10% higher lifesteal chance.", 30, 1,
                R("what_is_this"), E(StatNames.LifestealChance, ModifierType.Percent, 10, false)),
            new SkillSeed("lifesteal_gain", "Lifesteal Healing", "10% more lifesteal healing per level.", 25, 10,
                R("lifesteal_q"), E(StatNames.LifestealAmount, ModifierType.Percent, 10)),
            new SkillSeed("useless_upgrade", "Balanced Lifesteal", "5% more lifesteal chance and healing per level.", 5, 10,
                R("lifesteal_gain"),
                E(StatNames.LifestealChance, ModifierType.Percent, 5),
                E(StatNames.LifestealAmount, ModifierType.Percent, 5)),
            new SkillSeed("more_lifesteal", "Greater Lifesteal Chance", "2% higher lifesteal chance per level.", 25, 10,
                R("lifesteal_q"), E(StatNames.LifestealChance, ModifierType.Percent, 2)),
            new SkillSeed("more_lifesteal_again", "Greater Lifesteal Healing", "2% more lifesteal healing per level.", 25, 10,
                R("lifesteal_gain", "more_lifesteal"), E(StatNames.LifestealAmount, ModifierType.Percent, 2)),
            new SkillSeed("vampire", "Vampire", "15% more lifesteal chance and healing.", 90, 1,
                R("more_lifesteal_again"),
                E(StatNames.LifestealAmount, ModifierType.Percent, 15, false),
                E(StatNames.LifestealChance, ModifierType.Percent, 15, false)),
        };
    }

    public static List<SkillSeed> SupportTree()
    {
        return new List<SkillSeed>
        {
            new SkillSeed("support", "Tactical Support", "Unlocks support upgrades.", 0, 1, R()),

            new SkillSeed("burn_rate", "Burn Damage", "10% more burn damage per level.", 10, 5,
                R("support"), E(StatNames.BurnRate, ModifierType.Percent, 10)),
            new SkillSeed("incremental_burn", "Improved Burn Damage", "5% more burn damage per level.", 15, 10,
                R("burn_rate"), E(StatNames.BurnRate, ModifierType.Percent, 5)),
            new SkillSeed("brun_upgrades", "Throwable Mastery", "Unlocks throwable upgrades.", 0, 1,
                R("incremental_burn")),
            new SkillSeed("longer_burn", "Longer Burn", "10% longer burn per level.", 25, 5,
                R("brun_upgrades"), E(StatNames.BurnDuration, ModifierType.Percent, 10)),
            new SkillSeed("flash_damage", "Flash Damage", "10% more flash damage per level.", 25, 5,
                R("brun_upgrades"), E(StatNames.FlashDamage, ModifierType.Percent, 10)),
            new SkillSeed("incremental_increase", "Larger Blast Area", "3% larger blast per level.", 20, 10,
                R("brun_upgrades"), E(StatNames.ThrowableRadius, ModifierType.Percent, 3)),
            new SkillSeed("flash_length", "Longer Flash", "10% longer flash per level.", 30, 5,
                R("flash_damage", "incremental_increase"), E(StatNames.FlashDuration, ModifierType.Percent, 10)),

            new SkillSeed("fire_damage", "Molotov Damage", "10% more Molotov damage per level.", 20, 10,
                R("burn_rate"), E(StatNames.MolotovDamage, ModifierType.Percent, 10)),
            new SkillSeed("more_damage_15", "Greater Molotov Damage", "15% more Molotov damage.", 35, 1,
                R("fire_damage"), E(StatNames.MolotovDamage, ModifierType.Percent, 15, false)),
            new SkillSeed("increas_radius", "Increased Radius", "10% larger throwable radius per level.", 40, 5,
                R("more_damage_15"), E(StatNames.ThrowableRadius, ModifierType.Percent, 10)),
            new SkillSeed("more_damage_10", "Throwable Damage", "10% more frag damage per level.", 30, 10,
                R("more_damage_15"), E(StatNames.ThrowableDamage, ModifierType.Percent, 10)),
            new SkillSeed("even_more", "Greater Throwable Damage", "2% more frag damage per level.", 25, 20,
                R("more_damage_10"), E(StatNames.ThrowableDamage, ModifierType.Percent, 2)),

            new SkillSeed("nuke", "Nuke", "Unlocks the nuke.", 200, 1,
                R("more_damage_10"), E(StatNames.NukeUnlocked, ModifierType.Flat, 1, false)),
            new SkillSeed("shorter_reload_35", "Nuke Cooldown I", "35% shorter Nuke cooldown.", 60, 1,
                R("nuke"), E(StatNames.ThrowableReloadSpeed, ModifierType.Percent, 35, false)),
            new SkillSeed("shorter_reload_40", "Nuke Cooldown II", "40% shorter Nuke cooldown.", 80, 1,
                R("shorter_reload_35"), E(StatNames.ThrowableReloadSpeed, ModifierType.Percent, 40, false)),
        };
    }
    public static List<SkillSeed> DefenseTree()
    {
        return new List<SkillSeed>
        {
            new SkillSeed("defense", "Defensive Training", "Unlocks defense upgrades.", 0, 1, R()),

            new SkillSeed("health", "Health", "10% more health per level.", 10, 10,
                R("defense"), E(StatNames.Health, ModifierType.Percent, 10)),
            new SkillSeed("more_health", "Greater Health", "10% more health per level.", 20, 10,
                R("health"), E(StatNames.Health, ModifierType.Percent, 10)),
            new SkillSeed("more_health_q", "Improved Health", "4% more health per level.", 25, 20,
                R("more_health"), E(StatNames.Health, ModifierType.Percent, 4)),

            new SkillSeed("add_defence", "Unlock Defense", "Unlocks the defense bar.", 30, 1,
                R("health"), E(StatNames.DefenceUnlocked, ModifierType.Flat, 1, false)),
            new SkillSeed("defence", "Defense Capacity", "20 more defense.", 40, 1,
                R("add_defence"), E(StatNames.Defence, ModifierType.Flat, 20, false)),
            new SkillSeed("more_defence", "Greater Defense", "2% more defense per level.", 25, 20,
                R("defence"), E(StatNames.Defence, ModifierType.Percent, 2)),
            new SkillSeed("health_defence", "Health and Defense", "20% more health and defense.", 100, 1,
                R("add_defence", "defence"),
                E(StatNames.Health, ModifierType.Percent, 20, false),
                E(StatNames.Defence, ModifierType.Percent, 20, false)),

            new SkillSeed("dodge", "Dodge", "10% dodge chance.", 50, 1,
                R("health_defence"), E(StatNames.DodgeChance, ModifierType.Flat, 10, false)),
            new SkillSeed("more_dodge", "Greater Dodge Chance", "1% more dodge chance per level.", 20, 20,
                R("dodge"), E(StatNames.DodgeChance, ModifierType.Flat, 1)),
            new SkillSeed("even_more_dodge", "Dodge Mastery", "20% more dodge chance.", 90, 1,
                R("more_dodge"), E(StatNames.DodgeChance, ModifierType.Flat, 20, false)),

            new SkillSeed("pizza_circle", "Pulsar Ring", "Unlocks Pulsar Ring.", 60, 1,
                R("more_health_q"), E(StatNames.PizzaUnlocked, ModifierType.Flat, 1, false)),
            new SkillSeed("pizza_more_damage", "Pulsar Ring Damage", "10% more Pulsar damage per level.", 30, 5,
                R("pizza_circle"), E(StatNames.PizzaDamage, ModifierType.Percent, 10)),
            new SkillSeed("pizza_last_longer", "Pulsar Ring Duration", "10% longer Pulsar per level.", 30, 5,
                R("pizza_circle"), E(StatNames.PizzaDuration, ModifierType.Percent, 10)),
            new SkillSeed("pizza_more_damage2", "Greater Pulsar Damage", "2% more Pulsar damage per level.", 25, 20,
                R("pizza_more_damage"), E(StatNames.PizzaDamage, ModifierType.Percent, 2)),
            new SkillSeed("pizza_permanent", "Permanent Pulsar Ring", "Pulsar Ring stays active.", 80, 1,
                R("pizza_last_longer"), E(StatNames.PizzaPermanent, ModifierType.Flat, 1, false)),
        };
    }

    // Combined lookup so requirementIds (and anything else) can resolve a skill by id in O(1).
    public static Dictionary<string, SkillSeed> BuildSkillDictionary()
    {
        var dict = new Dictionary<string, SkillSeed>();

        foreach (var tree in new[] { AttacksTree(), SupportTree(), DefenseTree() })
        {
            foreach (var skill in tree)
            {
                if (dict.ContainsKey(skill.id))
                    throw new Exception($"Duplicate skill id: {skill.id}");
                dict[skill.id] = skill;
            }
        }

        return dict;
    }
}
