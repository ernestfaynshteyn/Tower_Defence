using UnityEngine;

/// <summary>
/// Applies the generated slime UI kit to the gameplay HUD and existing UI.
/// The source image remains one sheet so it can be replaced later without
/// changing any scene references.
/// </summary>
public class SlimeUiSkin : MonoBehaviour
{
    private const string UiKitResourcePath = "UI/SlimeUI/slime-ui-kit-source";

    private Sprite panelSprite;
    private Sprite buttonSprite;
    private Sprite barSprite;

    private void Start()
    {
        Texture2D kitTexture = Resources.Load<Texture2D>(UiKitResourcePath);
        if (kitTexture == null)
        {
            Debug.LogWarning("Slime UI kit could not be loaded from Resources.", this);
            return;
        }

        CreateSprites(kitTexture);
        ApplyHudSkin();
        ApplyButtonSkin();
        ApplySkillTreeSkin();
    }

    private void CreateSprites(Texture2D texture)
    {
        // Pixel coordinates are based on the generated 1536 x 1024 source sheet.
        panelSprite = Sprite.Create(texture, new Rect(35f, 560f, 1465f, 440f), new Vector2(0.5f, 0.5f), 100f);
        buttonSprite = Sprite.Create(texture, new Rect(330f, 245f, 880f, 300f), new Vector2(0.5f, 0.5f), 100f);
        barSprite = Sprite.Create(texture, new Rect(50f, 35f, 1400f, 190f), new Vector2(0.5f, 0.5f), 100f);
    }

    private void ApplyHudSkin()
    {
        foreach (UnityEngine.UI.Slider slider in FindObjectsByType<UnityEngine.UI.Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Transform background = slider.transform.Find("Background");
            UnityEngine.UI.Image backgroundImage = background != null
                ? background.GetComponent<UnityEngine.UI.Image>()
                : null;

            if (backgroundImage == null)
                continue;

            backgroundImage.sprite = barSprite;
            backgroundImage.type = UnityEngine.UI.Image.Type.Simple;
            backgroundImage.preserveAspect = false;
            backgroundImage.color = Color.white;
        }
    }

    private void ApplyButtonSkin()
    {
        foreach (UnityEngine.UI.Button button in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            UnityEngine.UI.Image image = button.GetComponent<UnityEngine.UI.Image>();
            if (image == null)
                continue;

            image.sprite = buttonSprite;
            image.type = UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
        }
    }

    private void ApplySkillTreeSkin()
    {
        GameObject skillTree = GameObject.Find("Skill Tree UI");
        if (skillTree == null)
            return;

        UnityEngine.UI.Image panelImage = skillTree.GetComponentInChildren<UnityEngine.UI.Image>(true);
        if (panelImage == null)
            return;

        panelImage.sprite = panelSprite;
        panelImage.type = UnityEngine.UI.Image.Type.Simple;
        panelImage.preserveAspect = false;
        panelImage.color = Color.white;
    }
}
