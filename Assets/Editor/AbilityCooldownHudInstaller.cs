#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds a scene-authored cooldown HUD to both gameplay scenes. Everything it
/// creates remains ordinary editable uGUI content in the scene hierarchy.
/// </summary>
[InitializeOnLoad]
internal static class AbilityCooldownHudInstaller
{
    private const int InstallerVersion = 3;
    private const string HudName = "Ability Cooldown HUD";
    private const float ThreeIconWidth = 420f;
    private const float FiveIconWidth = 684f;

    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Game thing.unity",
        "Assets/Scenes/Armory Prototype/Game thing - Armory Prototype.unity",
    };

    private static bool isRunning;

    static AbilityCooldownHudInstaller()
    {
        EditorApplication.delayCall += TryAutomaticInstall;
    }

    [MenuItem("Tools/Tower Defence/Rebuild Ability Cooldown HUD")]
    private static void RebuildFromMenu()
    {
        InstallAllScenes(true);
    }

    private static string InstallKey =>
        "TowerDefence.AbilityCooldownHudInstaller." + Application.dataPath + ".v" + InstallerVersion;

    private static void TryAutomaticInstall()
    {
        if (EditorPrefs.GetBool(InstallKey, false) || isRunning)
            return;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryAutomaticInstall;
            return;
        }

        if (InstallAllScenes(false))
            EditorPrefs.SetBool(InstallKey, true);
    }

    private static bool InstallAllScenes(bool forceSaveLoadedScenes)
    {
        if (isRunning)
            return false;

        isRunning = true;
        bool allSaved = true;

        try
        {
            foreach (string scenePath in ScenePaths)
            {
                if (!System.IO.File.Exists(scenePath))
                    continue;

                Scene scene = SceneManager.GetSceneByPath(scenePath);
                bool wasLoaded = scene.IsValid() && scene.isLoaded;
                bool wasDirty = wasLoaded && scene.isDirty;

                if (!wasLoaded)
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

                bool changed = InstallIntoScene(scene);
                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    bool maySave = !wasLoaded || !wasDirty || forceSaveLoadedScenes;
                    if (maySave)
                    {
                        EditorSceneManager.SaveScene(scene);
                    }
                    else
                    {
                        allSaved = false;
                        Debug.LogWarning(
                            $"[AbilityCooldownHudInstaller] Added the HUD to '{scene.name}' in memory but left " +
                            "the scene unsaved because it already had unsaved work. Save it normally when ready.");
                    }
                }

                if (!wasLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
        catch (Exception exception)
        {
            allSaved = false;
            Debug.LogException(exception);
        }
        finally
        {
            isRunning = false;
        }

        if (allSaved)
            Debug.Log("[AbilityCooldownHudInstaller] Five editable ability cooldown icons are installed.");

        return allSaved;
    }

    private static bool InstallIntoScene(Scene scene)
    {
        Canvas canvas = FindInScene<Canvas>(scene);
        if (canvas == null)
        {
            Debug.LogWarning($"[AbilityCooldownHudInstaller] No Canvas found in '{scene.name}'.");
            return false;
        }

        bool changed = false;
        Transform existing = canvas.transform.Find(HudName);
        RectTransform hudRect;
        if (existing == null)
        {
            GameObject hudObject = CreateUiObject(HudName, canvas.transform);
            hudRect = hudObject.GetComponent<RectTransform>();
            hudRect.sizeDelta = new Vector2(FiveIconWidth, 132f);
            SetBottomLeftPosition(hudRect);
            changed = true;
        }
        else
        {
            hudRect = existing as RectTransform;
        }

        if (hudRect == null)
            return changed;

        bool migrateThreeIconLayout = Mathf.Approximately(hudRect.sizeDelta.x, ThreeIconWidth);
        if (migrateThreeIconLayout)
        {
            hudRect.sizeDelta = new Vector2(FiveIconWidth, hudRect.sizeDelta.y);
            changed = true;
        }

        if (IsOriginalBottomCenterPosition(hudRect))
        {
            SetBottomLeftPosition(hudRect);
            changed = true;
        }

        if (hudRect.GetSiblingIndex() != 0)
        {
            hudRect.SetSiblingIndex(0);
            changed = true;
        }

        AbilityCooldownHud hud = hudRect.GetComponent<AbilityCooldownHud>();
        if (hud == null)
        {
            hud = hudRect.gameObject.AddComponent<AbilityCooldownHud>();
            changed = true;
        }

        AbilityCooldownIcon sewerPulse = CreateOrUpdateIcon(
            hudRect,
            "Sewer Pulse Cooldown",
            "PULSE",
            "Z",
            -264f,
            new Color(0.12f, 0.72f, 0.9f, 1f),
            ref changed);
        AbilityCooldownIcon emp = CreateOrUpdateIcon(
            hudRect,
            "EMP Cooldown",
            "EMP",
            "X",
            -132f,
            new Color(0.32f, 0.52f, 1f, 1f),
            ref changed);
        AbilityCooldownIcon toxic = CreateOrUpdateIcon(
            hudRect,
            "Toxic Flood Cooldown",
            "TOXIC",
            "C",
            0f,
            new Color(0.28f, 0.78f, 0.25f, 1f),
            ref changed);
        AbilityCooldownIcon nuke = CreateOrUpdateIcon(
            hudRect,
            "Nuke Cooldown",
            "NUKE",
            "N",
            132f,
            new Color(0.95f, 0.35f, 0.12f, 1f),
            ref changed);
        AbilityCooldownIcon pulsar = CreateOrUpdateIcon(
            hudRect,
            "Pulsar Ring Cooldown",
            "PULSAR",
            "AUTO",
            264f,
            new Color(0.72f, 0.3f, 0.95f, 1f),
            ref changed);

        if (migrateThreeIconLayout)
        {
            changed |= SetIconPosition(sewerPulse, -264f);
            changed |= SetIconPosition(emp, -132f);
            changed |= SetIconPosition(toxic, 0f);
        }

        TowerAbilityController controller = FindInScene<TowerAbilityController>(scene);
        NukeAbility nukeAbility = FindInScene<NukeAbility>(scene);
        PulsarRing pulsarRing = FindInScene<PulsarRing>(scene);
        hud.Configure(
            controller,
            sewerPulse,
            emp,
            toxic,
            nukeAbility,
            nuke,
            pulsarRing,
            pulsar);
        EditorUtility.SetDirty(hud);
        return true;
    }

    private static bool IsOriginalBottomCenterPosition(RectTransform rect)
    {
        return rect.anchorMin == new Vector2(0.5f, 0f) &&
               rect.anchorMax == new Vector2(0.5f, 0f) &&
               rect.pivot == new Vector2(0.5f, 0f) &&
               rect.anchoredPosition == new Vector2(0f, 32f);
    }

    private static void SetBottomLeftPosition(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(32f, 32f);
    }

    private static bool SetIconPosition(AbilityCooldownIcon icon, float xPosition)
    {
        if (icon == null || !(icon.transform is RectTransform rect))
            return false;

        Vector2 position = new Vector2(xPosition, 0f);
        if (rect.anchoredPosition == position)
            return false;

        rect.anchoredPosition = position;
        EditorUtility.SetDirty(rect);
        return true;
    }

    private static AbilityCooldownIcon CreateOrUpdateIcon(
        RectTransform parent,
        string objectName,
        string iconText,
        string keyText,
        float xPosition,
        Color readyColor,
        ref bool changed)
    {
        Transform existing = parent.Find(objectName);
        RectTransform iconRect;
        if (existing == null)
        {
            GameObject iconObject = CreateUiObject(objectName, parent);
            iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(xPosition, 0f);
            iconRect.sizeDelta = new Vector2(116f, 116f);
            changed = true;
        }
        else
        {
            iconRect = existing as RectTransform;
        }

        if (iconRect == null)
            return null;

        UnityEngine.UI.Image iconImage = iconRect.GetComponent<UnityEngine.UI.Image>();
        if (iconImage == null)
        {
            iconImage = iconRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            iconImage.sprite = GetUiSprite();
            iconImage.type = UnityEngine.UI.Image.Type.Sliced;
            iconImage.color = readyColor;
            iconImage.raycastTarget = false;

            UnityEngine.UI.Outline outline = iconRect.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0.02f, 0.04f, 0.06f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);
            changed = true;
        }

        TextMeshProUGUI symbol = GetOrCreateText(iconRect, "Ability Symbol", ref changed);
        SetStretch(symbol.rectTransform, 8f, 8f, 8f, 8f);
        symbol.text = iconText;
        symbol.fontSize = 24f;
        symbol.fontStyle = FontStyles.Bold;
        symbol.alignment = TextAlignmentOptions.Center;
        symbol.color = Color.white;

        RectTransform overlayRect = GetOrCreateRect(iconRect, "Cooldown Dark Wipe", ref changed);
        SetStretch(overlayRect, 0f, 0f, 0f, 0f);
        UnityEngine.UI.Image overlay = overlayRect.GetComponent<UnityEngine.UI.Image>();
        if (overlay == null)
        {
            overlay = overlayRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            changed = true;
        }
        overlay.sprite = GetUiSprite();
        overlay.type = UnityEngine.UI.Image.Type.Filled;
        overlay.fillMethod = UnityEngine.UI.Image.FillMethod.Vertical;
        overlay.fillOrigin = (int)UnityEngine.UI.Image.OriginVertical.Bottom;
        overlay.fillClockwise = true;
        overlay.fillAmount = 0f;
        overlay.color = new Color(0.07f, 0.07f, 0.08f, 0.84f);
        overlay.raycastTarget = false;

        TextMeshProUGUI timer = GetOrCreateText(iconRect, "Cooldown Seconds", ref changed);
        SetStretch(timer.rectTransform, 6f, 6f, 6f, 6f);
        timer.text = string.Empty;
        timer.fontSize = 27f;
        timer.fontStyle = FontStyles.Bold;
        timer.alignment = TextAlignmentOptions.Center;
        timer.color = Color.white;

        RectTransform badgeRect = GetOrCreateRect(iconRect, "Key Badge", ref changed);
        badgeRect.anchorMin = new Vector2(0f, 1f);
        badgeRect.anchorMax = new Vector2(0f, 1f);
        badgeRect.pivot = new Vector2(0f, 1f);
        badgeRect.anchoredPosition = new Vector2(6f, -6f);
        badgeRect.sizeDelta = new Vector2(31f, 31f);
        UnityEngine.UI.Image badgeImage = badgeRect.GetComponent<UnityEngine.UI.Image>();
        if (badgeImage == null)
        {
            badgeImage = badgeRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            changed = true;
        }
        badgeImage.sprite = GetUiSprite();
        badgeImage.type = UnityEngine.UI.Image.Type.Sliced;
        badgeImage.color = new Color(0.03f, 0.04f, 0.06f, 0.95f);
        badgeImage.raycastTarget = false;

        TextMeshProUGUI key = GetOrCreateText(badgeRect, "Key", ref changed);
        SetStretch(key.rectTransform, 0f, 0f, 0f, 0f);
        key.text = keyText;
        key.fontSize = keyText.Length > 1 ? 11f : 19f;
        key.fontStyle = FontStyles.Bold;
        key.alignment = TextAlignmentOptions.Center;
        key.color = Color.white;

        AbilityCooldownIcon icon = iconRect.GetComponent<AbilityCooldownIcon>();
        if (icon == null)
        {
            icon = iconRect.gameObject.AddComponent<AbilityCooldownIcon>();
            changed = true;
        }

        icon.Configure(iconImage, overlay, timer, iconRect, readyColor);
        EditorUtility.SetDirty(icon);
        return icon;
    }

    private static RectTransform GetOrCreateRect(
        RectTransform parent,
        string objectName,
        ref bool changed)
    {
        Transform existing = parent.Find(objectName);
        if (existing is RectTransform existingRect)
            return existingRect;

        changed = true;
        return CreateUiObject(objectName, parent).GetComponent<RectTransform>();
    }

    private static TextMeshProUGUI GetOrCreateText(
        RectTransform parent,
        string objectName,
        ref bool changed)
    {
        RectTransform rect = GetOrCreateRect(parent, objectName, ref changed);
        TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.raycastTarget = false;
            changed = true;
        }
        return text;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject result = new GameObject(objectName, typeof(RectTransform));
        result.layer = LayerMask.NameToLayer("UI");
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void SetStretch(
        RectTransform rect,
        float left,
        float right,
        float top,
        float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.localScale = Vector3.one;
    }

    private static Sprite GetUiSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T result = root.GetComponentInChildren<T>(true);
            if (result != null)
                return result;
        }
        return null;
    }
}
#endif
