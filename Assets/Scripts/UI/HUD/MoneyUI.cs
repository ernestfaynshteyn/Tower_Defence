// AI assistance: Codex helped revise the gameplay HUD. Ernest directed the design.
using TMPro;
using UnityEngine;

public class MoneyUI : MonoBehaviour
{
    public TextMeshProUGUI moneyText;

    void Update()
    {
        moneyText.text = "$: " + CurrencyManager.Instance.money;
    }
}
