using System.Collections;
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
    }

    public void Configure(
        TowerAbilityController controller,
        AbilityCooldownIcon sewerPulse,
        AbilityCooldownIcon emp,
        AbilityCooldownIcon toxicFlood,
        NukeAbility nuke,
        AbilityCooldownIcon nukeCooldownIcon,
        PulsarRing pulsar,
        AbilityCooldownIcon pulsarCooldownIcon)
    {
        abilityController = controller;
        sewerPulseIcon = sewerPulse;
        empIcon = emp;
        toxicFloodIcon = toxicFlood;
        nukeAbility = nuke;
        nukeIcon = nukeCooldownIcon;
        pulsarRing = pulsar;
        pulsarRingIcon = pulsarCooldownIcon;
    }

    private bool HasMissingSource()
    {
        return abilityController == null || nukeAbility == null || pulsarRing == null;
    }

    private void ResolveSources()
    {
        if (abilityController == null)
            abilityController = FindFirstObjectByType<TowerAbilityController>();
        if (nukeAbility == null)
            nukeAbility = FindFirstObjectByType<NukeAbility>();
        if (pulsarRing == null)
            pulsarRing = FindFirstObjectByType<PulsarRing>();
    }
}

[DisallowMultipleComponent]
public sealed class AbilityCooldownIcon : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private UnityEngine.UI.Image iconImage;
    [SerializeField] private UnityEngine.UI.Image cooldownOverlay;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private RectTransform animatedTransform;

    [Header("Appearance")]
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.42f, 0.42f, 0.42f, 1f);
    [SerializeField, Min(1f)] private float readyPopScale = 1.2f;
    [SerializeField, Min(0.05f)] private float readyPopDuration = 0.28f;

    private Coroutine popRoutine;
    private bool stateInitialized;
    private bool wasCoolingDown;
    private bool wasUnlocked;
    private string displayedText;

    private void Awake()
    {
        if (animatedTransform == null)
            animatedTransform = transform as RectTransform;
    }

    private void OnDisable()
    {
        if (popRoutine != null)
        {
            StopCoroutine(popRoutine);
            popRoutine = null;
        }

        if (animatedTransform != null)
            animatedTransform.localScale = Vector3.one;
    }

    public void Configure(
        UnityEngine.UI.Image icon,
        UnityEngine.UI.Image overlay,
        TMP_Text timer,
        RectTransform animatedRect,
        Color iconReadyColor)
    {
        iconImage = icon;
        cooldownOverlay = overlay;
        cooldownText = timer;
        animatedTransform = animatedRect;
        readyColor = iconReadyColor;
    }

    public void SetState(
        bool unlocked,
        float remainingSeconds,
        float totalDuration,
        string readyText = "")
    {
        float remaining = Mathf.Max(0f, remainingSeconds);
        bool coolingDown = unlocked && remaining > 0.001f;

        if (iconImage != null)
            iconImage.color = unlocked ? readyColor : lockedColor;

        if (cooldownOverlay != null)
        {
            cooldownOverlay.enabled = !unlocked || coolingDown;
            cooldownOverlay.fillAmount = unlocked
                ? Mathf.Clamp01(remaining / Mathf.Max(0.01f, totalDuration))
                : 1f;
        }

        SetTimerText(unlocked, coolingDown, remaining, readyText);

        if (stateInitialized && unlocked && !coolingDown && (wasCoolingDown || !wasUnlocked))
            PlayReadyPop();

        stateInitialized = true;
        wasCoolingDown = coolingDown;
        wasUnlocked = unlocked;
    }

    private void SetTimerText(
        bool unlocked,
        bool coolingDown,
        float remaining,
        string readyText)
    {
        string nextText = string.Empty;
        if (!unlocked)
        {
            nextText = "LOCKED";
        }
        else if (coolingDown)
        {
            nextText = remaining < 10f
                ? remaining.ToString("0.0")
                : Mathf.CeilToInt(remaining).ToString();
        }
        else if (!string.IsNullOrEmpty(readyText))
        {
            nextText = readyText;
        }

        if (cooldownText != null && displayedText != nextText)
        {
            displayedText = nextText;
            cooldownText.text = nextText;
        }
    }

    private void PlayReadyPop()
    {
        if (!isActiveAndEnabled || animatedTransform == null)
            return;

        if (popRoutine != null)
            StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(AnimateReadyPop());
    }

    private IEnumerator AnimateReadyPop()
    {
        float halfDuration = Mathf.Max(0.025f, readyPopDuration * 0.5f);

        for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.unscaledDeltaTime)
        {
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / halfDuration);
            animatedTransform.localScale = Vector3.one * Mathf.Lerp(1f, readyPopScale, progress);
            yield return null;
        }

        for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.unscaledDeltaTime)
        {
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / halfDuration);
            animatedTransform.localScale = Vector3.one * Mathf.Lerp(readyPopScale, 1f, progress);
            yield return null;
        }

        animatedTransform.localScale = Vector3.one;
        popRoutine = null;
    }
}
