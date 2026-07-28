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
            new SkillSeed("attacks", "Attacks", "no perks", 0, 1, R()),

            // left column: cooldown / attack speed spine, bottom to top
            new SkillSeed("attack_speed", "Attack speed", "10% faster attack speed", 10, 5,
                R("attacks"), E(StatNames.AttackSpeed, ModifierType.Percent, 10)),
            new SkillSeed("faster_attack", "faster attack", "Another 10%", 20, 5,
                R("attack_speed"), E(StatNames.AttackSpeed, ModifierType.Percent, 10)),
            new SkillSeed("faster_cooldown", "Faster Cooldown", "faster cooldown, 10%", 30, 5,
                R("faster_attack"), E(StatNames.CooldownReduction, ModifierType.Percent, 10)),
            new SkillSeed("reload_cooldown", "reload/cooldown", "3% lower reload per upgrade", 25, 10,
                R("faster_cooldown"), E(StatNames.ReloadSpeed, ModifierType.Percent, 3)),

            // middle column: range spine, bottom to top
            new SkillSeed("progression_upgrade", "Progression Upgrade", "space saving upgrade", 0, 1,
                R("attacks")),
            new SkillSeed("attack_damage", "Attack Damage", "10% more", 15, 10,
                R("progression_upgrade", "attack_speed"), E(StatNames.AttackDamage, ModifierType.Percent, 10)),
            new SkillSeed("range10", "Range +10%", "more range", 20, 5,
                R("attack_damage"), E(StatNames.Range, ModifierType.Percent, 10)),
            new SkillSeed("more_range", "MORE range", "+2% range per upgrade", 25, 10,
                R("range10"), E(StatNames.Range, ModifierType.Percent, 2)),

            // apex capstone joining both spines
            new SkillSeed("more_speeeed", "MORE SPEEEED", "20% faster cooldown/reload, and speed", 100, 1,
                R("faster_cooldown", "more_range"),
                E(StatNames.AttackSpeed, ModifierType.Percent, 20, false),
                E(StatNames.CooldownReduction, ModifierType.Percent, 20, false),
                E(StatNames.ReloadSpeed, ModifierType.Percent, 20, false)),

            // horizontal damage/multishot spine
            new SkillSeed("more_damage", "More Damage!", "2% more damage per upgrade", 20, 20,
                R("attack_damage"), E(StatNames.AttackDamage, ModifierType.Percent, 2)),
            new SkillSeed("multishot", "Multishot", "5% chance for shot to double", 40, 10,
                R("more_damage"), E(StatNames.MultishotChance, ModifierType.Percent, 5)),
            new SkillSeed("better_chances", "Better chances", "+3% chance for multishot", 45, 10,
                R("multishot"), E(StatNames.MultishotChance, ModifierType.Percent, 3)),

            // crit cluster
            new SkillSeed("crit_chance", "Crit chance", "+10% crit chance", 35, 5,
                R("multishot"), E(StatNames.CritChance, ModifierType.Percent, 10)),
            new SkillSeed("crit_damage", "Crit damage", "+10% more crit damage", 40, 5,
                R("crit_chance"), E(StatNames.CritDamage, ModifierType.Percent, 10)),
            new SkillSeed("more_crit_percent", "MORE Crit %", "+3% crit chance per upgrade", 40, 10,
                R("crit_damage"), E(StatNames.CritChance, ModifierType.Percent, 3)),
            new SkillSeed("more_crit_damage", "MORE Crit damage", "+3% crit damage per upgrade", 40, 10,
                R("crit_damage"), E(StatNames.CritDamage, ModifierType.Percent, 3)),
            new SkillSeed("max_crit", "MAX crit", "+20% crit damage/chance", 120, 1,
                R("more_crit_percent", "more_crit_damage", "better_chances"),
                E(StatNames.CritChance, ModifierType.Percent, 20, false),
                E(StatNames.CritDamage, ModifierType.Percent, 20, false)),

            // mystery / lifesteal cluster
            new SkillSeed("what_is_this", "What$ th1s d0?", "??????????????? (no effect text visible - fill in)", 5, 1,
                R("crit_chance")),
            new SkillSeed("lifesteal_q", "Lifesteal?", "10% for lifesteal", 30, 1,
                R("what_is_this"), E(StatNames.LifestealChance, ModifierType.Percent, 10, false)),
            new SkillSeed("lifesteal_gain", "Lifesteal gain", "10% more lifesteal gain", 25, 10,
                R("lifesteal_q"), E(StatNames.LifestealAmount, ModifierType.Percent, 10)),
            new SkillSeed("useless_upgrade", "Useless Upgrade", "waste your points (troll node - intentionally no effect)", 5, 10,
                R("lifesteal_gain")),
            new SkillSeed("more_lifesteal", "MORE lifesteal", "2% more chance per upgrade", 25, 10,
                R("lifesteal_q"), E(StatNames.LifestealChance, ModifierType.Percent, 2)),
            new SkillSeed("more_lifesteal_again", "MORE lifesteal (again)", "2% more gain per upgrade", 25, 10,
                R("lifesteal_gain", "more_lifesteal"), E(StatNames.LifestealAmount, ModifierType.Percent, 2)),
            new SkillSeed("vampire", "Vampire?", "15% more gain and chance", 90, 1,
                R("more_lifesteal_again"),
                E(StatNames.LifestealAmount, ModifierType.Percent, 15, false),
                E(StatNames.LifestealChance, ModifierType.Percent, 15, false)),
        };
    }

    public static List<SkillSeed> SupportTree()
    {
        return new List<SkillSeed>
        {
            new SkillSeed("support", "Support", "no perks", 0, 1, R()),

            new SkillSeed("burn_rate", "Burn rate", "Enemy burns faster", 10, 5,
                R("support"), E(StatNames.BurnRate, ModifierType.Percent, 10)),
            new SkillSeed("incremental_burn", "Incremental burn", "More Burn Per upg", 15, 10,
                R("burn_rate"), E(StatNames.BurnRate, ModifierType.Percent, 5)),
            new SkillSeed("brun_upgrades", "Brun upgrades", "molotov upgrades", 0, 1,
                R("incremental_burn")),
            new SkillSeed("longer_burn", "Longer Burn", "they can burn longer", 25, 5,
                R("brun_upgrades"), E(StatNames.BurnDuration, ModifierType.Percent, 10)),
            new SkillSeed("flash_damage", "Flash damage", "more damage", 25, 5,
                R("brun_upgrades"), E(StatNames.FlashDamage, ModifierType.Percent, 10)),
            new SkillSeed("incremental_increase", "Incremental increase", "3% larger area", 20, 10,
                R("brun_upgrades"), E(StatNames.ThrowableRadius, ModifierType.Percent, 3)),
            new SkillSeed("flash_length", "Flash length", "longer disorientation", 30, 5,
                R("flash_damage", "incremental_increase"), E(StatNames.FlashDuration, ModifierType.Percent, 10)),

            new SkillSeed("fire_damage", "Fire damage", "10% more", 20, 10,
                R("burn_rate"), E(StatNames.MolotovDamage, ModifierType.Percent, 10)),
            new SkillSeed("more_damage_15", "More Damage", "15%", 35, 1,
                R("fire_damage"), E(StatNames.MolotovDamage, ModifierType.Percent, 15, false)),
            new SkillSeed("increas_radius", "Increas radius", "10% increase radius for all throwables", 40, 5,
                R("more_damage_15"), E(StatNames.ThrowableRadius, ModifierType.Percent, 10)),
            new SkillSeed("more_damage_10", "More damage", "10% more damage", 30, 10,
                R("more_damage_15"), E(StatNames.ThrowableDamage, ModifierType.Percent, 10)),
            new SkillSeed("even_more", "Even more", "2% mroe damage Per upg", 25, 20,
                R("more_damage_10"), E(StatNames.ThrowableDamage, ModifierType.Percent, 2)),

            new SkillSeed("nuke", "Nuke", "YOU get a nuke", 200, 1,
                R("more_damage_10"), E(StatNames.NukeUnlocked, ModifierType.Flat, 1, false)),
            new SkillSeed("shorter_reload_35", "Shorter reload", "35% faster reload", 60, 1,
                R("nuke"), E(StatNames.ThrowableReloadSpeed, ModifierType.Percent, 35, false)),
            new SkillSeed("shorter_reload_40", "shorter reload", "40% faster reload", 80, 1,
                R("shorter_reload_35"), E(StatNames.ThrowableReloadSpeed, ModifierType.Percent, 40, false)),
        };
    }
    public static List<SkillSeed> DefenseTree()
    {
        return new List<SkillSeed>
        {
            new SkillSeed("defense", "Defense", "no perks", 0, 1, R()),

            new SkillSeed("health", "Health", "10% more health", 10, 10,
                R("defense"), E(StatNames.Health, ModifierType.Percent, 10)),
            new SkillSeed("more_health", "MORE Health", "10% mroe health", 20, 10,
                R("health"), E(StatNames.Health, ModifierType.Percent, 10)),
            new SkillSeed("more_health_q", "MORE Health?", "4% more health per upgrade", 25, 20,
                R("more_health"), E(StatNames.Health, ModifierType.Percent, 4)),

            new SkillSeed("add_defence", "Add defence", "Defence bar", 30, 1,
                R("health"), E(StatNames.DefenceUnlocked, ModifierType.Flat, 1, false)),
            new SkillSeed("defence", "Defence", "add 20 defence", 40, 1,
                R("add_defence"), E(StatNames.Defence, ModifierType.Flat, 20, false)),
            new SkillSeed("more_defence", "MORE Defence", "2% more defence per upgrade", 25, 20,
                R("defence"), E(StatNames.Defence, ModifierType.Percent, 2)),
            new SkillSeed("health_defence", "Health/Defence", "20% more health and defence", 100, 1,
                R("add_defence", "defence"),
                E(StatNames.Health, ModifierType.Percent, 20, false),
                E(StatNames.Defence, ModifierType.Percent, 20, false)),

            new SkillSeed("dodge", "Dodge", "You can dodge", 50, 1,
                R("health_defence"), E(StatNames.DodgeChance, ModifierType.Flat, 5, false)),
            new SkillSeed("more_dodge", "MORE dodge", "1% per upgrade", 20, 20,
                R("dodge"), E(StatNames.DodgeChance, ModifierType.Percent, 1)),
            new SkillSeed("even_more_dodge", "Even MORE", "20% more dodge chance", 90, 1,
                R("more_dodge"), E(StatNames.DodgeChance, ModifierType.Percent, 20, false)),

            new SkillSeed("pizza_circle", "pizza circle", "there lactose intollerant? (unlock ability)", 60, 1,
                R("more_health_q"), E(StatNames.PizzaUnlocked, ModifierType.Flat, 1, false)),
            new SkillSeed("pizza_more_damage", "More damage", "10% more damage", 30, 5,
                R("pizza_circle"), E(StatNames.PizzaDamage, ModifierType.Percent, 10)),
            new SkillSeed("pizza_last_longer", "last longer", "the pizza lasts longer", 30, 5,
                R("pizza_circle"), E(StatNames.PizzaDuration, ModifierType.Percent, 10)),
            new SkillSeed("pizza_more_damage2", "MORE damage", "2% more damage per upgrade", 25, 20,
                R("pizza_more_damage"), E(StatNames.PizzaDamage, ModifierType.Percent, 2)),
            new SkillSeed("pizza_permanent", "Permanent Circle", "doesnt dissapear anymore", 80, 1,
                R("pizza_last_longer"), E(StatNames.PizzaPermanent, ModifierType.Flat, 1, false)),
        };
    }
}