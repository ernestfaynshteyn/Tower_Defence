using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class BackgroundMusic : MonoBehaviour
{
    public AudioClip music, bossMusic, bossEncounter, wave, purchase, hover;
    public AudioClip hit, death, frag, nuke, shotgun, ak, minigun, flame;
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
    private void Play(AudioClip clip) { if (clip != null) effectsSource.PlayOneShot(clip); }
    public static void Purchase() { if (instance != null) instance.Play(instance.purchase); }
    public static void Explosion(bool isNuke) { if (instance != null) instance.Play(isNuke ? instance.nuke : instance.frag); }
    public static void Shotgun() { if (instance != null) instance.Play(instance.shotgun); }

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

public class ButtonAudioHover : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private UnityEngine.UI.Button button;
    private void Awake() { button = GetComponent<UnityEngine.UI.Button>(); }
    private void Play() { if (button != null && button.IsActive() && button.IsInteractable()) BackgroundMusic.Hover(); }
    public void OnPointerEnter(PointerEventData data) { Play(); }
    public void OnSelect(BaseEventData data) { Play(); }
}
