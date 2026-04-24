using UnityEngine;

public class BuyGrenadeButton : MonoBehaviour
{
    public GrenadeType grenadeToBuy = GrenadeType.Frag;

    public void BuyGrenade()
    {
        Inventory.instance.AddGrenade(grenadeToBuy);
    }
}