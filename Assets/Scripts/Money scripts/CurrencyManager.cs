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
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            // Uncomment if you want money to persist between scenes
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        UpdateUI();
    }

    // =====================
    // PUBLIC API
    // =====================
    public void AddMoney(int amount)
    {
        money += amount;
        UpdateUI();
        Debug.Log("Money: " + money);
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

    // =====================
    // UI
    // =====================
    void UpdateUI()
    {
        if (moneyText != null)
        {
            moneyText.text = money.ToString();
        }
    }
}
    