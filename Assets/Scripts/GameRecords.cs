using System.Globalization;
using System.Text;
using UnityEngine;

public enum StatusEffectRecord
{
    Burned,
    Poisoned,
    Slowed,
    Stunned,
    Shocked
}

/// <summary>
/// Owns lifetime combat records and the counters for the current run.
/// Values are saved at wave boundaries, scene changes, and application exit
/// instead of writing PlayerPrefs for every damage tick.
/// </summary>
public static class GameRecords
{
    private const string KeyPrefix = "GameRecords.v1.";
    private static bool loaded;
    private static int highestWave;
    private static double totalDamage;
    private static int burned;
    private static int poisoned;
    private static int slowed;
    private static int stunned;
    private static int shocked;

    public static int CurrentRunWave { get; private set; }
    public static double CurrentRunDamage { get; private set; }
    public static int CurrentRunBurned { get; private set; }
    public static int CurrentRunPoisoned { get; private set; }
    public static int CurrentRunSlowed { get; private set; }
    public static int CurrentRunStunned { get; private set; }
    public static int CurrentRunShocked { get; private set; }

    public static int HighestWave { get { EnsureLoaded(); return highestWave; } }
    public static double TotalDamage { get { EnsureLoaded(); return totalDamage; } }
    public static int Burned { get { EnsureLoaded(); return burned; } }
    public static int Poisoned { get { EnsureLoaded(); return poisoned; } }
    public static int Slowed { get { EnsureLoaded(); return slowed; } }
    public static int Stunned { get { EnsureLoaded(); return stunned; } }
    public static int Shocked { get { EnsureLoaded(); return shocked; } }

    public static void StartRun()
    {
        EnsureLoaded();
        CurrentRunWave = 0;
        CurrentRunDamage = 0d;
        CurrentRunBurned = 0;
        CurrentRunPoisoned = 0;
        CurrentRunSlowed = 0;
        CurrentRunStunned = 0;
        CurrentRunShocked = 0;
    }

    public static void RecordWaveReached(int wave)
    {
        EnsureLoaded();
        CurrentRunWave = Mathf.Max(CurrentRunWave, Mathf.Max(0, wave));
        highestWave = Mathf.Max(highestWave, CurrentRunWave);
    }

    public static void RecordDamage(float amount)
    {
        if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
            return;

        EnsureLoaded();
        CurrentRunDamage += amount;
        totalDamage += amount;
    }

    public static void RecordStatusEffect(StatusEffectRecord effect)
    {
        EnsureLoaded();
        switch (effect)
        {
            case StatusEffectRecord.Burned:
                burned++;
                CurrentRunBurned++;
                break;
            case StatusEffectRecord.Poisoned:
                poisoned++;
                CurrentRunPoisoned++;
                break;
            case StatusEffectRecord.Slowed:
                slowed++;
                CurrentRunSlowed++;
                break;
            case StatusEffectRecord.Stunned:
                stunned++;
                CurrentRunStunned++;
                break;
            case StatusEffectRecord.Shocked:
                shocked++;
                CurrentRunShocked++;
                break;
        }
    }

    public static string GetSummaryText()
    {
        EnsureLoaded();
        var summary = new StringBuilder(220);
        summary.AppendLine("RECORDS");
        summary.Append("Highest wave: ").AppendLine(highestWave.ToString("N0", CultureInfo.InvariantCulture));
        summary.Append("Total damage: ").AppendLine(totalDamage.ToString("N0", CultureInfo.InvariantCulture));
        summary.Append("Burned: ").Append(burned.ToString("N0", CultureInfo.InvariantCulture));
        summary.Append("   Poisoned: ").AppendLine(poisoned.ToString("N0", CultureInfo.InvariantCulture));
        summary.Append("Slowed: ").Append(slowed.ToString("N0", CultureInfo.InvariantCulture));
        summary.Append("   Stunned: ").AppendLine(stunned.ToString("N0", CultureInfo.InvariantCulture));
        summary.Append("Shocked: ").Append(shocked.ToString("N0", CultureInfo.InvariantCulture));
        return summary.ToString();
    }

    public static void Save()
    {
        EnsureLoaded();
        PlayerPrefs.SetInt(KeyPrefix + "HighestWave", highestWave);
        PlayerPrefs.SetString(KeyPrefix + "TotalDamage", totalDamage.ToString("R", CultureInfo.InvariantCulture));
        PlayerPrefs.SetInt(KeyPrefix + "Burned", burned);
        PlayerPrefs.SetInt(KeyPrefix + "Poisoned", poisoned);
        PlayerPrefs.SetInt(KeyPrefix + "Slowed", slowed);
        PlayerPrefs.SetInt(KeyPrefix + "Stunned", stunned);
        PlayerPrefs.SetInt(KeyPrefix + "Shocked", shocked);
        PlayerPrefs.Save();
    }

    private static void EnsureLoaded()
    {
        if (loaded)
            return;

        loaded = true;
        highestWave = Mathf.Max(0, PlayerPrefs.GetInt(KeyPrefix + "HighestWave", 0));
        totalDamage = LoadDouble(KeyPrefix + "TotalDamage");
        burned = Mathf.Max(0, PlayerPrefs.GetInt(KeyPrefix + "Burned", 0));
        poisoned = Mathf.Max(0, PlayerPrefs.GetInt(KeyPrefix + "Poisoned", 0));
        slowed = Mathf.Max(0, PlayerPrefs.GetInt(KeyPrefix + "Slowed", 0));
        stunned = Mathf.Max(0, PlayerPrefs.GetInt(KeyPrefix + "Stunned", 0));
        shocked = Mathf.Max(0, PlayerPrefs.GetInt(KeyPrefix + "Shocked", 0));
    }

    private static double LoadDouble(string key)
    {
        string stored = PlayerPrefs.GetString(key, "0");
        return double.TryParse(stored, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            ? System.Math.Max(0d, value)
            : 0d;
    }
}
