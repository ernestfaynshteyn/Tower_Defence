using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AbilityCooldownHud : MonoBehaviour
{
    [SerializeField] private TowerAbilityController abilityController;
    [SerializeField] private AbilityCooldownIcon sewerPulseIcon;
    [SerializeField] private AbilityCooldownIcon empIcon;
    [SerializeField] private AbilityCooldownIcon toxicFloodIcon;
    [SerializeField] private NukeAbility nukeAbility;
    [SerializeField] private AbilityCooldownIcon nukeIcon;
    [SerializeField] private PulsarRing pulsarRing;
    [SerializeField] private AbilityCooldownIcon pulsarRingIcon;
    [SerializeField] private PrototypeArmoryCombat prototypeArmoryCombat;
    [SerializeField] private AbilityCooldownIcon overchargeIcon;
    [SerializeField] private AbilityCooldownIcon freezeBurstIcon;

    private float nextControllerSearchTime;

    private void Awake()
    {
        ResolveSources();
    }

    private void Update()
    {
        if (HasMissingSource() && Time.unscaledTime >= nextControllerSearchTime)
        {
            ResolveSources();
            nextControllerSearchTime = Time.unscaledTime + 1f;
        }

        if (abilityController == null)
        {
            sewerPulseIcon?.SetState(false, 0f, 1f);
            empIcon?.SetState(false, 0f, 1f);
            toxicFloodIcon?.SetState(false, 0f, 1f);
        }
        else
        {
            sewerPulseIcon?.SetState(
                abilityController.IsSewerPulseUnlocked,
                abilityController.SewerPulseCooldownRemaining,
                abilityController.SewerPulseCooldownDuration);
            empIcon?.SetState(
                abilityController.IsEmpUnlocked,
                abilityController.EmpCooldownRemaining,
                abilityController.EmpCooldownDuration);
            toxicFloodIcon?.SetState(
                abilityController.IsToxicFloodUnlocked,
                abilityController.ToxicFloodCooldownRemaining,
                abilityController.ToxicFloodCooldownDuration);
        }

        if (nukeAbility == null)
        {
            nukeIcon?.SetState(false, 0f, 1f);
        }
        else
        {
            nukeIcon?.SetState(
                nukeAbility.IsUnlocked,
                nukeAbility.CooldownRemaining,
                nukeAbility.CooldownDuration);
        }

        if (pulsarRing == null)
        {
            pulsarRingIcon?.SetState(false, 0f, 1f);
        }
        else
        {
            string activeLabel = pulsarRing.IsPermanent || pulsarRing.IsActive
                ? "ACTIVE"
                : string.Empty;
            pulsarRingIcon?.SetState(
                pulsarRing.IsUnlocked,
                pulsarRing.CooldownRemaining,
                pulsarRing.CooldownDuration,
                activeLabel);
        }

        if (prototypeArmoryCombat == null)
        {
            overchargeIcon?.SetState(false, 0f, 1f);
            freezeBurstIcon?.SetState(false, 0f, 1f);
        }
        else
        {
            overchargeIcon?.SetState(
                prototypeArmoryCombat.IsOverchargeUnlocked,
                prototypeArmoryCombat.OverchargeCooldownRemaining,
                prototypeArmoryCombat.OverchargeCooldownDuration);
            freezeBurstIcon?.SetState(
                prototypeArmoryCombat.IsFreezeBurstUnlocked,
                prototypeArmoryCombat.FreezeBurstCooldownRemaining,
                prototypeArmoryCombat.FreezeBurstCooldownDuration);
        }
    }

    public void Configure(
        TowerAbilityController controller,
        AbilityCooldownIcon sewerPulse,
        AbilityCooldownIcon emp,
        AbilityCooldownIcon toxicFlood,
        NukeAbility nuke,
        AbilityCooldownIcon nukeCooldownIcon,
        PulsarRing pulsar,
        AbilityCooldownIcon pulsarCooldownIcon,
        PrototypeArmoryCombat armoryCombat = null,
        AbilityCooldownIcon overchargeCooldownIcon = null,
        AbilityCooldownIcon freezeBurstCooldownIcon = null)
    {
        abilityController = controller;
        sewerPulseIcon = sewerPulse;
        empIcon = emp;
        toxicFloodIcon = toxicFlood;
        nukeAbility = nuke;
        nukeIcon = nukeCooldownIcon;
        pulsarRing = pulsar;
        pulsarRingIcon = pulsarCooldownIcon;
        prototypeArmoryCombat = armoryCombat;
        overchargeIcon = overchargeCooldownIcon;
        freezeBurstIcon = freezeBurstCooldownIcon;
    }

    private bool HasMissingSource()
    {
        bool expectsArmoryCombat = overchargeIcon != null || freezeBurstIcon != null;
        return abilityController == null || nukeAbility == null || pulsarRing == null ||
               (expectsArmoryCombat && prototypeArmoryCombat == null);
    }

    private void ResolveSources()
    {
        if (abilityController == null)
            abilityController = FindFirstObjectByType<TowerAbilityController>();
        if (nukeAbility == null)
            nukeAbility = FindFirstObjectByType<NukeAbility>();
        if (pulsarRing == null)
            pulsarRing = FindFirstObjectByType<PulsarRing>();
        if (prototypeArmoryCombat == null &&
            (overchargeIcon != null || freezeBurstIcon != null))
        {
            prototypeArmoryCombat = FindFirstObjectByType<PrototypeArmoryCombat>();
        }
    }
}
