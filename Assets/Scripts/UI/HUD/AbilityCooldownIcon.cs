using System.Collections;
using TMPro;
using UnityEngine;

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
    [SerializeField, Min(1f)] private float readyPopScale = 1.2f;
    [SerializeField, Min(0.05f)] private float readyPopDuration = 0.28f;

    private Coroutine popRoutine;
    private bool stateInitialized;
    private bool wasCoolingDown;
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
        bool isOwned,
        float remainingSeconds,
        float totalDuration,
        string readyText = "")
    {
        if (!isOwned)
        {
            if (gameObject.activeSelf)
            {
                ResetPresentationState();
                gameObject.SetActive(false);
            }
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        float remaining = Mathf.Max(0f, remainingSeconds);
        bool coolingDown = remaining > 0.001f;

        if (iconImage != null)
            iconImage.color = readyColor;

        if (cooldownOverlay != null)
        {
            cooldownOverlay.enabled = coolingDown;
            cooldownOverlay.fillAmount = Mathf.Clamp01(
                remaining / Mathf.Max(0.01f, totalDuration));
        }

        SetTimerText(coolingDown, remaining, readyText);

        if (stateInitialized && !coolingDown && wasCoolingDown)
            PlayReadyPop();

        stateInitialized = true;
        wasCoolingDown = coolingDown;
    }

    private void SetTimerText(
        bool coolingDown,
        float remaining,
        string readyText)
    {
        string nextText = string.Empty;
        if (coolingDown)
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

    private void ResetPresentationState()
    {
        stateInitialized = false;
        wasCoolingDown = false;
        displayedText = string.Empty;

        if (cooldownText != null)
            cooldownText.text = string.Empty;
        if (cooldownOverlay != null)
        {
            cooldownOverlay.enabled = false;
            cooldownOverlay.fillAmount = 0f;
        }
    }

    private void PlayReadyPop()
    {
        if (!isActiveAndEnabled || animatedTransform == null)
            return;

        BackgroundMusic.AbilityBecameReady();
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
