using UnityEngine;

public class UseGrenade : MonoBehaviour
{
    public GameObject grenadePrefab;
    public Transform throwPoint;

    public GrenadeType selectedGrenade = GrenadeType.Frag;

    public float throwForce = 8f;

    [Header("Cursor")]
    public Texture2D defaultCursor;
    public Texture2D equippedCursor;
    public Vector2 hotspot = Vector2.zero;

    public bool isEquipped = false;

    void Start()
    {
        UpdateCursor();
    }

    void Update()
    {
        // Test key to equip / unequip
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleEquip();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ThrowGrenade();
        }
    }

    void UpdateCursor()
    {
        if (isEquipped)
            Cursor.SetCursor(equippedCursor, hotspot, CursorMode.Auto);
        else
            Cursor.SetCursor(defaultCursor, hotspot, CursorMode.Auto);
    }

    void ThrowGrenade()
    {
        if (!isEquipped) return;

        if (!Inventory.instance.UseGrenade(selectedGrenade))
            return;

        GameObject grenade = Instantiate(grenadePrefab, throwPoint.position, Quaternion.identity);

        Grenades grenadeScript = grenade.GetComponent<Grenades>();
        grenadeScript.grenadeType = selectedGrenade;

        Rigidbody2D rb = grenade.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = transform.right * throwForce;
        }
    }

    public void EquipGrenade()
    {
        isEquipped = true;
        UpdateCursor();
    }

    public void UnequipGrenade()
    {
        isEquipped = false;
        UpdateCursor();
    }

    public void ToggleEquip()
    {
        isEquipped = !isEquipped;
        UpdateCursor();
    }
}q