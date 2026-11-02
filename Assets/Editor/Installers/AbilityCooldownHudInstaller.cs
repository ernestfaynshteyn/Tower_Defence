#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds a scene-authored cooldown HUD to the prototype gameplay scene. Everything it
/// creates remains ordinary editable uGUI content in the scene hierarchy.
/// </summary>
[InitializeOnLoad]
internal static class AbilityCooldownHudInstaller
{
    private const int InstallerVersion = 11;
    private const string HudName = "Ability Cooldown HUD";
    private const string ThrowableHudName = "throwable visuals";
    private const string ThrowablePanelSpritePath = "Assets/UI/HUD/SlimeThrowableTrayPanel.png";
    private const float IconSize = 116f;
    private const float IconSpacing = 12f;

    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Game thing - Armory Prototype.unity",
    };

    private static bool isRunning;

    static AbilityCooldownHudInstaller()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        EditorApplication.delayCall += RefreshLoadedThrowableTrays;
        EditorApplication.delayCall += TryAutomaticInstall;
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += RefreshLoadedThrowableTrays;
            EditorApplication.delayCall += TryAutomaticInstall;
        }
    }

    [MenuItem("Tools/Tower Defence/Rebuild Ability Cooldown HUD")]
    private static void RebuildFromMenu()
    {
        InstallAllScenes(true);
    }

    [MenuItem("Tools/Tower Defence/Refresh Throwable HUD")]
    private static void RefreshThrowableHudFromMenu()
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
            Debug.Log("[AbilityCooldownHudInstaller] Editable ability cooldown icons are installed.");

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
        changed |= ConfigureThrowableTray(canvas.transform);
        bool hasPrototypeAttachments = FindInScene<PrototypeArmoryBootstrap>(scene) != null;
        int iconCount = hasPrototypeAttachments ? 7 : 5;
        Vector2 desiredSize = new Vector2(
            IconSize,
            iconCount * IconSize + (iconCount - 1) * IconSpacing);
        Transform existing = canvas.transform.Find(HudName);
        RectTransform hudRect;
        if (existing == null)
        {
            GameObject hudObject = CreateUiObject(HudName, canvas.transform);
            hudRect = hudObject.GetComponent<RectTransform>();
            hudRect.sizeDelta = desiredSize;
            SetBottomLeftPosition(hudRect);
            changed = true;
        }
        else
        {
            hudRect = existing as RectTransform;
        }

        if (hudRect == null)
            return changed;

        if (hudRect.sizeDelta != desiredSize)
        {
            hudRect.sizeDelta = desiredSize;
            changed = true;
        }

        changed |= ConfigureVerticalLayout(hudRect);

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
            new Color(0.12f, 0.72f, 0.9f, 1f),
            ref changed);
        AbilityCooldownIcon emp = CreateOrUpdateIcon(
            hudRect,
            "EMP Cooldown",
            "EMP",
            "X",
            new Color(0.32f, 0.52f, 1f, 1f),
            ref changed);
        AbilityCooldownIcon toxic = CreateOrUpdateIcon(
            hudRect,
            "Toxic Flood Cooldown",
            "TOXIC",
            "C",
            new Color(0.28f, 0.78f, 0.25f, 1f),
            ref changed);
        AbilityCooldownIcon nuke = CreateOrUpdateIcon(
            hudRect,
            "Nuke Cooldown",
            "NUKE",
            "N",
            new Color(0.95f, 0.35f, 0.12f, 1f),
            ref changed);
        AbilityCooldownIcon pulsar = CreateOrUpdateIcon(
            hudRect,
            "Pulsar Ring Cooldown",
            "PULSAR",
            "AUTO",
            new Color(0.72f, 0.3f, 0.95f, 1f),
            ref changed);

        changed |= SetIconLayout(sewerPulse, 0);
        changed |= SetIconLayout(emp, 1);
        changed |= SetIconLayout(toxic, 2);
        changed |= SetIconLayout(nuke, 3);
        changed |= SetIconLayout(pulsar, 4);

        AbilityCooldownIcon overcharge = null;
        AbilityCooldownIcon freezeBurst = null;
        if (hasPrototypeAttachments)
        {
            overcharge = CreateOrUpdateIcon(
                hudRect,
                "Overcharge Cooldown",
                "OVER",
                "R",
                new Color(1f, 0.45f, 0.08f, 1f),
                ref changed);
            freezeBurst = CreateOrUpdateIcon(
                hudRect,
                "Freeze Burst Cooldown",
                "FREEZE",
                "AUTO",
                new Color(0.25f, 0.85f, 1f, 1f),
                ref changed);
            changed |= SetIconLayout(overcharge, 5);
            changed |= SetIconLayout(freezeBurst, 6);
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
            pulsar,
            null,
            overcharge,
            freezeBurst);
        EditorUtility.SetDirty(hud);
        return true;
    }

    private static bool ConfigureThrowableTray(Transform canvasTransform)
    {
        RectTransform tray = canvasTransform.Find(ThrowableHudName) as RectTransform;
        if (tray == null)
            return false;

        bool trayChanged = false;
        trayChanged |= SetIfDifferent(tray.anchorMin, Vector2.zero, value => tray.anchorMin = value);
        trayChanged |= SetIfDifferent(tray.anchorMax, Vector2.zero, value => tray.anchorMax = value);
        trayChanged |= SetIfDifferent(tray.pivot, Vector2.zero, value => tray.pivot = value);
        trayChanged |= SetIfDifferent(tray.anchoredPosition, new Vector2(196f, 36f),
            value => tray.anchoredPosition = value);
        trayChanged |= SetIfDifferent(tray.sizeDelta, new Vector2(1325f, 241f),
            value => tray.sizeDelta = value);
        if (trayChanged)
            EditorUtility.SetDirty(tray);

        RectTransform table = tray.Find("table") as RectTransform;
        if (table == null)
            return trayChanged;

        bool tableChanged = false;
        tableChanged |= SetIfDifferent(table.localScale, new Vector3(0.82f, 0.82f, 1f),
            value => table.localScale = value);
        tableChanged |= SetIfDifferent(table.anchoredPosition, Vector2.zero,
            value => table.anchoredPosition = value);
        if (tableChanged)
            EditorUtility.SetDirty(table);

        UnityEngine.UI.Image background = table.GetComponent<UnityEngine.UI.Image>();
        bool backgroundChanged = false;
        if (background != null)
        {
            Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ThrowablePanelSpritePath);
            if (!background.enabled)
            {
                background.enabled = true;
                backgroundChanged = true;
            }
            if (panelSprite != null && background.sprite != panelSprite)
            {
                background.sprite = panelSprite;
                backgroundChanged = true;
            }
            if (background.type != UnityEngine.UI.Image.Type.Sliced)
            {
                background.type = UnityEngine.UI.Image.Type.Sliced;
                backgroundChanged = true;
            }
            if (background.color != Color.white)
            {
                background.color = Color.white;
                backgroundChanged = true;
            }
            if (background.raycastTarget)
            {
                background.raycastTarget = false;
                backgroundChanged = true;
            }
            if (backgroundChanged)
                EditorUtility.SetDirty(background);
        }

        return trayChanged || tableChanged || backgroundChanged;
    }

    private static void RefreshLoadedThrowableTrays()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            Canvas canvas = FindInScene<Canvas>(scene);
            if (canvas != null && ConfigureThrowableTray(canvas.transform))
                EditorSceneManager.MarkSceneDirty(scene);
        }
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

    private static bool ConfigureVerticalLayout(RectTransform hudRect)
    {
        bool changed = false;
        UnityEngine.UI.HorizontalLayoutGroup horizontal =
            hudRect.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        if (horizontal != null)
        {
            UnityEngine.Object.DestroyImmediate(horizontal);
            changed = true;
        }

        UnityEngine.UI.VerticalLayoutGroup layout =
            hudRect.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = hudRect.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            changed = true;
        }

        if (layout.padding.left != 0 || layout.padding.right != 0 ||
            layout.padding.top != 0 || layout.padding.bottom != 0)
        {
            layout.padding = new RectOffset();
            changed = true;
        }

        changed |= SetIfDifferent(layout.spacing, IconSpacing, value => layout.spacing = value);
        if (layout.childAlignment != TextAnchor.LowerLeft)
        {
            layout.childAlignment = TextAnchor.LowerLeft;
            changed = true;
        }
        changed |= SetIfDifferent(layout.childControlWidth, false, value => layout.childControlWidth = value);
        changed |= SetIfDifferent(layout.childControlHeight, false, value => layout.childControlHeight = value);
        changed |= SetIfDifferent(layout.childForceExpandWidth, false, value => layout.childForceExpandWidth = value);
        changed |= SetIfDifferent(layout.childForceExpandHeight, false, value => layout.childForceExpandHeight = value);
        changed |= SetIfDifferent(layout.reverseArrangement, true, value => layout.reverseArrangement = value);

        if (changed)
            EditorUtility.SetDirty(layout);
        return changed;
    }

    private static bool SetIfDifferent<T>(T current, T desired, Action<T> setter)
    {
        if (Equals(current, desired))
            return false;

        setter(desired);
        return true;
    }

    private static bool SetIconLayout(AbilityCooldownIcon icon, int siblingIndex)
    {
        if (icon == null || !(icon.transform is RectTransform rect))
            return false;

        bool changed = false;
        if (rect.GetSiblingIndex() != siblingIndex)
        {
            rect.SetSiblingIndex(siblingIndex);
            changed = true;
        }
        if (rect.sizeDelta != new Vector2(IconSize, IconSize))
        {
            rect.sizeDelta = new Vector2(IconSize, IconSize);
            changed = true;
        }

        if (changed)
            EditorUtility.SetDirty(rect);
        return changed;
    }

    private static AbilityCooldownIcon CreateOrUpdateIcon(
        RectTransform parent,
        string objectName,
        string iconText,
        string keyText,
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
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(IconSize, IconSize);
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

        int missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(iconRect.gameObject);
        if (missingScriptCount > 0)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(iconRect.gameObject);
            changed = true;
        }

        AbilityCooldownIcon[] existingIcons = iconRect.GetComponents<AbilityCooldownIcon>();
        for (int i = 1; i < existingIcons.Length; i++)
        {
            UnityEngine.Object.DestroyImmediate(existingIcons[i]);
            changed = true;
        }

        AbilityCooldownIcon icon = existingIcons.Length > 0 ? existingIcons[0] : null;
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

/// <summary>
/// Materializes the prototype Armory as normal scene-authored uGUI so its
/// hierarchy, text, layout groups, and buttons are editable outside Play Mode.
/// Kept with the other scene UI migrations so existing projects discover it
/// even when Unity's automatic import of newly added Editor files is disabled.
/// </summary>
[InitializeOnLoad]
internal static class SceneAuthoredArmoryPanelInstaller
{
    private const int InstallerVersion = 7;
    private const string ScenePath = "Assets/Scenes/Game thing - Armory Prototype.unity";
    private static bool isRunning;

    static SceneAuthoredArmoryPanelInstaller()
    {
        EditorApplication.delayCall += TryAutomaticInstall;
    }

    [MenuItem("Tools/Tower Defence/Create Missing Armory Panel")]
    private static void InstallFromMenu()
    {
        Install(true);
    }

    [MenuItem("Tools/Tower Defence/Refresh Armory and Shop UI")]
    private static void RefreshShopUiFromMenu()
    {
        Install(true);
    }

    private static string InstallKey =>
        "TowerDefence.SceneAuthoredArmoryPanelInstaller." + Application.dataPath + ".v" + InstallerVersion;

    private static void TryAutomaticInstall()
    {
        if (EditorPrefs.GetBool(InstallKey, false) || isRunning)
            return;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryAutomaticInstall;
            return;
        }

        if (Install(false))
            EditorPrefs.SetBool(InstallKey, true);
    }

    private static bool Install(bool forceSaveLoadedScene)
    {
        if (isRunning || !System.IO.File.Exists(ScenePath))
            return false;

        isRunning = true;
        bool completed = false;

        try
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            bool wasDirty = wasLoaded && scene.isDirty;

            if (!wasLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            PrototypeArmoryBootstrap bootstrap = FindInScene<PrototypeArmoryBootstrap>(scene);
            if (bootstrap == null)
            {
                Debug.LogError("[ArmoryPanelInstaller] PrototypeArmoryBootstrap was not found in the prototype gameplay scene.");
            }
            else
            {
                bool changed = bootstrap.EnsureSceneAuthoredArmoryPanel();
                if (changed)
                {
                    EditorUtility.SetDirty(bootstrap);
                    EditorSceneManager.MarkSceneDirty(scene);

                    bool maySave = !wasLoaded || !wasDirty || forceSaveLoadedScene;
                    if (maySave)
                    {
                        completed = EditorSceneManager.SaveScene(scene);
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[ArmoryPanelInstaller] Created the editable Armory Panel in memory, but the scene " +
                            "already contained unsaved work. Save the scene normally to keep it.");
                    }
                }
                else
                {
                    completed = true;
                }
            }

            if (!wasLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            isRunning = false;
        }

        if (completed)
            Debug.Log("[ArmoryPanelInstaller] Editable Armory Panel is installed beside SkillTree and Shop.");

        return completed;
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
