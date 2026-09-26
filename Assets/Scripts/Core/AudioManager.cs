using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[System.Serializable]
public class SoundEntry
{
    public string key;
    public AudioClip clip;
    [Range(0f, 1f)]  public float volume        = 1f;
    [Range(0.8f, 1.2f)] public float pitchVariance = 1f;
    public bool loop = false;
}

public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX")]
    [SerializeField] private List<SoundEntry> sfxList = new List<SoundEntry>();
    [SerializeField] private int poolSize = 16;

    [Header("Music")]
    [SerializeField] private AudioClip nightMusic;
    [SerializeField] private AudioClip dayMusic;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.4f;

    /// <summary>Текущая громкость музыки. Read-only снаружи — используй SetMusicVolume() для записи.</summary>
    public float MusicVolume => musicVolume;

    private AudioSource _musicSource;
    private AudioSource[] _sfxSources;
    private int _nextSource;

    private readonly Dictionary<string, SoundEntry> _sfxMap = new Dictionary<string, SoundEntry>();

    private GameManager _boundGameManager;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Music source
        _musicSource        = gameObject.AddComponent<AudioSource>();
        _musicSource.loop   = true;
        _musicSource.volume = musicVolume;

        // SFX pool
        CreatePool();

        // SFX map
        foreach (SoundEntry entry in sfxList)
            if (!string.IsNullOrEmpty(entry.key) && !_sfxMap.ContainsKey(entry.key))
                _sfxMap[entry.key] = entry;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        RebindGameManager();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindGameManager();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scene lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindGameManager();
    }

    private void RebindGameManager()
    {
        UnbindGameManager();

        _boundGameManager = GameManager.Instance;

        if (_boundGameManager == null)
            return;

        _boundGameManager.onNightBegin.AddListener(OnNightStarted);
        _boundGameManager.onDayBegin.AddListener(OnDayStarted);

        // Play correct music immediately based on current phase.
        if (_boundGameManager.currentPhase == GamePhase.Night)
            OnNightStarted();
        else
            OnDayStarted();
    }

    private void UnbindGameManager()
    {
        if (_boundGameManager == null)
            return;

        _boundGameManager.onNightBegin.RemoveListener(OnNightStarted);
        _boundGameManager.onDayBegin.RemoveListener(OnDayStarted);
        _boundGameManager = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Music
    // ─────────────────────────────────────────────────────────────────────────

    private void OnNightStarted() => PlayMusic(nightMusic);
    private void OnDayStarted()   => PlayMusic(dayMusic);

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || _musicSource.clip == clip) return;
        _musicSource.clip = clip;
        _musicSource.Play();
    }

    public void StopMusic()                   => _musicSource.Stop();
    public void SetMusicVolume(float v)        => _musicSource.volume = v;

    // ─────────────────────────────────────────────────────────────────────────
    // AudioSource pool
    // ─────────────────────────────────────────────────────────────────────────

    private void CreatePool()
    {
        _sfxSources = new AudioSource[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = new GameObject($"SFX_Source_{i}");
            go.transform.SetParent(transform);
            _sfxSources[i]            = go.AddComponent<AudioSource>();
            _sfxSources[i].playOnAwake = false;
        }
    }

    /// <summary>
    /// Returns a non-playing source. Loops get their own reserved slot — never
    /// returned by this method to avoid being stomped by a one-shot.
    /// Falls back to stealing the oldest one-shot source if all are busy.
    /// </summary>
    private AudioSource GetAvailableSource(bool forLoop = false)
    {
        for (int i = 0; i < _sfxSources.Length; i++)
        {
            int idx = (_nextSource + i) % _sfxSources.Length;
            AudioSource src = _sfxSources[idx];

            // Skip looping sources when looking for a one-shot slot.
            if (!forLoop && src.loop && src.isPlaying)
                continue;

            if (!src.isPlaying)
            {
                _nextSource = (idx + 1) % _sfxSources.Length;
                return src;
            }
        }

        // Pool is full — steal the next non-loop source (or any source for a loop).
        for (int i = 0; i < _sfxSources.Length; i++)
        {
            int idx = (_nextSource + i) % _sfxSources.Length;
            if (forLoop || !_sfxSources[idx].loop)
            {
                _nextSource = (idx + 1) % _sfxSources.Length;
                return _sfxSources[idx];
            }
        }

        // All sources are looping — steal the oldest one as last resort.
        AudioSource fallback = _sfxSources[_nextSource];
        _nextSource = (_nextSource + 1) % _sfxSources.Length;
        return fallback;
    }

    /// <summary>Resets all mutable AudioSource fields to neutral defaults.</summary>
    private static void ResetSource(AudioSource source)
    {
        source.Stop();
        source.clip          = null;
        source.loop          = false;
        source.volume        = 1f;
        source.pitch         = 1f;
        source.spatialBlend  = 0f;
        source.dopplerLevel  = 1f;
        source.minDistance   = 1f;
        source.maxDistance   = 500f;
        source.transform.localPosition = Vector3.zero;
    }

    private static float GetRandomPitch(SoundEntry entry)
    {
        float pVar = Mathf.Max(0.1f, entry.pitchVariance);
        return Random.Range(1f / pVar, pVar);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public SFX API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Plays a non-spatial (2D) one-shot SFX.</summary>
    public void Play(string key)
    {
        if (!_sfxMap.TryGetValue(key, out SoundEntry entry) || entry.clip == null)
            return;

        AudioSource source = GetAvailableSource(entry.loop);
        ResetSource(source);

        source.clip         = entry.clip;
        source.volume       = entry.volume;
        source.pitch        = GetRandomPitch(entry);
        source.spatialBlend = 0f;
        source.loop         = entry.loop;

        source.Play();
    }

    /// <summary>Plays a spatial (3D) one-shot SFX at world position.</summary>
    public void PlayAt(string key, Vector3 position)
    {
        if (!_sfxMap.TryGetValue(key, out SoundEntry entry) || entry.clip == null)
            return;

        AudioSource source = GetAvailableSource(forLoop: false);
        ResetSource(source);

        source.transform.position = position;
        source.clip               = entry.clip;
        source.volume             = entry.volume;
        source.pitch              = GetRandomPitch(entry);
        source.spatialBlend       = 1f;
        source.loop               = false;

        source.Play();
    }

    /// <summary>
    /// Starts a looping SFX and returns the AudioSource so the caller can Stop it.
    /// Caller is responsible for calling StopLoop() when done.
    /// </summary>
    public AudioSource PlayLoop(string key, Vector3 position)
    {
        if (!_sfxMap.TryGetValue(key, out SoundEntry entry) || entry.clip == null)
            return null;

        AudioSource source = GetAvailableSource(forLoop: true);
        ResetSource(source);

        source.transform.position = position;
        source.clip               = entry.clip;
        source.volume             = entry.volume;
        source.pitch              = GetRandomPitch(entry);
        source.spatialBlend       = 1f;
        source.loop               = true;

        source.Play();
        return source;
    }

    /// <summary>Convenience: stops a looping source returned by PlayLoop().</summary>
    public static void StopLoop(AudioSource source)
    {
        if (source == null) return;
        source.Stop();
        source.loop = false;
    }
}
