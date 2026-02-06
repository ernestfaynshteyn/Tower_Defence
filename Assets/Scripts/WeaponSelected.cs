using UnityEngine;

public class WeaponSelected : MonoBehaviour
{
    public GameObject flamethrower;
    public GameObject gun1;
    public GameObject gun2;
    public GameObject gun3;

    void Start()
    {
        ApplyWeapon();
    }

    public void ApplyWeapon()
    {
        // Hide all
        flamethrower.SetActive(false);
        gun1.SetActive(false);
        gun2.SetActive(false);
        gun3.SetActive(false);

        // Show selected
        switch (GlobalData.Instance.selectedWeapon)
        {
            case Weapon.flamethrower:
                flamethrower.SetActive(true);
                break;

            case Weapon.gun1:
                gun1.SetActive(true);
                break;

            case Weapon.gun2:
                gun2.SetActive(true);
                break;

            case Weapon.gun3:
                gun3.SetActive(true);
                break;
        }
    }
}
