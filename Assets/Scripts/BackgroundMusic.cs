using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class BackgroundMusic : MonoBehaviour
{
    public AudioClip music, bossMusic, bossEncounter, wave, purchase, hover;
    public AudioClip hit, death, playerHurt, playerDeath, purchaseDenied;
    public AudioClip buttonClick, reload;
    public AudioClip gunOverheatStart, flameOverheatStart, overheatSound;
    public AudioClip frag, nuke, shotgun, ak, minigun, flame;
    public AudioClip smokeStart, smokeLoop, molotovLoop, flashRinging;
    private static BackgroundMusic instance;
    private AudioSource musicSource, effectsSource;
    private float nextHit, nextDeath, nextHover;

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
        musicSource = CreateSource(gameObject, true, 0.35f);
        effectsSource = CreateSource(gameObject, false, 0.65f);
        SceneManager.sceneLoaded += SceneLoaded;
        SetMusic(false);
    }

    public static AudioSource CreateSource(GameObject owner, bool loop, float volume)
    {
        var source = owner.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = volume;
        return source;
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetMusic(false);
        foreach (var button in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (button.gameObject.scene == scene && button.GetComponent<ButtonAudioHover>() == null)
                button.gameObject.AddComponent<ButtonAudioHover>();
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= SceneLoaded;
        instance = null;
    }

    private void SetMusic(bool boss)
    {
        AudioClip clip = boss && bossMusic != null ? bossMusic : music;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        if (clip != null) musicSource.Play();
    }

    public static void WaveStarted(bool boss)
    {
        if (instance == null) return;
        instance.SetMusic(boss);
        instance.Play(boss ? instance.bossEncounter : instance.wave);
    }

    public static void WaveEnded() { if (instance != null) instance.SetMusic(false); }
    private void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        if (pitch == 1f)
        {
            effectsSource.PlayOneShot(clip, volume);
            return;
        }

        var oneShotObject = new GameObject("Temporary Audio One Shot");
        oneShotObject.transform.SetParent(transform, false);
        var source = CreateSource(oneShotObject, false, volume);
        source.pitch = pitch;
        source.clip = clip;
        source.Play();
        Destroy(oneShotObject, clip.length / Mathf.Abs(pitch));
    }

    public static void Purchase() { if (instance != null) instance.Play(instance.purchase); }
    public static void PurchaseDenied() { if (instance != null) instance.Play(instance.purchaseDenied); }
    public static void ButtonClick() { if (instance != null) instance.Play(instance.buttonClick); }
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
            Play(intro);
            yield return new WaitForSecondsRealtime(intro.length);
        }

        Play(overheatSound);
    }
    public static void Explosion(bool isNuke) { if (instance != null) instance.Play(isNuke ? instance.nuke : instance.frag); }
    public static void Shotgun() { if (instance != null) instance.Play(instance.shotgun); }
    public static void PlayerHurt() { if (instance != null) instance.Play(instance.playerHurt, 0.35f); }
    public static void PlayerDeath() { if (instance != null) instance.Play(instance.playerDeath); }

    public static void SmokeStarted(Vector2 position, float duration)
    {
        if (instance == null) return;
        instance.Play(instance.smokeStart);
        instance.PlayLoop(instance.smokeLoop, position, duration, 0.35f);
    }

    public static void MolotovStarted(Vector2 position, float duration)
    {
        if (instance != null) instance.PlayLoop(instance.molotovLoop, position, duration, 0.4f);
    }

    public static void FlashStarted(Vector2 position, float duration)
    {
        if (instance != null) instance.PlayLoop(instance.flashRinging, position, duration, 0.3f, 1f);
    }

    private void PlayLoop(AudioClip clip, Vector2 position, float duration, float volume, float segmentLength = 0f)
    {
        if (clip == null || duration <= 0f) return;
        var loopObject = new GameObject("Temporary Audio Loop");
        loopObject.transform.position = position;
        var source = CreateSource(loopObject, true, volume);
        if (segmentLength > 0f)
        {
            source.loop = false;
            segmentLength = Mathf.Min(segmentLength, clip.length);
        }
        source.clip = clip;
        source.Play();
        var lifetime = loopObject.AddComponent<AudioLoopLifetime>();
        lifetime.Initialize(source, duration, segmentLength);
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
        instance.Play(instance.hover);
    }

    public static AudioClip WeaponLoop(bool flamethrower, bool isMinigun)
    {
        if (instance == null) return null;
        return flamethrower ? instance.flame : isMinigun ? instance.minigun : instance.ak;
    }
}

public class AudioLoopLifetime : MonoBehaviour
{
    private AudioSource source;
    private float remaining;
    private float fadeDuration;
    private float initialVolume;
    private float loopSegmentLength;

    public void Initialize(AudioSource audioSource, float duration, float segmentLength = 0f)
    {
        source = audioSource;
        remaining = Mathf.Max(0f, duration);
        fadeDuration = Mathf.Min(1f, remaining);
        initialVolume = source.volume;
        loopSegmentLength = segmentLength;
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
                source.volume = initialVolume * Mathf.Clamp01(remaining / fadeDuration);
        }
        if (remaining <= 0f)
            Destroy(gameObject);
    }
}

public class ButtonAudioHover : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IPointerClickHandler, ISubmitHandler
{
    private UnityEngine.UI.Button button;
    private bool usesSpecificPurchaseSound;

    private void Awake() { button = GetComponent<UnityEngine.UI.Button>(); }
    private void Start()
    {
        // Purchase buttons already play their own confirmation sound.
        usesSpecificPurchaseSound = GetComponent<BuyGrenadeButton>() != null || GetComponent<skills>() != null;
    }

    private void Play() { if (button != null && button.IsActive() && button.IsInteractable()) BackgroundMusic.Hover(); }
    private void PlayClick()
    {
        if (button == null || !button.IsActive() || !button.IsInteractable() || usesSpecificPurchaseSound)
            return;
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
