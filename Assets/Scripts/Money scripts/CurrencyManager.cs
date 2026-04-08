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
            Destroy(gameObject);
            return;
        }

        UpdateUI();
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

        if (SkillTreeScript.skillTree != null)
            SkillTreeScript.skillTree.UpdateAllSkillUI();
    }
}