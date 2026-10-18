#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports the supporting CC0 effects with lightweight settings and keeps the
/// shared GameAudio prefab wired without requiring per-scene AudioSources.
/// </summary>
[InitializeOnLoad]
internal static class GameAudioInstaller
{
    private const int InstallerVersion = 2;
    private const string PrefabPath = "Assets/Resources/GameAudio.prefab";

    private readonly struct ClipBinding
    {
        public readonly string PropertyName;
        public readonly string AssetPath;
        public readonly bool IsMusic;

        public ClipBinding(string propertyName, string fileName, bool isMusic = false)
        {
            PropertyName = propertyName;
            AssetPath = "Assets/Sounds/" + fileName;
            IsMusic = isMusic;
        }
    }

    private static readonly ClipBinding[] Bindings =
    {
        new ClipBinding("gameplayMusic", "Music_Gameplay_Pulse_CC0.mp3", true),
        new ClipBinding("bossMusic", "Music_Boss_Urgent_CC0.mp3", true),
        new ClipBinding("laserLoop", "Weapon_Laser_Fire_Loop.ogg"),
        new ClipBinding("laserStart", "Weapon_Laser_Start.ogg"),
        new ClipBinding("laserStop", "Weapon_Laser_Stop.ogg"),
        new ClipBinding("lightningArc", "Weapon_Lightning_Arc.ogg"),
        new ClipBinding("lightningChain", "Weapon_Lightning_Chain.ogg"),
        new ClipBinding("toxicSpitterFire", "Weapon_ToxicSpitter_Fire.ogg"),
        new ClipBinding("toxicSpitterImpact", "Weapon_ToxicSpitter_Impact.ogg"),
        new ClipBinding("toxicPoolLoop", "Weapon_Toxic_Pool_Loop.ogg"),
        new ClipBinding("flamethrowerIgnite", "Weapon_Flamethrower_Ignite.ogg"),
        new ClipBinding("flamethrowerStop", "Weapon_Flamethrower_Stop.ogg"),
        new ClipBinding("overheatVent", "Weapon_Overheat_Vent.ogg"),
        new ClipBinding("overheatReady", "Weapon_Overheat_Ready.ogg"),
        new ClipBinding("iceSlimeShatter", "SFX_Slime_Ice_Shatter.ogg"),
        new ClipBinding("acidSlimeSizzle", "SFX_Slime_Acid_Sizzle.ogg"),
        new ClipBinding("electricSlimeCharge", "SFX_Slime_Electric_Charge.ogg"),
        new ClipBinding("toxicSlimeTrail", "SFX_Slime_Toxic_Trail.ogg"),
        new ClipBinding("megaSlimeSpawn", "SFX_MegaSlime_Spawn.ogg"),
        new ClipBinding("megaSlimeSplit", "SFX_MegaSlime_Split.ogg"),
        new ClipBinding("miniSlimeSpawn", "SFX_MiniSlime_Spawn.ogg"),
        new ClipBinding("miniTurretFire", "SFX_MiniTurret_Fire.ogg"),
        new ClipBinding("spikeBarrierHit", "SFX_SpikeBarrier_Hit.ogg"),
        new ClipBinding("overchargeActivate", "SFX_Overcharge_Activate.ogg"),
        new ClipBinding("freezeBurstActivate", "SFX_FreezeBurst_Activate.ogg"),
        new ClipBinding("reflectorRicochet", "SFX_Reflector_Ricochet.ogg"),
        new ClipBinding("empCharge", "SFX_EMP_Charge.ogg"),
        new ClipBinding("empBlast", "SFX_EMP_Blast.ogg"),
        new ClipBinding("toxicFloodActivate", "SFX_ToxicFlood_Activate.ogg"),
        new ClipBinding("abilityReady", "SFX_Ability_Ready.ogg"),
        new ClipBinding("hugeWaveWarning", "SFX_HugeWave_Warning.ogg"),
        new ClipBinding("bossIncomingAlarm", "SFX_Boss_Incoming_Alarm.ogg"),
        new ClipBinding("defenceRecharging", "SFX_Player_Defence_Recharging.ogg"),
        new ClipBinding("defenceFull", "SFX_Player_Defence_Full.ogg"),
        new ClipBinding("upgradeUnlocked", "SFX_Upgrade_Unlocked.ogg"),
        new ClipBinding("tabOpen", "UI_Tab_Open.ogg"),
        new ClipBinding("toggleSelect", "UI_Toggle_Select.ogg"),
    };

    static GameAudioInstaller()
    {
        EditorApplication.delayCall += TryAutomaticInstall;
    }

    private static string InstallKey =>
        "TowerDefence.GameAudioInstaller." + Application.dataPath + ".v" + InstallerVersion;

    [MenuItem("Tools/Tower Defence/Rewire Game Audio")]
    private static void InstallFromMenu()
    {
        Install(true);
    }

    private static void TryAutomaticInstall()
    {
        if (EditorPrefs.GetBool(InstallKey, false))
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryAutomaticInstall;
            return;
        }

        if (Install(false))
            EditorPrefs.SetBool(InstallKey, true);
    }

    private static bool Install(bool logSuccess)
    {
        GameObject root = null;
        try
        {
            root = PrefabUtility.LoadPrefabContents(PrefabPath);
            BackgroundMusic audio = root != null ? root.GetComponent<BackgroundMusic>() : null;
            if (audio == null)
            {
                Debug.LogError("[GameAudioInstaller] GameAudio.prefab is missing BackgroundMusic.");
                return false;
            }

            SerializedObject serializedAudio = new SerializedObject(audio);
            bool changed = false;
            foreach (ClipBinding binding in Bindings)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(binding.AssetPath);
                SerializedProperty property = serializedAudio.FindProperty(binding.PropertyName);
                if (clip == null || property == null)
                {
                    Debug.LogError(
                        $"[GameAudioInstaller] Could not bind '{binding.AssetPath}' to '{binding.PropertyName}'.");
                    return false;
                }

                ConfigureImporter(binding.AssetPath, binding.IsMusic);
                if (property.objectReferenceValue != clip)
                {
                    property.objectReferenceValue = clip;
                    changed = true;
                }
            }

            if (changed)
            {
                serializedAudio.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(audio);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }

            if (logSuccess || changed)
                Debug.Log("[GameAudioInstaller] GameAudio now contains separate gameplay and boss music plus 35 balanced supporting effects.");
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return false;
        }
        finally
        {
            if (root != null)
                PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureImporter(string assetPath, bool isMusic)
    {
        if (!(AssetImporter.GetAtPath(assetPath) is AudioImporter importer))
            return;

        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        AudioClipLoadType loadType = isMusic
            ? AudioClipLoadType.Streaming
            : AudioClipLoadType.CompressedInMemory;
        float quality = isMusic ? 0.7f : 0.55f;
        bool preload = !isMusic;
        bool forceToMono = !isMusic;
        bool changed = importer.forceToMono != forceToMono ||
                       importer.loadInBackground != isMusic ||
                       settings.preloadAudioData != preload ||
                       settings.loadType != loadType ||
                       settings.compressionFormat != AudioCompressionFormat.Vorbis ||
                       !Mathf.Approximately(settings.quality, quality);
        if (!changed)
            return;

        importer.forceToMono = forceToMono;
        importer.loadInBackground = isMusic;
        settings.preloadAudioData = preload;
        settings.loadType = loadType;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = quality;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
    }
}
#endif
