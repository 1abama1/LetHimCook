using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[System.Serializable]
public class SoundEntry
{
    public string key;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
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

    public float MusicVolume => musicVolume;

    private AudioSource _musicSource;
    private AudioSource[] _sfxSources;
    private int _nextSource;
    private readonly Dictionary<string, SoundEntry> _sfxMap = new Dictionary<string, SoundEntry>();
    private GameManager _boundGameManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicVolume = PlayerPrefs.GetFloat("MusicVolume", musicVolume);

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.volume = musicVolume;

        CreatePool();

        foreach (SoundEntry entry in sfxList)
        {
            if (!string.IsNullOrEmpty(entry.key) && !_sfxMap.ContainsKey(entry.key))
                _sfxMap[entry.key] = entry;
        }
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

    private void OnNightStarted() => PlayMusic(nightMusic);
    private void OnDayStarted() => PlayMusic(dayMusic);

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || _musicSource.clip == clip)
            return;

        _musicSource.clip = clip;
        _musicSource.Play();
    }

    public void StopMusic()
    {
        if (_musicSource != null)
            _musicSource.Stop();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);

        if (_musicSource != null)
            _musicSource.volume = musicVolume;
    }

    private void CreatePool()
    {
        _sfxSources = new AudioSource[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = new GameObject($"SFX_Source_{i}");
            go.transform.SetParent(transform);
            _sfxSources[i] = go.AddComponent<AudioSource>();
            _sfxSources[i].playOnAwake = false;
        }
    }

    private AudioSource GetAvailableSource(bool forLoop = false)
    {
        for (int i = 0; i < _sfxSources.Length; i++)
        {
            int idx = (_nextSource + i) % _sfxSources.Length;
            AudioSource src = _sfxSources[idx];

            if (!forLoop && src.loop && src.isPlaying)
                continue;

            if (!src.isPlaying)
            {
                _nextSource = (idx + 1) % _sfxSources.Length;
                return src;
            }
        }

        for (int i = 0; i < _sfxSources.Length; i++)
        {
            int idx = (_nextSource + i) % _sfxSources.Length;

            if (forLoop || !_sfxSources[idx].loop)
            {
                _nextSource = (idx + 1) % _sfxSources.Length;
                return _sfxSources[idx];
            }
        }

        AudioSource fallback = _sfxSources[_nextSource];
        _nextSource = (_nextSource + 1) % _sfxSources.Length;
        return fallback;
    }

    private static void ResetSource(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        source.loop = false;
        source.volume = 1f;
        source.pitch = 1f;
        source.spatialBlend = 0f;
        source.dopplerLevel = 1f;
        source.minDistance = 1f;
        source.maxDistance = 500f;
        source.transform.localPosition = Vector3.zero;
    }

    private static float GetRandomPitch(SoundEntry entry)
    {
        float pVar = Mathf.Max(0.1f, entry.pitchVariance);
        return Random.Range(1f / pVar, pVar);
    }

    public void PlayClip2D(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null)
            return;

        AudioSource source = GetAvailableSource(false);
        ResetSource(source);

        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = 0f;
        source.loop = false;
        source.Play();
    }

    public void Play(string key)
    {
        if (!_sfxMap.TryGetValue(key, out SoundEntry entry) || entry.clip == null)
            return;

        AudioSource source = GetAvailableSource(entry.loop);
        ResetSource(source);

        source.clip = entry.clip;
        source.volume = entry.volume;
        source.pitch = GetRandomPitch(entry);
        source.spatialBlend = 0f;
        source.loop = entry.loop;
        source.Play();
    }

    public void PlayAt(string key, Vector3 position)
    {
        if (!_sfxMap.TryGetValue(key, out SoundEntry entry) || entry.clip == null)
            return;

        AudioSource source = GetAvailableSource(false);
        ResetSource(source);

        source.transform.position = position;
        source.clip = entry.clip;
        source.volume = entry.volume;
        source.pitch = GetRandomPitch(entry);
        source.spatialBlend = 1f;
        source.loop = false;
        source.Play();
    }

    public AudioSource PlayLoop(string key, Vector3 position)
    {
        if (!_sfxMap.TryGetValue(key, out SoundEntry entry) || entry.clip == null)
            return null;

        AudioSource source = GetAvailableSource(true);
        ResetSource(source);

        source.transform.position = position;
        source.clip = entry.clip;
        source.volume = entry.volume;
        source.pitch = GetRandomPitch(entry);
        source.spatialBlend = 1f;
        source.loop = true;
        source.Play();
        return source;
    }

    public static void StopLoop(AudioSource source)
    {
        if (source == null)
            return;

        source.Stop();
        source.loop = false;
    }
}
