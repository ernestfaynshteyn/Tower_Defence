#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-time, Editor-safe scene migration for the active ability branches.
/// The generated nodes remain ordinary editable uGUI objects after creation.
/// </summary>
[InitializeOnLoad]
internal static class AbilitySkillTreeInstaller
{
    private const int InstallerVersion = 8;
    private const string ConnectorPrefix = "Ability Connector - ";
    private const string SewerPulseConnectorPrefix = "Sewer Pulse Connector - ";
    private const float SewerPulseConnectorScale = 0.5f;
    private const float SupportBoundaryPadding = 5f;
    private const float CostLabelGap = 6f;
    private const float CostLabelHeight = 32f;

    // The support-tree red square ends at the white divider on the right.
    // New ability nodes are clamped by their full rendered size, not just their centres.
    private static readonly Rect SupportTreeBounds = Rect.MinMaxRect(-150f, -185f, 375f, 340f);
    private static readonly Rect DefenceTreeBounds = Rect.MinMaxRect(-100f, -180f, 400f, 320f);
    private static readonly Rect AttackTreeBounds = Rect.MinMaxRect(18f, -129f, 540f, 250f);

    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Armory Prototype/Game thing - Armory Prototype.unity",
    };

    private static readonly string[] PulsarRingIds =
    {
        "pizza_circle",
        "pizza_more_damage",
        "pizza_last_longer",
        "pizza_more_damage2",
        "pizza_permanent",
    };

    private static readonly NodePlacement[] NewSupportNodes =
    {
        // Upper open area, above Throwable Mastery.
        new NodePlacement("emp_shockwave", new Vector2(105f, 270f)),
        new NodePlacement("emp_duration", new Vector2(65f, 320f)),
        new NodePlacement("emp_recharge", new Vector2(145f, 320f)),

        // Lower-left open area, between the burn branch and nuke branch.
        new NodePlacement("toxic_flood", new Vector2(-80f, 75f)),
        new NodePlacement("toxic_flood_damage", new Vector2(-115f, 30f)),
        new NodePlacement("toxic_flood_duration", new Vector2(-45f, 30f)),
        new NodePlacement("toxic_flood_recharge", new Vector2(-80f, -15f)),
    };

    private static readonly NodePlacement[] NewAttackNodes =
    {
        // Empty upper-right area of the attack-tree red panel. With the
        // existing 0.3 node scale these positions leave at least 20px between
        // neighbouring buttons and remain clear of the range/multishot nodes.
        new NodePlacement("sewer_pulse", new Vector2(280f, 225f)),
        new NodePlacement("sewer_pulse_damage", new Vector2(360f, 225f)),
        new NodePlacement("sewer_pulse_force", new Vector2(440f, 225f)),
        new NodePlacement("sewer_pulse_radius", new Vector2(320f, 170f)),
        new NodePlacement("sewer_pulse_recharge", new Vector2(400f, 170f)),
    };

    private static readonly ConnectorEdge[] SewerPulseConnectorEdges =
    {
        new ConnectorEdge("more_range", "sewer_pulse"),
        new ConnectorEdge("sewer_pulse", "sewer_pulse_damage"),
        new ConnectorEdge("sewer_pulse_damage", "sewer_pulse_force"),
        new ConnectorEdge("sewer_pulse", "sewer_pulse_radius"),
        new ConnectorEdge("sewer_pulse_radius", "sewer_pulse_recharge"),
    };

    private static readonly NodePlacement[] NewDefenceNodes =
    {
        // Open strip below Greater Defense and left of the Pulsar Ring branch.
        new NodePlacement("defence_regeneration", new Vector2(146f, -150f)),
        new NodePlacement("rapid_defence_regeneration", new Vector2(220f, -150f)),
    };

    private static bool isRunning;

    static AbilitySkillTreeInstaller()
    {
        EditorApplication.delayCall += TryAutomaticInstall;
    }

    [MenuItem("Tools/Tower Defence/Rebuild Ability Skill Nodes")]
    private static void RebuildFromMenu()
    {
        InstallAllScenes(true);
    }

    private static string InstallKey =>
        "TowerDefence.AbilitySkillTreeInstaller." + Application.dataPath + ".v" + InstallerVersion;

    private static void TryAutomaticInstall()
    {
        if (EditorPrefs.GetBool(InstallKey, false) || isRunning)
            return;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryAutomaticInstall;
            return;
        }

        bool allSaved = InstallAllScenes(false);
        if (allSaved)
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
                            $"[AbilitySkillTreeInstaller] Updated '{scene.name}' in memory but left it unsaved " +
                            "because it already contained unsaved work. Save the scene normally when ready.");
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
            Debug.Log("[AbilitySkillTreeInstaller] Ability nodes, text, effects, and requirements are installed.");

        return allSaved;
    }

    private static bool InstallIntoScene(Scene scene)
    {
        SkillTreeScript tree = FindInScene<SkillTreeScript>(scene);
        if (tree == null)
        {
            Debug.LogWarning($"[AbilitySkillTreeInstaller] No SkillTreeScript found in '{scene.name}'.");
            return false;
        }

        Dictionary<string, SkillSeed> seeds = SkillSeedData.BuildSkillDictionary();
        List<skills> nodes = FindAllInScene<skills>(scene);
        bool changed = false;

        foreach (string id in PulsarRingIds)
        {
            skills node = FindNode(nodes, id);
            if (node != null)
                changed |= ApplySeed(node, seeds[id]);
        }

        skills attackRoot = FindNode(nodes, "attacks");
        Transform attackParent = attackRoot != null ? attackRoot.transform.parent : null;
        if (attackRoot == null || attackParent == null)
        {
            Debug.LogWarning($"[AbilitySkillTreeInstaller] Attack tree root not found in '{scene.name}'.");
        }
        else
        {
            foreach (NodePlacement placement in NewAttackNodes)
            {
                skills node = FindNode(nodes, placement.SkillId);
                if (node == null)
                {
                    node = CreateNode(attackRoot, attackParent, placement, seeds[placement.SkillId]);
                    nodes.Add(node);
                    changed = true;
                }
                else
                {
                    changed |= ApplySeed(node, seeds[placement.SkillId]);
                }

                RectTransform rect = node.transform as RectTransform;
                if (rect == null)
                    continue;

                Vector2 safePosition = ClampInsideBounds(rect, placement.Position, AttackTreeBounds);
                if (safePosition != placement.Position)
                {
                    Debug.LogWarning(
                        $"[AbilitySkillTreeInstaller] Clamped '{placement.SkillId}' inside the attack-tree red square.",
                        node);
                }

                if (rect.anchoredPosition != safePosition)
                {
                    rect.anchoredPosition = safePosition;
                    changed = true;
                }
            }
        }

        skills supportRoot = FindNode(nodes, "support");
        if (supportRoot == null || supportRoot.transform.parent == null)
        {
            Debug.LogWarning($"[AbilitySkillTreeInstaller] Support tree root not found in '{scene.name}'.");
            return changed;
        }

        Transform supportParent = supportRoot.transform.parent;
        foreach (NodePlacement placement in NewSupportNodes)
        {
            skills node = FindNode(nodes, placement.SkillId);
            if (node == null)
            {
                node = CreateNode(supportRoot, supportParent, placement, seeds[placement.SkillId]);
                nodes.Add(node);
                changed = true;
            }
            else
            {
                changed |= ApplySeed(node, seeds[placement.SkillId]);
            }

            RectTransform rect = node.transform as RectTransform;
            if (rect != null)
            {
                Vector2 safePosition = ClampInsideBounds(rect, placement.Position, SupportTreeBounds);
                if (safePosition != placement.Position)
                {
                    Debug.LogWarning(
                        $"[AbilitySkillTreeInstaller] Clamped '{placement.SkillId}' inside the support-tree red square.",
                        node);
                }

                if (rect.anchoredPosition != safePosition)
                {
                    rect.anchoredPosition = safePosition;
                    changed = true;
                }
            }
        }

        skills defenceRoot = FindNode(nodes, "defense");
        Transform defenceParent = defenceRoot != null ? defenceRoot.transform.parent : null;
        if (defenceRoot == null || defenceParent == null)
        {
            Debug.LogWarning($"[AbilitySkillTreeInstaller] Defense tree root not found in '{scene.name}'.");
        }
        else
        {
            foreach (NodePlacement placement in NewDefenceNodes)
            {
                skills node = FindNode(nodes, placement.SkillId);
                if (node == null)
                {
                    node = CreateNode(defenceRoot, defenceParent, placement, seeds[placement.SkillId]);
                    nodes.Add(node);
                    changed = true;
                }
                else
                {
                    changed |= ApplySeed(node, seeds[placement.SkillId]);
                }

                RectTransform rect = node.transform as RectTransform;
                if (rect == null)
                    continue;

                Vector2 safePosition = ClampInsideBounds(rect, placement.Position, DefenceTreeBounds);
                if (safePosition != placement.Position)
                {
                    Debug.LogWarning(
                        $"[AbilitySkillTreeInstaller] Clamped '{placement.SkillId}' inside the defense-tree red square.",
                        node);
                }

                if (rect.anchoredPosition != safePosition)
                {
                    rect.anchoredPosition = safePosition;
                    changed = true;
                }
            }
        }

        foreach (string id in PulsarRingIds)
        {
            skills node = FindNode(nodes, id);
            if (node != null)
                changed |= WireRequirements(node, seeds[id], nodes);
        }

        foreach (NodePlacement placement in NewAttackNodes)
        {
            skills node = FindNode(nodes, placement.SkillId);
            if (node != null)
                changed |= WireRequirements(node, seeds[placement.SkillId], nodes);
        }

        foreach (NodePlacement placement in NewSupportNodes)
        {
            skills node = FindNode(nodes, placement.SkillId);
            if (node != null)
                changed |= WireRequirements(node, seeds[placement.SkillId], nodes);
        }

        foreach (NodePlacement placement in NewDefenceNodes)
        {
            skills node = FindNode(nodes, placement.SkillId);
            if (node != null)
                changed |= WireRequirements(node, seeds[placement.SkillId], nodes);
        }

        // Connector artwork is already authored in both scenes. Older versions
        // of this installer generated overlapping black lines whenever the
        // migration version changed, so remove only those generated extras and
        // leave all existing scene-authored connectors untouched.
        changed |= RemoveGeneratedConnectors(supportParent);
        if (attackParent != null)
        {
            changed |= RemoveGeneratedConnectors(attackParent);
            changed |= CreateOrUpdateSewerPulseConnectors(attackParent, nodes);
        }
        if (defenceParent != null)
            changed |= RemoveGeneratedConnectors(defenceParent);

        foreach (skills node in nodes)
        {
            if (node != null)
            {
                Rect? treeBounds = GetTreeBounds(
                    node.transform,
                    attackParent,
                    supportParent,
                    defenceParent);
                changed |= EnsureCostLabel(node, treeBounds);
            }

            if (node != null && !tree.nodes.Contains(node))
            {
                tree.nodes.Add(node);
                changed = true;
            }
        }

        tree.collectChildrenOnAwake = true;

        PulsarRing pulsarRing = FindInScene<PulsarRing>(scene);
        if (pulsarRing != null && pulsarRing.GetComponent<TowerAbilityController>() == null)
        {
            pulsarRing.gameObject.AddComponent<TowerAbilityController>();
            changed = true;
        }

        if (changed)
            EditorUtility.SetDirty(tree);

        return changed;
    }

    private static bool EnsureCostLabel(skills node, Rect? treeBounds)
    {
        if (node == null || !(node.transform is RectTransform nodeRect))
            return false;

        bool placeAbove = CanPlaceCostAbove(nodeRect, treeBounds);
        bool changed = node.EnsureCostLabel(placeAbove);

        if (changed)
        {
            if (node.costText != null)
                EditorUtility.SetDirty(node.costText);
            EditorUtility.SetDirty(node);
        }

        return changed;
    }

    private static bool CanPlaceCostAbove(RectTransform nodeRect, Rect? treeBounds)
    {
        if (!treeBounds.HasValue)
            return true;

        float scaleY = Mathf.Abs(nodeRect.localScale.y);
        float labelTop = nodeRect.anchoredPosition.y +
                         (nodeRect.rect.yMax + CostLabelGap + CostLabelHeight) * scaleY;
        return labelTop <= treeBounds.Value.yMax - SupportBoundaryPadding;
    }

    private static Rect? GetTreeBounds(
        Transform node,
        Transform attackParent,
        Transform supportParent,
        Transform defenceParent)
    {
        if (attackParent != null && (node == attackParent || node.IsChildOf(attackParent)))
            return AttackTreeBounds;
        if (supportParent != null && (node == supportParent || node.IsChildOf(supportParent)))
            return SupportTreeBounds;
        if (defenceParent != null && (node == defenceParent || node.IsChildOf(defenceParent)))
            return DefenceTreeBounds;
        return null;
    }

    private static skills CreateNode(
        skills template,
        Transform parent,
        NodePlacement placement,
        SkillSeed seed)
    {
        GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
        clone.name = "Ability - " + seed.displayName;

        RectTransform rect = clone.transform as RectTransform;
        if (rect != null)
            rect.anchoredPosition = placement.Position;

        skills node = clone.GetComponent<skills>();
        ApplySeed(node, seed);
        EditorUtility.SetDirty(clone);
        return node;
    }

    private static bool ApplySeed(skills node, SkillSeed seed)
    {
        bool changed = node.skillId != seed.id ||
                       node.displayName != seed.displayName ||
                       node.description != seed.description ||
                       node.maxLevel != seed.maxLevel ||
                       node.baseCost != seed.baseCost ||
                       node.costAddPerLevel != seed.costAddPerLevel ||
                       !Mathf.Approximately(node.costMultiplierPerLevel, seed.costMultiplierPerLevel);

        node.skillId = seed.id;
        node.displayName = seed.displayName;
        node.description = seed.description;
        node.maxLevel = seed.maxLevel;
        node.baseCost = seed.baseCost;
        node.costAddPerLevel = seed.costAddPerLevel;
        node.costMultiplierPerLevel = seed.costMultiplierPerLevel;
        node.level = Mathf.Clamp(node.level, 0, node.maxLevel);
        node.effects = CloneEffects(seed.effects);
        node.autoWireButton = true;

        UnityEngine.UI.Button button = node.GetComponent<UnityEngine.UI.Button>();
        if (button != null && button.onClick.GetPersistentEventCount() > 0)
        {
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            changed = true;
        }

        TMP_Text[] labels = node.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text label in labels)
        {
            string labelName = label.gameObject.name.Trim().ToLowerInvariant();
            if (labelName.Contains("title"))
            {
                if (label.text != seed.displayName)
                    changed = true;
                label.text = seed.displayName;
            }
            else if (labelName.Contains("desc"))
            {
                if (label.text != seed.description)
                    changed = true;
                label.text = seed.description;
            }
        }

        EditorUtility.SetDirty(node);
        return changed;
    }

    private static SkillEffect[] CloneEffects(SkillEffect[] effects)
    {
        if (effects == null)
            return Array.Empty<SkillEffect>();

        SkillEffect[] clones = new SkillEffect[effects.Length];
        for (int i = 0; i < effects.Length; i++)
        {
            SkillEffect effect = effects[i];
            clones[i] = new SkillEffect
            {
                statName = effect.statName,
                modifierType = effect.modifierType,
                value = effect.value,
                perLevel = effect.perLevel,
            };
        }
        return clones;
    }

    private static bool WireRequirements(skills node, SkillSeed seed, List<skills> nodes)
    {
        List<SkillRequirement> requirements = new List<SkillRequirement>();
        for (int i = 0; i < seed.requirementIds.Length; i++)
        {
            string requirementId = seed.requirementIds[i];
            skills requiredNode = FindNode(nodes, requirementId);
            if (requiredNode == null)
            {
                Debug.LogError(
                    $"[AbilitySkillTreeInstaller] '{seed.id}' could not find requirement '{requirementId}'.",
                    node);
                continue;
            }

            int requiredLevel = seed.requirementLevels != null && i < seed.requirementLevels.Length
                ? Mathf.Max(1, seed.requirementLevels[i])
                : 1;
            requirements.Add(new SkillRequirement { skill = requiredNode, requiredLevel = requiredLevel });
        }

        bool changed = !RequirementsMatch(node.requirements, requirements);
        node.requirements = requirements.ToArray();
        EditorUtility.SetDirty(node);
        return changed;
    }

    private static bool RequirementsMatch(SkillRequirement[] current, List<SkillRequirement> expected)
    {
        if (current == null || current.Length != expected.Count)
            return false;

        for (int i = 0; i < current.Length; i++)
        {
            if (current[i] == null || current[i].skill != expected[i].skill ||
                current[i].requiredLevel != expected[i].requiredLevel)
            {
                return false;
            }
        }
        return true;
    }

    private static bool RemoveGeneratedConnectors(Transform parent)
    {
        bool changed = false;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name.StartsWith(ConnectorPrefix, StringComparison.Ordinal))
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
                changed = true;
            }
        }

        return changed;
    }

    private static bool CreateOrUpdateSewerPulseConnectors(
        Transform attackNodeParent,
        List<skills> nodes)
    {
        RectTransform connectorContainer = FindAttackConnectorContainer(attackNodeParent);
        if (connectorContainer == null)
        {
            Debug.LogWarning("[AbilitySkillTreeInstaller] Attack-tree 'connecters' container was not found.");
            return false;
        }

        RectTransform template = null;
        for (int i = 0; i < connectorContainer.childCount; i++)
        {
            Transform child = connectorContainer.GetChild(i);
            if (!child.name.StartsWith(SewerPulseConnectorPrefix, StringComparison.Ordinal) &&
                child is RectTransform childRect &&
                child.GetComponent<Image>() != null)
            {
                template = childRect;
                break;
            }
        }

        if (template == null)
        {
            Debug.LogWarning("[AbilitySkillTreeInstaller] No attack-tree connector template was found.");
            return false;
        }

        bool changed = false;
        foreach (ConnectorEdge edge in SewerPulseConnectorEdges)
        {
            skills from = FindNode(nodes, edge.FromSkillId);
            skills to = FindNode(nodes, edge.ToSkillId);
            if (from == null || to == null)
                continue;

            string connectorName = SewerPulseConnectorPrefix + edge.FromSkillId + " to " + edge.ToSkillId;
            RectTransform connector = null;

            for (int i = connectorContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = connectorContainer.GetChild(i);
                if (!string.Equals(child.name, connectorName, StringComparison.Ordinal))
                    continue;

                if (connector == null)
                {
                    connector = child as RectTransform;
                }
                else
                {
                    // Keep exactly one line for each edge if an older migration
                    // was interrupted or manually run more than once.
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                    changed = true;
                }
            }

            if (connector == null)
            {
                GameObject clone = UnityEngine.Object.Instantiate(
                    template.gameObject,
                    connectorContainer,
                    false);
                clone.name = connectorName;
                connector = clone.GetComponent<RectTransform>();
                changed = true;
            }

            RectTransform fromRect = from.transform as RectTransform;
            RectTransform toRect = to.transform as RectTransform;
            if (connector == null || fromRect == null || toRect == null)
                continue;

            Vector2 start = connectorContainer.InverseTransformPoint(
                fromRect.TransformPoint(fromRect.rect.center));
            Vector2 end = connectorContainer.InverseTransformPoint(
                toRect.TransformPoint(toRect.rect.center));
            Vector2 delta = end - start;
            Vector2 position = (start + end) * 0.5f;
            Vector2 size = new Vector2(delta.magnitude, 25f);
            Quaternion rotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            Vector3 scale = Vector3.one * SewerPulseConnectorScale;

            if (connector.anchorMin != new Vector2(0.5f, 0.5f) ||
                connector.anchorMax != new Vector2(0.5f, 0.5f) ||
                connector.pivot != new Vector2(0.5f, 0.5f) ||
                connector.anchoredPosition != position ||
                connector.sizeDelta != size ||
                connector.localRotation != rotation ||
                connector.localScale != scale)
            {
                connector.anchorMin = new Vector2(0.5f, 0.5f);
                connector.anchorMax = new Vector2(0.5f, 0.5f);
                connector.pivot = new Vector2(0.5f, 0.5f);
                connector.anchoredPosition = position;
                connector.sizeDelta = size;
                connector.localRotation = rotation;
                connector.localScale = scale;
                EditorUtility.SetDirty(connector);
                changed = true;
            }

            Image image = connector.GetComponent<Image>();
            if (image != null && image.color != Color.black)
            {
                image.color = Color.black;
                EditorUtility.SetDirty(image);
                changed = true;
            }
        }

        return changed;
    }

    private static RectTransform FindAttackConnectorContainer(Transform attackNodeParent)
    {
        Transform attackTree = attackNodeParent.parent;
        if (attackTree == null)
            return null;

        for (int i = 0; i < attackTree.childCount; i++)
        {
            Transform child = attackTree.GetChild(i);
            if (string.Equals(child.name, "connecters", StringComparison.OrdinalIgnoreCase))
                return child as RectTransform;
        }

        return null;
    }

    private static Vector2 ClampInsideBounds(
        RectTransform rect,
        Vector2 requestedPosition,
        Rect bounds)
    {
        float halfWidth = rect.rect.width * Mathf.Abs(rect.localScale.x) * 0.5f;
        float halfHeight = rect.rect.height * Mathf.Abs(rect.localScale.y) * 0.5f;

        float minX = bounds.xMin + halfWidth + SupportBoundaryPadding;
        float maxX = bounds.xMax - halfWidth - SupportBoundaryPadding;
        float minY = bounds.yMin + halfHeight + SupportBoundaryPadding;
        float maxY = bounds.yMax - halfHeight - SupportBoundaryPadding;

        return new Vector2(
            Mathf.Clamp(requestedPosition.x, minX, maxX),
            Mathf.Clamp(requestedPosition.y, minY, maxY));
    }

    private static skills FindNode(List<skills> nodes, string currentId)
    {
        string resolvedWanted = SkillSeedData.ResolveSkillId(currentId);
        return nodes.FirstOrDefault(node =>
            node != null &&
            string.Equals(
                SkillSeedData.ResolveSkillId(node.skillId),
                resolvedWanted,
                StringComparison.OrdinalIgnoreCase));
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

    private static List<T> FindAllInScene<T>(Scene scene) where T : Component
    {
        List<T> results = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects())
            results.AddRange(root.GetComponentsInChildren<T>(true));
        return results;
    }

    private readonly struct NodePlacement
    {
        public NodePlacement(string skillId, Vector2 position)
        {
            SkillId = skillId;
            Position = position;
        }

        public string SkillId { get; }
        public Vector2 Position { get; }
    }

    private readonly struct ConnectorEdge
    {
        public ConnectorEdge(string fromSkillId, string toSkillId)
        {
            FromSkillId = fromSkillId;
            ToSkillId = toSkillId;
        }

        public string FromSkillId { get; }
        public string ToSkillId { get; }
    }
}
#endif
