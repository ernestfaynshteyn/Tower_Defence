using UnityEngine;

public class UseGrenade : MonoBehaviour
{
    public GameObject grenadePrefab;
    public Transform throwPoint;

    public GrenadeType selectedGrenade = GrenadeType.Frag;

    public float throwForce = 8f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            ThrowGrenade();
        }
    }

    void ThrowGrenade()
    {
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
}