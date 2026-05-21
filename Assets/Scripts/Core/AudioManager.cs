using UnityEngine;
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

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX")]
    public List<SoundEntry> sfxList = new List<SoundEntry>();

    [Header("Music")]
    public AudioClip nightMusic;
    public AudioClip dayMusic;
    [Range(0f, 1f)] public float musicVolume = 0.4f;

    private AudioSource _musicSource;
    private Dictionary<string, SoundEntry> _sfxMap = new Dictionary<string, SoundEntry>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop   = true;
        _musicSource.volume = musicVolume;

        foreach (var entry in sfxList)
            if (!_sfxMap.ContainsKey(entry.key)) _sfxMap[entry.key] = entry;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onNightBegin.AddListener(() => PlayMusic(nightMusic));
            GameManager.Instance.onDayBegin.AddListener(()   => PlayMusic(dayMusic));
        }

        PlayMusic(GameManager.Instance?.currentPhase == GamePhase.Night ? nightMusic : dayMusic);
    }

    public void Play(string key)
    {
        if (!_sfxMap.TryGetValue(key, out SoundEntry entry)) return;
        if (entry.clip == null) return;

        GameObject go = new GameObject($"SFX_{key}");
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip   = entry.clip;
        src.volume = entry.volume;
        float pVar = Mathf.Max(0.1f, entry.pitchVariance);
        src.pitch  = Random.Range(1f / pVar, pVar);
        src.loop   = entry.loop;
        src.Play();

        if (!entry.loop) Destroy(go, entry.clip.length + 0.1f);
    }

    public void PlayAt(string key, Vector3 position)
    {
        if (!_sfxMap.TryGetValue(key, out SoundEntry entry)) return;
        if (entry.clip == null) return;
        AudioSource.PlayClipAtPoint(entry.clip, position, entry.volume);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || _musicSource.clip == clip) return;
        _musicSource.clip = clip;
        _musicSource.Play();
    }

    public void StopMusic() => _musicSource.Stop();

    public void SetMusicVolume(float v) => _musicSource.volume = v;
}
