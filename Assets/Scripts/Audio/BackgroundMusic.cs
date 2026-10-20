using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class BackgroundMusic : MonoBehaviour
{
    private sealed class ManagedAudioSource
    {
        public AudioSource Source;
        public float BaseVolume;
        public GameSettings.EffectCategory Category;
    }

    [Header("Music")]
    [Tooltip("Used on the title, game selection, and losing screens.")]
    public AudioClip music;
    [Tooltip("Used during normal gameplay and between boss waves.")]
    public AudioClip gameplayMusic;
    [Tooltip("Used while a boss wave is active.")]
    public AudioClip bossMusic;
    [Range(0f, 1f)] public float menuMusicVolume = 0.26f;
    [Range(0f, 1f)] public float gameplayMusicVolume = 1f;
    [Range(0f, 1f)] public float bossMusicVolume = 1f;
    [Min(0f)] public float musicFadeDuration = 0.35f;

    [Header("General Feedback")]
    public AudioClip bossEncounter, bossDefeat, wave, purchase, hover;
    public AudioClip hit, death, playerHurt, playerDeath, purchaseDenied;
    public AudioClip buttonClick, reload;

    [Header("Existing Combat Sounds")]
    public AudioClip gunOverheatStart, flameOverheatStart, overheatSound;
    public AudioClip frag, nuke, shotgun, ak, minigun, flame;
    public AudioClip smokeStart, smokeLoop, molotovShatter, molotovLoop, flashExplosion, flashRinging;
    public AudioClip dodge, defenceHit, grenadeThrow, pulsarActivate, pulsarPulse;
    public AudioClip nukeLaunch, waveComplete, defenceBreak, lowHealthWarning;

    [Header("Primary Weapons")]
    public AudioClip laserLoop, laserStart, laserStop;
    public AudioClip lightningArc, lightningChain;
    public AudioClip toxicSpitterFire, toxicSpitterImpact, toxicPoolLoop;
    public AudioClip flamethrowerIgnite, flamethrowerStop;
    public AudioClip overheatVent, overheatReady;

    [Header("Slimes")]
    public AudioClip iceSlimeShatter, acidSlimeSizzle, electricSlimeCharge, toxicSlimeTrail;
    public AudioClip megaSlimeSpawn, megaSlimeSplit, miniSlimeSpawn;

    [Header("Attachments and Abilities")]
    public AudioClip miniTurretFire, spikeBarrierHit, overchargeActivate, freezeBurstActivate;
    public AudioClip reflectorRicochet, empCharge, empBlast, toxicFloodActivate, abilityReady;

    [Header("Warnings, Defence, and UI")]
    public AudioClip hugeWaveWarning, bossIncomingAlarm;
    public AudioClip defenceRecharging, defenceFull, upgradeUnlocked;
    public AudioClip tabOpen, toggleSelect;

    private static BackgroundMusic instance;
    private AudioSource musicSource, effectsSource, defenceRegenSource;
    private readonly List<ManagedAudioSource> managedAudioSources = new List<ManagedAudioSource>();
    private Coroutine musicTransition;
    private bool currentSceneIsGameplay;
    private bool currentMusicIsBoss;
    private float nextHit, nextDeath, nextHover, nextLightningChain;
    private float nextMiniTurret, nextSpikeHit, nextRicochet, nextToxicTrail;
    private float nextAbilityReady, nextToggle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var prefab = Resources.Load<GameObject>("GameAudio");
        if (prefab != null) Instantiate(prefab);
        else Debug.LogWarning("Missing Resources/GameAudio audio configuration.");
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        musicSource = CreateRawSource(gameObject, true, 0.35f);
        effectsSource = CreateManagedSource(gameObject, false, 1f, GameSettings.EffectCategory.EffectsMaster);
        defenceRegenSource = CreateManagedSource(gameObject, true, 0.12f, GameSettings.EffectCategory.World);
        GameSettings.AudioSettingsChanged += ApplyAudioSettings;
        SceneManager.sceneLoaded += SceneLoaded;
        currentSceneIsGameplay = IsGameplayScene(SceneManager.GetActiveScene());
        ApplyAudioSettings();
        SetMusic(false);
    }

    private void Start()
    {
        // The persistent audio prefab is created before the first scene. Check
        // again on Start, after that scene and its WaveManager are fully loaded.
        currentSceneIsGameplay = IsGameplayScene(SceneManager.GetActiveScene());
        SetMusic(false);
    }

    public static AudioSource CreateSource(GameObject owner, bool loop, float volume)
    {
        if (instance != null)
            return instance.CreateManagedSource(owner, loop, volume, GameSettings.EffectCategory.Shooting);

        return CreateRawSource(owner, loop,
            volume * GameSettings.GetEffectSourceMultiplier(GameSettings.EffectCategory.Shooting));
    }

    private static AudioSource CreateRawSource(GameObject owner, bool loop, float volume)
    {
        var source = owner.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = volume;
        return source;
    }

    private AudioSource CreateManagedSource(GameObject owner, bool loop, float baseVolume,
        GameSettings.EffectCategory category)
    {
        for (int i = managedAudioSources.Count - 1; i >= 0; i--)
            if (managedAudioSources[i].Source == null)
                managedAudioSources.RemoveAt(i);

        AudioSource source = CreateRawSource(owner, loop, baseVolume);
        managedAudioSources.Add(new ManagedAudioSource
        {
            Source = source,
            BaseVolume = baseVolume,
            Category = category
        });
        source.volume = baseVolume * GameSettings.GetEffectSourceMultiplier(category);
        return source;
    }

    private static void SetManagedSourceBaseVolume(AudioSource source, float baseVolume,
        GameSettings.EffectCategory category)
    {
        if (source == null)
            return;

        if (instance == null)
        {
            source.volume = baseVolume * GameSettings.GetEffectSourceMultiplier(category);
            return;
        }

        for (int i = 0; i < instance.managedAudioSources.Count; i++)
        {
            ManagedAudioSource managed = instance.managedAudioSources[i];
            if (managed.Source != source)
                continue;

            managed.BaseVolume = baseVolume;
            managed.Category = category;
            source.volume = baseVolume * GameSettings.GetEffectSourceMultiplier(category);
            return;
        }

        instance.managedAudioSources.Add(new ManagedAudioSource
        {
            Source = source,
            BaseVolume = baseVolume,
            Category = category
        });
        source.volume = baseVolume * GameSettings.GetEffectSourceMultiplier(category);
    }

    private void ApplyAudioSettings()
    {
        AudioListener.volume = GameSettings.MasterVolume;
        for (int i = managedAudioSources.Count - 1; i >= 0; i--)
        {
            ManagedAudioSource managed = managedAudioSources[i];
            if (managed.Source == null)
            {
                managedAudioSources.RemoveAt(i);
                continue;
            }

            managed.Source.volume = managed.BaseVolume *
                                    GameSettings.GetEffectSourceMultiplier(managed.Category);
        }

        if (musicSource != null && (musicSource.clip != null || musicTransition != null))
            SetMusic(currentMusicIsBoss);
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single || IsGameplayScene(scene))
            currentSceneIsGameplay = IsGameplayScene(scene);

        SetMusic(false);
        foreach (var button in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (button.gameObject.scene == scene && button.GetComponent<ButtonAudioHover>() == null)
                button.gameObject.AddComponent<ButtonAudioHover>();

        foreach (var toggle in FindObjectsByType<UnityEngine.UI.Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (toggle.gameObject.scene == scene && toggle.GetComponent<ButtonAudioHover>() == null)
                toggle.gameObject.AddComponent<ButtonAudioHover>();
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= SceneLoaded;
        GameSettings.AudioSettingsChanged -= ApplyAudioSettings;
        instance = null;
    }

    private static bool IsGameplayScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return false;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<WaveManager>(true) != null)
                return true;
        }

        return false;
    }

    private void SetMusic(bool boss)
    {
        currentMusicIsBoss = boss && bossMusic != null;
        if (!boss && !currentSceneIsGameplay)
            currentSceneIsGameplay = IsGameplayScene(SceneManager.GetActiveScene());

        AudioClip clip;
        float targetVolume;
        if (currentMusicIsBoss)
        {
            clip = bossMusic;
            targetVolume = bossMusicVolume;
        }
        else if (currentSceneIsGameplay && gameplayMusic != null)
        {
            clip = gameplayMusic;
            targetVolume = gameplayMusicVolume;
        }
        else
        {
            clip = music;
            targetVolume = menuMusicVolume;
        }

        targetVolume *= GameSettings.GetMusicMultiplier(currentMusicIsBoss);

        if (musicSource.clip == clip)
        {
            if (musicTransition != null)
            {
                StopCoroutine(musicTransition);
                musicTransition = null;
            }
            musicSource.volume = targetVolume;
            if (clip != null && !musicSource.isPlaying)
                musicSource.Play();
            return;
        }

        if (musicTransition != null)
            StopCoroutine(musicTransition);
        musicTransition = StartCoroutine(TransitionMusic(clip, targetVolume));
    }

    private IEnumerator TransitionMusic(AudioClip clip, float targetVolume)
    {
        float fadeDuration = Mathf.Max(0f, musicFadeDuration);
        if (musicSource.isPlaying && fadeDuration > 0f)
        {
            float startVolume = musicSource.volume;
            for (float elapsed = 0f; elapsed < fadeDuration; elapsed += Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                yield return null;
            }
        }

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.volume = fadeDuration > 0f ? 0f : targetVolume;
        if (clip == null)
        {
            musicTransition = null;
            yield break;
        }

        if (clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();

        musicSource.Play();
        if (fadeDuration > 0f)
        {
            for (float elapsed = 0f; elapsed < fadeDuration; elapsed += Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeDuration);
                yield return null;
            }
        }

        musicSource.volume = targetVolume;
        musicTransition = null;
    }

    public static void WaveStarted(bool boss)
    {
        if (instance == null) return;
        instance.currentSceneIsGameplay = true;
        instance.SetMusic(boss);
        if (boss)
        {
            instance.Play(instance.bossIncomingAlarm, 0.5f);
            instance.Play(instance.bossEncounter, 0.42f);
        }
        else
        {
            instance.Play(instance.wave, 0.7f);
        }
    }

    public static void WaveEnded() { if (instance != null) instance.SetMusic(false); }
    public static void BossDefeated()
    {
        if (instance == null) return;
        instance.Play(instance.bossDefeat);
        instance.SetMusic(false);
    }
    private void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        GameSettings.EffectCategory category = GetEffectCategory(clip);
        if (pitch == 1f)
        {
            effectsSource.PlayOneShot(clip, volume * GameSettings.GetEffectCategoryMultiplier(category));
            return;
        }

        var oneShotObject = new GameObject("Temporary Audio One Shot");
        oneShotObject.transform.SetParent(transform, false);
        var source = CreateManagedSource(oneShotObject, false, volume, category);
        source.pitch = pitch;
        source.clip = clip;
        source.Play();
        Destroy(oneShotObject, clip.length / Mathf.Abs(pitch));
    }

    private GameSettings.EffectCategory GetEffectCategory(AudioClip clip)
    {
        if (clip == frag || clip == nuke || clip == molotovShatter || clip == flashExplosion ||
            clip == empBlast || clip == toxicSpitterImpact)
            return GameSettings.EffectCategory.Explosions;

        if (clip == buttonClick || clip == hover || clip == purchase || clip == purchaseDenied ||
            clip == upgradeUnlocked || clip == tabOpen || clip == toggleSelect)
            return GameSettings.EffectCategory.Interface;

        if (clip == shotgun || clip == ak || clip == minigun || clip == flame || clip == reload ||
            clip == gunOverheatStart || clip == flameOverheatStart || clip == overheatSound ||
            clip == overheatVent || clip == overheatReady || clip == laserLoop || clip == laserStart ||
            clip == laserStop || clip == lightningArc || clip == lightningChain ||
            clip == toxicSpitterFire || clip == miniTurretFire)
            return GameSettings.EffectCategory.Shooting;

        if (clip == grenadeThrow || clip == smokeStart || clip == smokeLoop || clip == molotovLoop ||
            clip == flashRinging || clip == pulsarActivate || clip == pulsarPulse || clip == nukeLaunch ||
            clip == overchargeActivate || clip == freezeBurstActivate || clip == spikeBarrierHit ||
            clip == reflectorRicochet || clip == empCharge || clip == toxicFloodActivate ||
            clip == abilityReady || clip == toxicPoolLoop)
            return GameSettings.EffectCategory.Abilities;

        return GameSettings.EffectCategory.World;
    }

    public static void Purchase() { if (instance != null) instance.Play(instance.purchase, 0.5f); }
    public static void PurchaseDenied() { if (instance != null) instance.Play(instance.purchaseDenied, 0.5f); }
    public static void ButtonClick() { if (instance != null) instance.Play(instance.buttonClick, 0.45f); }
    public static void Reload() { if (instance != null) instance.Play(instance.reload, 1f, 0.75f); }
    public static void Overheated(bool flamethrower)
    {
        if (instance != null)
            instance.StartCoroutine(instance.PlayOverheatSequence(flamethrower));
    }

    private IEnumerator PlayOverheatSequence(bool flamethrower)
    {
        AudioClip intro = flamethrower ? flameOverheatStart : gunOverheatStart;
        if (intro != null)
        {
            Play(intro, 0.55f);
            yield return new WaitForSecondsRealtime(intro.length);
        }

        Play(overheatVent, 0.22f);
        Play(overheatSound, 0.42f);
    }
    public static void Explosion(bool isNuke)
    {
        if (instance == null)
            return;

        instance.Play(isNuke ? instance.nuke : instance.frag);
        if (GameSettings.ScreenShake > 0f)
            CodeMonkey.Utils.UtilsClass.ShakeCamera(
                (isNuke ? 0.45f : 0.18f) * GameSettings.ScreenShake,
                isNuke ? 0.45f : 0.22f);
    }
    public static void Shotgun() { if (instance != null) instance.Play(instance.shotgun); }
    public static void PlayerHurt() { if (instance != null) instance.Play(instance.playerHurt, 0.35f); }
    public static void PlayerDeath() { if (instance != null) instance.Play(instance.playerDeath); }
    public static void PlayerDodged() { if (instance != null) instance.Play(instance.dodge, 0.55f); }
    public static void DefenceHit() { if (instance != null) instance.Play(instance.defenceHit, 0.45f); }
    public static void GrenadeThrown() { if (instance != null) instance.Play(instance.grenadeThrow, 0.6f); }
    public static void PulsarActivated() { if (instance != null) instance.Play(instance.pulsarActivate, 0.65f); }
    public static void PulsarPulsed() { if (instance != null) instance.Play(instance.pulsarPulse, 0.3f); }
    public static void NukeLaunched() { if (instance != null) instance.Play(instance.nukeLaunch, 0.75f); }
    public static void WaveCompleted() { if (instance != null) instance.Play(instance.waveComplete, 0.65f); }
    public static void DefenceBroken() { if (instance != null) instance.Play(instance.defenceBreak, 0.65f); }
    public static void LowHealthWarning() { if (instance != null) instance.Play(instance.lowHealthWarning, 0.5f); }
    public static void OverheatCooled() { if (instance != null) instance.Play(instance.overheatReady, 0.28f); }
    public static void HugeWaveIncoming() { if (instance != null) instance.Play(instance.hugeWaveWarning, 0.48f); }
    public static void UpgradeUnlocked() { if (instance != null) instance.Play(instance.upgradeUnlocked, 0.34f); }
    public static void TabOpened() { if (instance != null) instance.Play(instance.tabOpen, 0.22f); }

    public static void ToggleSelected()
    {
        if (instance == null || Time.unscaledTime < instance.nextToggle) return;
        instance.nextToggle = Time.unscaledTime + 0.06f;
        instance.Play(instance.toggleSelect, 0.18f);
    }

    public static void AbilityBecameReady()
    {
        if (instance == null || Time.unscaledTime < instance.nextAbilityReady) return;
        instance.nextAbilityReady = Time.unscaledTime + 0.12f;
        instance.Play(instance.abilityReady, 0.25f);
    }

    public static void SetDefenceRegenerating(bool active)
    {
        if (instance == null || instance.defenceRegenSource == null)
            return;

        AudioSource source = instance.defenceRegenSource;
        if (!active || instance.defenceRecharging == null)
        {
            if (source.isPlaying)
                source.Stop();
            return;
        }

        if (source.clip != instance.defenceRecharging)
            source.clip = instance.defenceRecharging;
        SetManagedSourceBaseVolume(source, 0.11f, GameSettings.EffectCategory.World);
        source.loop = true;
        if (!source.isPlaying)
            source.Play();
    }

    public static void DefenceFullyRecharged()
    {
        if (instance == null) return;
        SetDefenceRegenerating(false);
        instance.Play(instance.defenceFull, 0.3f);
    }

    public static void SmokeStarted(Vector2 position, float duration)
    {
        if (instance == null) return;
        instance.Play(instance.smokeStart);
        instance.PlayLoop(instance.smokeLoop, position, duration, 0.35f);
    }

    public static void MolotovStarted(Vector2 position, float duration)
    {
        if (instance == null) return;
        instance.Play(instance.molotovShatter, 0.8f);
        instance.PlayLoop(instance.molotovLoop, position, duration, 0.4f);
    }

    public static void FlashStarted(Vector2 position, float duration)
    {
        if (instance == null) return;
        instance.Play(instance.flashExplosion, 0.85f);
        instance.PlayLoop(instance.flashRinging, position, duration, 0.6f, 1f);
    }

    private void PlayLoop(AudioClip clip, Vector2 position, float duration, float volume, float segmentLength = 0f)
    {
        if (clip == null || duration <= 0f) return;
        var loopObject = new GameObject("Temporary Audio Loop");
        loopObject.transform.position = position;
        GameSettings.EffectCategory category = GetEffectCategory(clip);
        var source = CreateManagedSource(loopObject, true, volume, category);
        if (segmentLength > 0f)
        {
            source.loop = false;
            segmentLength = Mathf.Min(segmentLength, clip.length);
        }
        source.clip = clip;
        source.Play();
        var lifetime = loopObject.AddComponent<AudioLoopLifetime>();
        lifetime.Initialize(source, duration, segmentLength, volume, category);
    }

    public static void EnemyHit(bool died)
    {
        if (instance == null) return;
        float now = Time.unscaledTime;
        if (died)
        {
            if (now < instance.nextDeath) return;
            instance.nextDeath = now + 0.08f;
            instance.Play(instance.death);
        }
        else
        {
            if (now < instance.nextHit) return;
            instance.nextHit = now + 0.1f;
            instance.Play(instance.hit);
        }
    }

    public static void Hover()
    {
        if (instance == null || Time.unscaledTime < instance.nextHover) return;
        instance.nextHover = Time.unscaledTime + 0.08f;
        instance.Play(instance.hover, 0.3f);
    }

    public static AudioClip WeaponLoop(bool flamethrower, bool isMinigun)
    {
        if (instance == null) return null;
        return flamethrower ? instance.flame : isMinigun ? instance.minigun : instance.ak;
    }

    public static bool ConfigurePrototypeWeaponLoop(AudioSource source, PrototypePrimaryWeapon weapon)
    {
        if (instance == null || source == null)
            return false;

        AudioClip clip;
        float volume;
        float pitch;
        switch (weapon)
        {
            case PrototypePrimaryWeapon.RapidBullets:
                clip = instance.ak;
                volume = 0.24f;
                pitch = 1f;
                break;
            case PrototypePrimaryWeapon.PiercingLaser:
                clip = instance.laserLoop;
                volume = 0.18f;
                pitch = 1f;
                break;
            case PrototypePrimaryWeapon.Flamethrower:
                clip = instance.flame;
                volume = 0.34f;
                pitch = 1f;
                break;
            default:
                return false;
        }

        source.Stop();
        source.clip = clip;
        SetManagedSourceBaseVolume(source, volume, GameSettings.EffectCategory.Shooting);
        source.pitch = pitch;
        source.loop = true;
        return clip != null;
    }

    public static void PrototypeWeaponShot(PrototypePrimaryWeapon weapon)
    {
        if (instance == null)
            return;

        switch (weapon)
        {
            case PrototypePrimaryWeapon.ShotgunBurst:
                instance.Play(instance.shotgun, 0.8f);
                break;
            case PrototypePrimaryWeapon.LightningArc:
                instance.Play(instance.lightningArc, 0.28f);
                break;
            case PrototypePrimaryWeapon.ToxicSpitter:
                instance.Play(instance.toxicSpitterFire, 0.25f);
                break;
        }
    }

    public static void PrototypeWeaponLoopStarted(PrototypePrimaryWeapon weapon)
    {
        if (instance == null) return;
        if (weapon == PrototypePrimaryWeapon.PiercingLaser)
            instance.Play(instance.laserStart, 0.28f);
        else if (weapon == PrototypePrimaryWeapon.Flamethrower)
            instance.Play(instance.flamethrowerIgnite, 0.22f);
    }

    public static void PrototypeWeaponLoopStopped(PrototypePrimaryWeapon weapon)
    {
        if (instance == null) return;
        if (weapon == PrototypePrimaryWeapon.PiercingLaser)
            instance.Play(instance.laserStop, 0.2f);
        else if (weapon == PrototypePrimaryWeapon.Flamethrower)
            instance.Play(instance.flamethrowerStop, 0.18f);
    }

    public static void LightningChained()
    {
        if (instance == null || Time.unscaledTime < instance.nextLightningChain) return;
        instance.nextLightningChain = Time.unscaledTime + 0.06f;
        instance.Play(instance.lightningChain, 0.16f);
    }

    public static void ToxicSpitterImpacted()
    {
        if (instance != null)
            instance.Play(instance.toxicSpitterImpact, 0.27f);
    }

    public static void ToxicPoolStarted(Vector2 position, float duration)
    {
        if (instance != null)
            instance.PlayLoop(instance.toxicPoolLoop, position, duration, 0.12f);
    }

    public static void IceSlimeShattered() { if (instance != null) instance.Play(instance.iceSlimeShatter, 0.28f); }
    public static void AcidSlimeAttacked() { if (instance != null) instance.Play(instance.acidSlimeSizzle, 0.16f); }
    public static void ElectricSlimeAttacked() { if (instance != null) instance.Play(instance.electricSlimeCharge, 0.2f); }

    public static void ToxicSlimeTrailed()
    {
        if (instance == null || Time.unscaledTime < instance.nextToxicTrail) return;
        instance.nextToxicTrail = Time.unscaledTime + 2.5f;
        instance.Play(instance.toxicSlimeTrail, 0.07f);
    }

    public static void MegaSlimeSpawned() { if (instance != null) instance.Play(instance.megaSlimeSpawn, 0.38f); }
    public static void MegaSlimeSplit() { if (instance != null) instance.Play(instance.megaSlimeSplit, 0.3f); }
    public static void MiniSlimesSpawned() { if (instance != null) instance.Play(instance.miniSlimeSpawn, 0.18f); }

    public static void MiniTurretFired()
    {
        if (instance == null || Time.unscaledTime < instance.nextMiniTurret) return;
        instance.nextMiniTurret = Time.unscaledTime + 0.08f;
        instance.Play(instance.miniTurretFire, 0.12f);
    }

    public static void SpikeBarrierStruck()
    {
        if (instance == null || Time.unscaledTime < instance.nextSpikeHit) return;
        instance.nextSpikeHit = Time.unscaledTime + 0.12f;
        instance.Play(instance.spikeBarrierHit, 0.16f);
    }

    public static void OverchargeActivated() { if (instance != null) instance.Play(instance.overchargeActivate, 0.3f); }
    public static void FreezeBurstActivated() { if (instance != null) instance.Play(instance.freezeBurstActivate, 0.3f); }

    public static void ReflectorRicocheted()
    {
        if (instance == null || Time.unscaledTime < instance.nextRicochet) return;
        instance.nextRicochet = Time.unscaledTime + 0.08f;
        instance.Play(instance.reflectorRicochet, 0.15f);
    }

    public static void EmpActivated()
    {
        if (instance != null)
            instance.StartCoroutine(instance.PlayEmpSequence());
    }

    private IEnumerator PlayEmpSequence()
    {
        Play(empCharge, 0.3f);
        yield return new WaitForSecondsRealtime(0.14f);
        Play(empBlast, 0.42f);
    }

    public static void ToxicFloodStarted(Vector2 position, float duration)
    {
        if (instance == null) return;
        instance.Play(instance.toxicFloodActivate, 0.32f);
        instance.PlayLoop(instance.toxicPoolLoop, position, duration, 0.1f);
    }
}

public class AudioLoopLifetime : MonoBehaviour
{
    private AudioSource source;
    private float remaining;
    private float fadeDuration;
    private float baseVolume;
    private float loopSegmentLength;
    private GameSettings.EffectCategory category;

    public void Initialize(AudioSource audioSource, float duration, float segmentLength,
        float sourceBaseVolume, GameSettings.EffectCategory sourceCategory)
    {
        source = audioSource;
        remaining = Mathf.Max(0f, duration);
        fadeDuration = Mathf.Min(1f, remaining);
        baseVolume = sourceBaseVolume;
        loopSegmentLength = segmentLength;
        category = sourceCategory;
    }

    private void Update()
    {
        remaining -= Time.unscaledDeltaTime;
        if (loopSegmentLength > 0f && source != null && source.time >= loopSegmentLength)
        {
            source.time = 0f;
            source.Play();
        }
        if (remaining <= fadeDuration && fadeDuration > 0f)
        {
            if (source != null)
                source.volume = baseVolume * GameSettings.GetEffectSourceMultiplier(category) *
                                Mathf.Clamp01(remaining / fadeDuration);
        }
        else if (source != null)
            source.volume = baseVolume * GameSettings.GetEffectSourceMultiplier(category);
        if (remaining <= 0f)
            Destroy(gameObject);
    }
}

public class ButtonAudioHover : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IPointerClickHandler, ISubmitHandler
{
    private UnityEngine.UI.Selectable selectable;
    private bool usesSpecificPurchaseSound;

    private void Awake() { selectable = GetComponent<UnityEngine.UI.Selectable>(); }
    private void Start()
    {
        // Purchase buttons already play their own confirmation sound.
        usesSpecificPurchaseSound |= GetComponent<BuyGrenadeButton>() != null || GetComponent<skills>() != null;
    }

    public void SuppressClickSound() { usesSpecificPurchaseSound = true; }

    private void Play() { if (selectable != null && selectable.IsActive() && selectable.IsInteractable()) BackgroundMusic.Hover(); }
    private void PlayClick()
    {
        if (selectable == null || !selectable.IsActive() || !selectable.IsInteractable() || usesSpecificPurchaseSound)
            return;

        if (selectable is UnityEngine.UI.Toggle)
            BackgroundMusic.ToggleSelected();
        else
            BackgroundMusic.ButtonClick();
    }

    public void OnPointerEnter(PointerEventData data) { Play(); }
    public void OnSelect(BaseEventData data) { Play(); }
    public void OnPointerClick(PointerEventData data)
    {
        if (data.button == PointerEventData.InputButton.Left) PlayClick();
    }
    public void OnSubmit(BaseEventData data) { PlayClick(); }
}
