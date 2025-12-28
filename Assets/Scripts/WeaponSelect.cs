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
        LevelManager.Instance.selectedWeapon = weaponName;
        SceneManager.LoadScene("Game Thing");
    }
}
