using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Applies the shared pixel frame to standard UI buttons and skill-tree links.
/// Buttons that already use a custom sprite keep their own artwork.
/// </summary>
public static class SlimePixelUiTheme
{
    private const string ButtonSpritePath = "UI/PixelButton";
    private const string ConnectorSpritePath = "UI/PixelConnector";
    private const string SkillNodeSpritePath = "UI/SkillTreeNode";
    private const string SkillCrownSpritePath = "UI/SkillTreeCrown";
    private const string FinaleCrownObjectName = "Finale Crown";

    private static Sprite buttonSprite;
    private static Sprite connectorSprite;
    private static Sprite skillNodeSprite;
    private static Sprite skillCrownSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        ApplyToLoadedScene();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToLoadedScene();
    }

    private static void ApplyToLoadedScene()
    {
        if (buttonSprite == null)
            buttonSprite = Resources.Load<Sprite>(ButtonSpritePath);
        if (connectorSprite == null)
            connectorSprite = Resources.Load<Sprite>(ConnectorSpritePath);
        if (skillNodeSprite == null)
            skillNodeSprite = Resources.Load<Sprite>(SkillNodeSpritePath);
        if (skillCrownSprite == null)
            skillCrownSprite = Resources.Load<Sprite>(SkillCrownSpritePath);

        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
            ApplyButtonSkin(button);

        ApplySkillTreeSkins();

        Image[] images = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Image image in images)
            ApplyConnectorSkin(image);
    }

    private static void ApplySkillTreeSkins()
    {
        if (skillNodeSprite == null)
            return;

        skills[] nodes = Object.FindObjectsByType<skills>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        HashSet<skills> nodesWithChildren = new HashSet<skills>();

        foreach (skills node in nodes)
        {
            if (node == null || node.requirements == null)
                continue;

            foreach (SkillRequirement requirement in node.requirements)
            {
                if (requirement != null && requirement.skill != null)
                    nodesWithChildren.Add(requirement.skill);
            }
        }

        foreach (skills node in nodes)
        {
            if (node == null)
                continue;

            ApplySkillNodeSkin(node);
            bool isBranchEnd = node.requirements != null &&
                               node.requirements.Length > 0 &&
                               !nodesWithChildren.Contains(node);
            ApplyFinaleCrown(node, isBranchEnd);
        }
    }

    private static void ApplySkillNodeSkin(skills node)
    {
        node.lockColor = new Color32(172, 181, 161, 255);
        node.affordableColor = Color.white;
        node.unaffordableColor = new Color32(208, 220, 195, 255);
        node.maxColor = new Color32(255, 235, 160, 255);
        Button button = node.GetComponent<Button>();
        if (button == null || !(button.targetGraphic is Image image))
            return;

        image.sprite = skillNodeSprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 3.125f;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = true;

        foreach (TMPro.TMP_Text label in node.GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            if (label == node.costText || label.gameObject.name == "Price") continue;
            label.color = new Color32(12, 28, 12, 255);
            label.enableAutoSizing = true;
            label.fontSizeMin = 28f;
            label.fontSizeMax = 38f;
            label.raycastTarget = false;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(218, 252, 184, 255);
        colors.pressedColor = new Color32(103, 166, 78, 255);
        colors.selectedColor = new Color32(198, 240, 154, 255);
        colors.disabledColor = new Color32(133, 151, 121, 190);
        button.colors = colors;
    }

    private static void ApplyFinaleCrown(skills node, bool show)
    {
        Transform existing = node.transform.Find(FinaleCrownObjectName);
        if (!show || skillCrownSprite == null)
        {
            if (existing != null)
                existing.gameObject.SetActive(false);
            return;
        }

        Image crown = existing != null ? existing.GetComponent<Image>() : null;
        if (crown == null)
        {
            GameObject crownObject = new GameObject(
                FinaleCrownObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            crownObject.layer = node.gameObject.layer;
            crownObject.transform.SetParent(node.transform, false);
            crown = crownObject.GetComponent<Image>();
        }

        RectTransform crownRect = crown.rectTransform;
        float scaleX = Mathf.Max(0.01f, Mathf.Abs(node.transform.lossyScale.x));
        float scaleY = Mathf.Max(0.01f, Mathf.Abs(node.transform.lossyScale.y));
        crownRect.anchorMin = new Vector2(0.88f, 1f);
        crownRect.anchorMax = new Vector2(0.88f, 1f);
        crownRect.pivot = new Vector2(0.5f, 0f);
        crownRect.anchoredPosition = new Vector2(0f, -1f / scaleY);
        crownRect.sizeDelta = new Vector2(18f / scaleX, 12f / scaleY);
        crownRect.localScale = Vector3.one;
        crownRect.localRotation = Quaternion.identity;

        crown.sprite = skillCrownSprite;
        crown.type = Image.Type.Simple;
        crown.preserveAspect = true;
        crown.color = Color.white;
        crown.raycastTarget = false;
        crown.gameObject.SetActive(true);
        crown.transform.SetAsLastSibling();
    }

    private static void ApplyButtonSkin(Button button)
    {
        if (button == null || !(button.targetGraphic is Image image) || buttonSprite == null)
            return;

        // Keep icons and purpose-made button artwork intact. The default uGUI
        // UISprite is the plain background used by the game's skill buttons.
        if (image.sprite != null && image.sprite.name != "UISprite" &&
            image.sprite.name != "Background" && image.sprite.name != "InputFieldBackground" &&
            image.sprite.name != "PixelButton")
            return;

        image.sprite = buttonSprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 3.125f;
        image.color = Color.white;
        image.raycastTarget = true;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(218, 252, 184, 255);
        colors.pressedColor = new Color32(103, 166, 78, 255);
        colors.selectedColor = new Color32(198, 240, 154, 255);
        colors.disabledColor = new Color32(133, 151, 121, 190);
        button.colors = colors;
    }

    private static void ApplyConnectorSkin(Image image)
    {
        if (image == null || connectorSprite == null)
            return;

        string objectName = image.gameObject.name.ToLowerInvariant();
        if (!objectName.Contains("connector") && !objectName.Contains("connecter"))
            return;

        image.sprite = connectorSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.raycastTarget = false;
    }
}
