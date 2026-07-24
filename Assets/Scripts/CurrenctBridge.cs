using UnityEngine;

/// <summary>
/// The ONLY place the skill tree touches your currency code.
/// Matched to your existing CurrencyManager (money / GetMoney / SpendMoney / AddMoney).
/// </summary>
public static class CurrencyBridge
{
    public static bool IsReady => CurrencyManager.Instance != null;

    public static int Current =>
        CurrencyManager.Instance != null ? CurrencyManager.Instance.GetMoney() : 0;

    public static bool Spend(int amount)
    {
        if (amount <= 0) return true;
        if (CurrencyManager.Instance == null) return false;
        return CurrencyManager.Instance.SpendMoney(amount);
    }

    public static void Refund(int amount)
    {
        if (amount <= 0 || CurrencyManager.Instance == null) return;
        CurrencyManager.Instance.AddMoney(amount);
    }
}