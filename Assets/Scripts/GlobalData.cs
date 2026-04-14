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
    gun3
}
public class GlobalData : MonoBehaviour
{
    public static GlobalData Instance;

     public string lastEnemyThatKilledPlayer;
    public Sprite sprite;

    public Difficulty currentDifficulty = Difficulty.Normal;
    public Weapon selectedWeapon = Weapon.flamethrower;

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
