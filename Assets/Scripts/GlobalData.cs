using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public enum Difficulty
{
    Easy,
    Normal,
    Hard,
    Extreme
}
public enum Weapon
{
    flamethrower,
    gun1,
    gun2,
    gun3,
    lightningArc,
    toxicSpitter,
    piercingLaser
}
public class GlobalData : MonoBehaviour
{
    public static GlobalData Instance;

    // This defaults on for the current game. A future Settings menu can
    // change this property without any weapon scripts needing to be rewritten.
    public static bool AutomaticPrimaryAiming { get; set; } = true;

    private static bool warnedAboutMissingGlobalData;

     public string lastEnemyThatKilledPlayer;
    public Sprite sprite;

    public Difficulty currentDifficulty = Difficulty.Normal;
    public Weapon selectedWeapon = Weapon.flamethrower;

    // When the gameplay scene is launched directly from the Unity Editor, the
    // menu's GlobalData object does not exist. Use the first difficulty (Easy)
    // and the default weapon so gameplay can still start safely.
    public static Difficulty ActiveDifficulty
    {
        get
        {
            if (Instance != null)
                return Instance.currentDifficulty;

            if (!warnedAboutMissingGlobalData)
            {
                warnedAboutMissingGlobalData = true;
                Debug.Log("GlobalData was not loaded. Starting this direct scene launch on Easy difficulty.");
            }

            return Difficulty.Easy;
        }
    }

    public static Weapon ActiveWeapon => Instance != null
        ? Instance.selectedWeapon
        : Weapon.flamethrower;

    public static int GetBossWaveInterval()
    {
        switch (ActiveDifficulty)
        {
            case Difficulty.Easy: return 15;
            case Difficulty.Normal: return 12;
            case Difficulty.Hard: return 8;
            case Difficulty.Extreme: return 5;
            default: return 12;
        }
    }

    public static float GetMoneyRewardMultiplier()
    {
        switch (ActiveDifficulty)
        {
            case Difficulty.Easy: return 0.9f;
            case Difficulty.Normal: return 1f;
            case Difficulty.Hard: return 1.5f;
            case Difficulty.Extreme: return 2f;
            default: return 1f;
        }
    }

    public static float GetUpgradePriceMultiplier()
    {
        switch (ActiveDifficulty)
        {
            case Difficulty.Easy: return 0.85f;
            case Difficulty.Normal: return 1f;
            case Difficulty.Hard: return 1.25f;
            case Difficulty.Extreme: return 1.5f;
            default: return 1f;
        }
    }

    public static int ScaleMoneyReward(int baseAmount)
    {
        if (baseAmount <= 0)
            return 0;

        return Mathf.Max(1, Mathf.RoundToInt(baseAmount * GetMoneyRewardMultiplier()));
    }

    public static int ScaleUpgradePrice(int basePrice)
    {
        if (basePrice <= 0)
            return 0;

        return Mathf.Max(1, Mathf.RoundToInt(basePrice * GetUpgradePriceMultiplier()));
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

        }
        else
        {
            Destroy(gameObject);
        }
    }


    // Difficulty buttons
    public void SetEasy() => currentDifficulty = Difficulty.Easy;
    public void SetNormal() => currentDifficulty = Difficulty.Normal;
    public void SetHard() => currentDifficulty = Difficulty.Hard;
    public void SetExtreme() => currentDifficulty = Difficulty.Extreme;

    public void SelectFlamethrower() => selectedWeapon = Weapon.flamethrower;
    public void SelectGun() => selectedWeapon = Weapon.gun1;
    public void Selectgun1() => selectedWeapon = Weapon.gun2;
    public void Selectgun2() => selectedWeapon = Weapon.gun3;

    public void StartGame()
    {
        Debug.Log("Difficulty: " + currentDifficulty);
        Debug.Log("Weapon: " + selectedWeapon);


        SceneManager.LoadScene("Game thing");
    }
}
