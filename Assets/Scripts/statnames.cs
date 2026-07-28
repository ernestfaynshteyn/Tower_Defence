/// <summary>
/// Central list of stat keys so SkillEffect.statName and gameplay code
/// (Upgrades.Instance.GetValue(...)) always agree on spelling.
/// </summary>
public static class StatNames
{
    // ---- Attacks tree ----
    public const string AttackDamage = "AttackDamage";
    public const string AttackSpeed = "AttackSpeed";
    public const string CooldownReduction = "CooldownReduction";
    public const string ReloadSpeed = "ReloadSpeed";
    public const string Range = "Range";
    public const string MultishotChance = "MultishotChance";
    public const string CritChance = "CritChance";
    public const string CritDamage = "CritDamage";
    public const string LifestealChance = "LifestealChance";
    public const string LifestealAmount = "LifestealAmount";

    // ---- Support / throwables tree ----
    public const string BurnRate = "BurnRate";
    public const string BurnDuration = "BurnDuration";
    public const string MolotovDamage = "MolotovDamage";
    public const string FlashDamage = "FlashDamage";
    public const string FlashDuration = "FlashDuration";
    public const string ThrowableRadius = "ThrowableRadius";
    public const string ThrowableDamage = "ThrowableDamage";
    public const string ThrowableReloadSpeed = "ThrowableReloadSpeed";
    public const string NukeUnlocked = "NukeUnlocked"; // treat as a 0/1 flag stat

    // ---- Defense tree ----
    public const string Health = "Health";
    public const string Defence = "Defence";
    public const string DefenceUnlocked = "DefenceUnlocked"; // 0/1 flag
    public const string DodgeChance = "DodgeChance";
    public const string PizzaUnlocked = "PizzaUnlocked";     // 0/1 flag
    public const string PizzaDamage = "PizzaDamage";
    public const string PizzaDuration = "PizzaDuration";
    public const string PizzaPermanent = "PizzaPermanent";   // 0/1 flag
}