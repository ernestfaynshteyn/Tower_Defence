using UnityEngine;
using TMPro;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance;

    [Header("Currency")]
    public int money = 0;
    [SerializeField] private string persistenceKey = "";

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

        if (!string.IsNullOrWhiteSpace(persistenceKey))
        {
            if (PlayerPrefs.HasKey(persistenceKey))
                money = Mathf.Max(0, PlayerPrefs.GetInt(persistenceKey, money));
            else
                PlayerPrefs.SetInt(persistenceKey, Mathf.Max(0, money));
        }

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (Instance == this && !string.IsNullOrWhiteSpace(persistenceKey))
            PlayerPrefs.Save();

        if (Instance == this)
            Instance = null;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && !string.IsNullOrWhiteSpace(persistenceKey))
            PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        if (!string.IsNullOrWhiteSpace(persistenceKey))
            PlayerPrefs.Save();
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
            return;

        money += amount;
        PersistMoney();
        UpdateUI();
    }

    public bool SpendMoney(int amount)
    {
        if (amount < 0)
            return false;

        if (money < amount)
            return false;

        money -= amount;
        PersistMoney();
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

    private void PersistMoney()
    {
        if (!string.IsNullOrWhiteSpace(persistenceKey))
            PlayerPrefs.SetInt(persistenceKey, Mathf.Max(0, money));
    }
}
