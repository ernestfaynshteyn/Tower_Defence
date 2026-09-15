    using UnityEngine;
using UnityEngine.SceneManagement;

public class WeaponSelect : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void SelectWeapon(string weaponName)
    {
        if (GlobalData.Instance != null && TryGetWeapon(weaponName, out Weapon weapon))
        {
            GlobalData.Instance.selectedWeapon = weapon;
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.selectedWeapon = weaponName;
        }

        SceneManager.LoadScene("Game thing");
    }

    private bool TryGetWeapon(string weaponName, out Weapon weapon)
    {
        string name = (weaponName ?? string.Empty).Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();

        switch (name)
        {
            case "flamethrower": weapon = Weapon.flamethrower; return true;
            case "gun":
            case "gun1": weapon = Weapon.gun1; return true;
            case "shotgun":
            case "gun2": weapon = Weapon.gun2; return true;
            case "minigun":
            case "gun3": weapon = Weapon.gun3; return true;
            default:
                weapon = Weapon.flamethrower;
                Debug.LogWarning($"Unknown weapon '{weaponName}'. Keeping the current selection.", this);
                return false;
        }
    }
}
