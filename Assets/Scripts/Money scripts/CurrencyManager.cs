using UnityEngine;
using TMPro;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance;

    [Header("Currency")]
    public int money = 0;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI moneyText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // A second currency component must not remove the GameObject it sits on:
            // in the gameplay scene those objects also own wave, player-stat, and UI
            // components. Prefer the manager that has a money UI assigned, then keep
            // the existing instance for an otherwise ambiguous duplicate.
            if (moneyText != null && Instance.moneyText == null)
            {
                CurrencyManager previous = Instance;
                Instance = this;
                Destroy(previous);
            }
            else
            {
                Destroy(this);
                return;
            }
        }

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddMoney(int amount)
    {
        money += amount;
        UpdateUI();
    }

    public bool SpendMoney(int amount)
    {
        if (money < amount)
            return false;

        money -= amount;
        UpdateUI();
        return true;
    }

    public int GetMoney()
    {
        return money;
    }

    void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = money.ToString();

    }
}
