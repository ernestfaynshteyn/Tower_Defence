using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays a short-lived, world-space damage number and merges rapid hits
/// against the same enemy into one readable popup.
/// </summary>
public sealed class DamageNumberPopup : MonoBehaviour
{
    private const float Lifetime = 0.7f;
    private const float RiseSpeed = 0.7f;
    private const float VerticalOffset = 0.7f;
    private const float TypeOffset = 0.22f;

    private Transform followTarget;
    private TextMeshPro label;
    private Camera mainCamera;
    private float damageTotal;
    private float timeRemaining;
    private float elapsed;
    private float horizontalOffset;
    private int displayedDamage;
    private Color baseColor;

    public static DamageNumberPopup Create(Transform target, bool isBurnDamage)
    {
        if (target == null)
            return null;

        var popupObject = new GameObject("Damage Number");
        var popup = popupObject.AddComponent<DamageNumberPopup>();
        popup.Initialize(target, isBurnDamage);
        return popup;
    }

    private void Awake()
    {
        mainCamera = Camera.main;
        label = gameObject.AddComponent<TextMeshPro>();
        label.text = string.Empty;
        label.fontSize = 4f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;

        if (label.font == null)
        {
            label.font = TMP_Settings.defaultFontAsset;
            if (label.font == null)
                label.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sortingOrder = 100;
    }

    private void Initialize(Transform target, bool isBurnDamage)
    {
        followTarget = target;
        horizontalOffset = Random.Range(-0.1f, 0.1f) + (isBurnDamage ? TypeOffset : -TypeOffset);
        baseColor = isBurnDamage ? new Color(1f, 0.18f, 0.18f, 1f) : Color.white;
        label.color = baseColor;
    }

    public void AddDamage(float amount)
    {
        if (amount <= 0f || label == null)
            return;

        damageTotal += amount;
        int roundedDamage = Mathf.RoundToInt(damageTotal);
        if (roundedDamage != displayedDamage)
        {
            displayedDamage = roundedDamage;
            label.text = displayedDamage.ToString("N0", CultureInfo.InvariantCulture);
        }
        timeRemaining = Lifetime;
        elapsed = 0f;
    }

    private void LateUpdate()
    {
        if (followTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        float deltaTime = Time.deltaTime;
        elapsed += deltaTime;
        timeRemaining -= deltaTime;

        Vector3 towardCamera = mainCamera != null ? -mainCamera.transform.forward * 0.1f : Vector3.zero;
        transform.position = followTarget.position +
            Vector3.up * (VerticalOffset + elapsed * RiseSpeed) +
            Vector3.right * horizontalOffset + towardCamera;
        transform.rotation = mainCamera != null ? mainCamera.transform.rotation : Quaternion.identity;

        float alpha = Mathf.Clamp01(timeRemaining / Lifetime);
        label.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

        if (timeRemaining <= 0f)
            Destroy(gameObject);
    }
}
