using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class PrototypeLoadoutSelection : MonoBehaviour
{
    [Header("Existing weapon buttons")]
    [SerializeField] private UnityEngine.UI.Button rapidBulletsButton;
    [SerializeField] private UnityEngine.UI.Button piercingLaserButton;
    [SerializeField] private UnityEngine.UI.Button shotgunBurstButton;
    [SerializeField] private UnityEngine.UI.Button flamethrowerButton;
    [SerializeField] private UnityEngine.UI.Button lightningArcButton;
    [SerializeField] private UnityEngine.UI.Button toxicSpitterButton;
    [SerializeField] private UnityEngine.UI.Button startButton;

    private readonly List<WeaponChoice> choices = new List<WeaponChoice>();
    private GlobalData globalData;

    private sealed class WeaponChoice
    {
        public Weapon weapon;
        public string displayName;
        public string description;
        public UnityEngine.UI.Button button;
        public TextMeshProUGUI label;
        public GameObject iconPrefab;
    }

    private void Start()
    {
        globalData = GlobalData.Instance != null ? GlobalData.Instance : FindFirstObjectByType<GlobalData>();
        if (globalData == null || rapidBulletsButton == null || piercingLaserButton == null ||
            shotgunBurstButton == null || flamethrowerButton == null || lightningArcButton == null ||
            toxicSpitterButton == null || startButton == null)
        {
            Debug.LogError("The Armory Prototype loadout screen is missing a required reference.", this);
            enabled = false;
            return;
        }

        AddChoice(Weapon.gun1, "Rapid Bullets", "High fire rate, low damage", rapidBulletsButton);
        // Keep the prototype laser separate from the legacy M4/gun3 selection.
        // This prevents WeaponSelected from showing that prefab for the laser.
        AddChoice(Weapon.piercingLaser, "Piercing Laser", "Hold to fire; pierces multiple slimes", piercingLaserButton);
        AddChoice(Weapon.gun2, "Shotgun Burst", "Wide spread, strong up close", shotgunBurstButton);
        AddChoice(Weapon.flamethrower, "Flamethrower", "Continuous burning damage", flamethrowerButton);
        AddChoice(Weapon.lightningArc, "Lightning Arc", "Jumps between nearby slimes", lightningArcButton);
        AddChoice(Weapon.toxicSpitter, "Toxic Spitter", "Poison bombs with splash damage", toxicSpitterButton);

        // Older copies stored the prototype laser as gun3, which also enabled
        // the legacy M4 prefab. Migrate that selection to the dedicated value.
        if (globalData.selectedWeapon == Weapon.gun3)
            globalData.selectedWeapon = Weapon.piercingLaser;

        foreach (WeaponChoice choice in choices)
            ConfigureChoice(choice);

        startButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        startButton.onClick.AddListener(StartPrototypeGame);
        SelectWeapon(globalData.selectedWeapon);
    }

    private void AddChoice(Weapon weapon, string displayName, string description, UnityEngine.UI.Button button)
    {
        choices.Add(new WeaponChoice
        {
            weapon = weapon,
            displayName = displayName,
            description = description,
            button = button
        });
    }

    private void ConfigureChoice(WeaponChoice choice)
    {
        choice.button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();

        Weapon captured = choice.weapon;
        choice.button.onClick.AddListener(() => SelectWeapon(captured));
        choice.label = choice.button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (choice.label != null)
        {
            RectTransform labelRect = choice.label.rectTransform;
            labelRect.anchorMin = new Vector2(0.06f, 0.06f);
            labelRect.anchorMax = new Vector2(0.94f, 0.43f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelRect.localScale = Vector3.one;
            choice.label.fontSize = 50f;
            choice.label.enableAutoSizing = true;
            choice.label.fontSizeMin = 38f;
            choice.label.fontSizeMax = 50f;
            choice.label.alignment = TextAlignmentOptions.Center;
            choice.label.color = new Color32(15, 40, 16, 255);
            choice.label.raycastTarget = false;
        }
        if (choice.label == null)
            Debug.LogError(choice.displayName + " is missing its editable TextMeshPro label.", choice.button);
        choice.iconPrefab = LoadWeaponIconPrefab(choice.weapon);
        if (choice.iconPrefab == null)
            Debug.LogError("Missing weapon icon prefab for " + choice.displayName + ".", choice.button);
        AddWeaponIconToButton(choice);
        UpdateChoiceVisual(choice, globalData.selectedWeapon == choice.weapon);
    }

    private void SelectWeapon(Weapon weapon)
    {
        globalData.selectedWeapon = weapon;
        foreach (WeaponChoice choice in choices)
            UpdateChoiceVisual(choice, choice.weapon == weapon);
    }

    private static void UpdateChoiceVisual(WeaponChoice choice, bool selected)
    {
        choice.button.interactable = !selected;
        if (choice.label != null)
            choice.label.text = (selected ? "SELECTED\n" : string.Empty) +
                                "<b>" + choice.displayName + "</b>\n<size=85%>" + choice.description + "</size>";
    }

    private void AddWeaponIconToButton(WeaponChoice choice)
    {
        if (choice.iconPrefab == null)
            return;

        GameObject iconObject = Instantiate(choice.iconPrefab, choice.button.transform, false);
        iconObject.name = "Weapon Icon - " + choice.displayName;

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.12f, 0.46f);
        iconRect.anchorMax = new Vector2(0.88f, 0.94f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        iconRect.localScale = Vector3.one;

        UnityEngine.UI.Image image = iconObject.GetComponent<UnityEngine.UI.Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private static GameObject LoadWeaponIconPrefab(Weapon weapon)
    {
        string resourcePath;
        switch (weapon)
        {
            case Weapon.gun1:
                resourcePath = "PixelPrefabs/WeaponIcons/RapidBullets";
                break;
            case Weapon.piercingLaser:
                resourcePath = "PixelPrefabs/WeaponIcons/PiercingLaser";
                break;
            case Weapon.gun2:
                resourcePath = "PixelPrefabs/WeaponIcons/ShotgunBurst";
                break;
            case Weapon.flamethrower:
                resourcePath = "PixelPrefabs/WeaponIcons/Flamethrower";
                break;
            case Weapon.lightningArc:
                resourcePath = "PixelPrefabs/WeaponIcons/LightningArc";
                break;
            case Weapon.toxicSpitter:
                resourcePath = "PixelPrefabs/WeaponIcons/ToxicSpitter";
                break;
            default:
                return null;
        }
        return Resources.Load<GameObject>(resourcePath);
    }

    private static void StartPrototypeGame()
    {
        SceneManager.LoadScene(PrototypeSceneRoutes.GameplayScene);
    }
}

public static class PrototypeSceneRoutes
{
    public const string TitleScene = "Title card - Armory Prototype";
    public const string SelectionScene = "Game selecting things - Armory Prototype";
    public const string GameplayScene = "Game thing - Armory Prototype";
    public const string LossScene = "You lost - Armory Prototype";

    public static bool IsPrototypeGameplay(string sceneName) => sceneName == GameplayScene;
    public static bool IsPrototypeLoss(string sceneName) => sceneName == LossScene;
}
