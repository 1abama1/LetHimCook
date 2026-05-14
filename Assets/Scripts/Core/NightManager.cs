using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Менеджер ночной фазы.
/// Активирует ночные объекты (Коп, Баки, Бомжи), выключает дневные.
/// Подпишитесь через GameManager.onNightBegin → OnNightBegin()
/// или назначьте автоматически в Start().
/// </summary>
public class NightManager : MonoBehaviour
{
    public static NightManager Instance { get; private set; }

    [Header("Night-only Objects")]
    [Tooltip("Все объекты ночной сцены: Коп, Мусорки, Бомжи")]
    public GameObject[] nightObjects;

    [Header("Day-only Objects")]
    [Tooltip("Объекты дня, которые нужно спрятать ночью: клиенты, стойки, инспектор")]
    public GameObject[] dayObjects;

    [Header("Ambient Light")]
    public Color  nightAmbientColor  = new Color(0.05f, 0.05f, 0.15f);
    public Color  dayAmbientColor    = new Color(0.8f,  0.75f, 0.6f);
    public float  ambientTransitionTime = 1.5f;

    [Header("Fog")]
    public bool  useNightFog        = true;
    public Color nightFogColor      = new Color(0.03f, 0.03f, 0.08f);
    public float nightFogDensity    = 0.04f;
    public float dayFogDensity      = 0.01f;

    [Header("Events")]
    public UnityEvent onNightStart;
    public UnityEvent onNightEnd;

    private bool _isNight;
    private Color _savedAmbient;
    private bool  _savedFog;
    private float _savedFogDensity;
    private Color _savedFogColor;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Сохраняем дневные настройки
        _savedAmbient   = RenderSettings.ambientLight;
        _savedFog       = RenderSettings.fog;
        _savedFogDensity= RenderSettings.fogDensity;
        _savedFogColor  = RenderSettings.fogColor;

        // Подписываемся на смену фаз
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onNightBegin.AddListener(OnNightBegin);
            GameManager.Instance.onDayBegin.AddListener(OnDayBegin);

            // Запускаем с текущей фазы
            if (GameManager.Instance.currentPhase == GamePhase.Night)
                OnNightBegin();
            else
                OnDayBegin();
        }
    }

    // ─── Ночная фаза ─────────────────────────────────────────────────────────
    public void OnNightBegin()
    {
        _isNight = true;

        foreach (var obj in nightObjects) if (obj != null) obj.SetActive(true);
        foreach (var obj in dayObjects)   if (obj != null) obj.SetActive(false);

        StartCoroutine(TransitionAmbient(dayAmbientColor, nightAmbientColor));

        if (useNightFog)
        {
            RenderSettings.fog         = true;
            RenderSettings.fogColor    = nightFogColor;
            RenderSettings.fogDensity  = nightFogDensity;
            RenderSettings.fogMode     = FogMode.Exponential;
        }

        onNightStart?.Invoke();
        AudioManager.Instance?.Play("night_start");
    }

    // ─── Дневная фаза ─────────────────────────────────────────────────────────
    public void OnDayBegin()
    {
        _isNight = false;

        foreach (var obj in nightObjects) if (obj != null) obj.SetActive(false);
        foreach (var obj in dayObjects)   if (obj != null) obj.SetActive(true);

        StartCoroutine(TransitionAmbient(nightAmbientColor, dayAmbientColor));

        RenderSettings.fogDensity = dayFogDensity;

        onNightEnd?.Invoke();
        AudioManager.Instance?.Play("day_start");
    }

    // ─── Плавный переход освещения ────────────────────────────────────────────
    IEnumerator TransitionAmbient(Color from, Color to)
    {
        float t = 0f;
        while (t < ambientTransitionTime)
        {
            t += Time.deltaTime;
            RenderSettings.ambientLight = Color.Lerp(from, to, t / ambientTransitionTime);
            yield return null;
        }
        RenderSettings.ambientLight = to;
    }

    public bool IsNight => _isNight;
}
