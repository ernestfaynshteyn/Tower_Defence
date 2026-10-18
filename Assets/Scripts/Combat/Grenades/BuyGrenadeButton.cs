using UnityEngine;

public class BuyGrenadeButton : MonoBehaviour
{
    public GrenadeType grenadeToBuy = GrenadeType.Frag;
    [Min(0)] public int cost = 50;

    public void BuyGrenade()
    {
        if (Inventory.instance == null)
        {
            Debug.LogError("Cannot buy a grenade because the Inventory is missing.", this);
            return;
        }

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("Cannot buy a grenade because the CurrencyManager is missing.", this);
            return;
        }

        if (!CurrencyManager.Instance.SpendMoney(GlobalData.ScaleUpgradePrice(Mathf.Max(0, cost))))
        {
            BackgroundMusic.PurchaseDenied();
            return;
        }

        Inventory.instance.AddGrenade(grenadeToBuy);
        BackgroundMusic.Purchase();
    }
}
