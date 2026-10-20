using System;
using UnityEngine;

public static class GameSettings
{
    public enum EffectCategory
    {
        EffectsMaster,
        Shooting,
        Explosions,
        Abilities,
        World,
        Interface
    }

    private const string MasterKey = "TowerDefence.Settings.MasterVolume";
    private const string MusicMasterKey = "TowerDefence.Settings.MusicMasterVolume";
    private const string BackgroundMusicKey = "TowerDefence.Settings.BackgroundMusicVolume";
    private const string BossMusicKey = "TowerDefence.Settings.BossMusicVolume";
    private const string EffectsMasterKey = "TowerDefence.Settings.EffectsMasterVolume";
    private const string ShootingKey = "TowerDefence.Settings.ShootingVolume";
    private const string ExplosionsKey = "TowerDefence.Settings.ExplosionsVolume";
    private const string AbilitiesKey = "TowerDefence.Settings.AbilitiesVolume";
    private const string WorldKey = "TowerDefence.Settings.WorldVolume";
    private const string InterfaceKey = "TowerDefence.Settings.InterfaceVolume";
    private const string ScreenShakeKey = "TowerDefence.Settings.ScreenShake";
    private const string AutoShootKey = "TowerDefence.Settings.AutoShoot";

    private static float masterVolume = ReadVolume(MasterKey);
    private static float musicMasterVolume = ReadVolume(MusicMasterKey);
    private static float backgroundMusicVolume = ReadVolume(BackgroundMusicKey);
    private static float bossMusicVolume = ReadVolume(BossMusicKey);
    private static float effectsMasterVolume = ReadVolume(EffectsMasterKey);
    private static float shootingVolume = ReadVolume(ShootingKey);
    private static float explosionsVolume = ReadVolume(ExplosionsKey);
    private static float abilitiesVolume = ReadVolume(AbilitiesKey);
    private static float worldVolume = ReadVolume(WorldKey);
    private static float interfaceVolume = ReadVolume(InterfaceKey);
    private static float screenShake = ReadVolume(ScreenShakeKey);
    private static bool autoShoot = PlayerPrefs.GetInt(AutoShootKey, 1) != 0;

    public static event Action AudioSettingsChanged;

    public static float MasterVolume => masterVolume;
    public static float MusicMasterVolume => musicMasterVolume;
    public static float BackgroundMusicVolume => backgroundMusicVolume;
    public static float BossMusicVolume => bossMusicVolume;
    public static float EffectsMasterVolume => effectsMasterVolume;
    public static float ShootingVolume => shootingVolume;
    public static float ExplosionsVolume => explosionsVolume;
    public static float AbilitiesVolume => abilitiesVolume;
    public static float WorldVolume => worldVolume;
    public static float InterfaceVolume => interfaceVolume;
    public static float ScreenShake => screenShake;
    public static bool AutomaticShootingEnabled => autoShoot;

    public static void SetMasterVolume(float value) => SetVolume(MasterKey, ref masterVolume, value);
    public static void SetMusicMasterVolume(float value) => SetVolume(MusicMasterKey, ref musicMasterVolume, value);
    public static void SetBackgroundMusicVolume(float value) => SetVolume(BackgroundMusicKey, ref backgroundMusicVolume, value);
    public static void SetBossMusicVolume(float value) => SetVolume(BossMusicKey, ref bossMusicVolume, value);
    public static void SetEffectsMasterVolume(float value) => SetVolume(EffectsMasterKey, ref effectsMasterVolume, value);
    public static void SetShootingVolume(float value) => SetVolume(ShootingKey, ref shootingVolume, value);
    public static void SetExplosionsVolume(float value) => SetVolume(ExplosionsKey, ref explosionsVolume, value);
    public static void SetAbilitiesVolume(float value) => SetVolume(AbilitiesKey, ref abilitiesVolume, value);
    public static void SetWorldVolume(float value) => SetVolume(WorldKey, ref worldVolume, value);
    public static void SetInterfaceVolume(float value) => SetVolume(InterfaceKey, ref interfaceVolume, value);
    public static void SetScreenShake(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(screenShake, value))
            return;

        screenShake = value;
        PlayerPrefs.SetFloat(ScreenShakeKey, value);
    }

    public static void SetAutomaticShootingEnabled(bool value)
    {
        if (autoShoot == value)
            return;

        autoShoot = value;
        PlayerPrefs.SetInt(AutoShootKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static float GetMusicMultiplier(bool bossMusic)
    {
        return musicMasterVolume * (bossMusic ? bossMusicVolume : backgroundMusicVolume);
    }

    public static float GetEffectCategoryMultiplier(EffectCategory category)
    {
        switch (category)
        {
            case EffectCategory.Shooting: return shootingVolume;
            case EffectCategory.Explosions: return explosionsVolume;
            case EffectCategory.Abilities: return abilitiesVolume;
            case EffectCategory.World: return worldVolume;
            case EffectCategory.Interface: return interfaceVolume;
            default: return 1f;
        }
    }

    public static float GetEffectSourceMultiplier(EffectCategory category)
    {
        return effectsMasterVolume * GetEffectCategoryMultiplier(category);
    }

    public static void Save() => PlayerPrefs.Save();

    private static float ReadVolume(string key)
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(key, 1f));
    }

    private static void SetVolume(string key, ref float currentValue, float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(currentValue, value))
            return;

        currentValue = value;
        PlayerPrefs.SetFloat(key, value);
        AudioSettingsChanged?.Invoke();
    }
}
