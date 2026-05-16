using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum GamePhase { Night, Day }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Phase Settings")]
    public GamePhase currentPhase = GamePhase.Night;
    public float dayDurationSeconds  = 180f;
    public float nightDurationSeconds = 120f;

    [Header("Teleport Points")]
    [Tooltip("Координаты точки спавна днём")]
    public Vector3 daySpawnPosition;
    [Tooltip("Поворот игрока днём (Euler)")]
    public Vector3 daySpawnRotation;

    [Tooltip("Координаты точки спавна ночью")]
    public Vector3 nightSpawnPosition;
    [Tooltip("Поворот игрока ночью (Euler)")]
    public Vector3 nightSpawnRotation;

    [Header("Player")]
    [Tooltip("Перетащи сюда объект игрока из иерархии")]
    public Transform playerTransform;

    [Header("Transition")]
    [Tooltip("Скорость затемнения/осветления экрана")]
    public float fadeDuration = 0.6f;
    [Tooltip("Пауза на чёрном экране между телепортом (сек)")]
    public float holdBlackDuration = 0.3f;

    [Header("Events")]
    public UnityEvent onNightBegin;
    public UnityEvent onDayBegin;
    public UnityEvent onArrest;
    public UnityEvent onInspectorFine;

    [HideInInspector] public float phaseTimer;
    private bool _phaseActive;
    private bool _transitioning;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        FindPlayer();

        // Если есть StoryManager — только телепортируем, он сам запустит BeginPhase
        if (StoryManager.Instance != null)
        {
            TeleportPlayer(currentPhase);
        }
        else
        {
            BeginPhase(currentPhase);
        }
    }

    void Update()
    {
        if (!_phaseActive || _transitioning) return;
        
        // 1. Не тикаем, если игра закончена
        if (StoryManager.Instance != null && StoryManager.Instance.IsGameEnded)
        {
            _phaseActive = false;
            return;
        }

        // 2. Не тикаем, если StoryManager сейчас показывает итоги дня (пауза)
        if (StoryManager.Instance != null && StoryManager.Instance.IsShowingSummary)
        {
            return; 
        }

        phaseTimer -= Time.deltaTime;
        if (phaseTimer <= 0f) TransitionPhase();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Запуск фазы (без анимации — внутренний)
    // ─────────────────────────────────────────────────────────────────────────
    public void BeginPhase(GamePhase phase)
    {
        currentPhase  = phase;
        _phaseActive  = true;

        TeleportPlayer(phase);

        if (phase == GamePhase.Night)
        {
            phaseTimer = nightDurationSeconds;
            onNightBegin?.Invoke();
        }
        else
        {
            phaseTimer = dayDurationSeconds;
            onDayBegin?.Invoke();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Переход с затемнением
    // ─────────────────────────────────────────────────────────────────────────
    void TransitionPhase()
    {
        if (_transitioning) return;

        // Если игра закончилась, больше не переключаем фазы
        if (StoryManager.Instance != null && StoryManager.Instance.IsGameEnded)
            return;

        // Если это был День 3 (последний), то мы НЕ переходим к следующему дню автоматически.
        // Мы ждем, пока StoryManager вызовет концовку.
        if (currentPhase == GamePhase.Day && StoryManager.Instance != null)
        {
            if (StoryManager.Instance.CurrentDay >= StoryManager.Instance.totalDays)
            {
                // Финальный день закончен. Переходим в Ночь (формально), 
                // но StoryManager в onNightBegin поймает это и завершит игру.
                // Просто убедимся, что мы не уходим в бесконечный цикл.
            }
        }

        GamePhase next = (currentPhase == GamePhase.Night) ? GamePhase.Day : GamePhase.Night;
        StartCoroutine(FadeTransition(next));
    }

    IEnumerator FadeTransition(GamePhase next)
    {
        _transitioning = true;
        _phaseActive   = false;

        // ── Fade to black ──────────────────────────────────────────────────
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeToBlack(fadeDuration);

        // ── Телепорт пока экран чёрный ─────────────────────────────────────
        currentPhase = next;
        yield return StartCoroutine(TeleportPlayerCoroutine(next));

        yield return new WaitForSeconds(holdBlackDuration);

        // ── Запускаем новую фазу (события, таймер) ─────────────────────────
        _phaseActive = true;
        if (next == GamePhase.Night)
        {
            phaseTimer = nightDurationSeconds;
            onNightBegin?.Invoke();
        }
        else
        {
            phaseTimer = dayDurationSeconds;
            onDayBegin?.Invoke();
        }

        // ── Fade from black ────────────────────────────────────────────────
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeFromBlack(fadeDuration);

        _transitioning = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Арест — мгновенный скип ночи (тоже с затемнением)
    // ─────────────────────────────────────────────────────────────────────────
    public void TriggerArrest()
    {
        onArrest?.Invoke();
        PlayerEconomy.Instance?.LoseAllNightResources();

        if (currentPhase == GamePhase.Night && !_transitioning)
        {
            Debug.Log("[GameManager] Игрок арестован! Скипаем ночь.");
            _phaseActive = false;
            StartCoroutine(FadeTransition(GamePhase.Day));
        }
    }

    public void TriggerInspectorFine(int amount)
    {
        onInspectorFine?.Invoke();
        PlayerEconomy.Instance?.SpendMoney(amount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Телепортация
    // ─────────────────────────────────────────────────────────────────────────
    private void FindPlayer()
    {
        // Приоритет: прямая ссылка из инспектора
        if (playerTransform != null)
        {
            return; // уже есть
        }
        // Запасной вариант: поиск по тегу
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) playerTransform = go.transform;
    }

    /// <summary>
    /// Корутина телепорта: ждёт кадр после отключения CharacterController,
    /// иначе CC может проигнорировать изменение позиции в том же кадре.
    /// </summary>
    private IEnumerator TeleportPlayerCoroutine(GamePhase phase)
    {
        FindPlayer();
        if (playerTransform == null)
        {
            Debug.LogWarning("[GameManager] Игрок не найден! Назначь playerTransform в инспекторе.");
            yield break;
        }

        Vector3    targetPos = (phase == GamePhase.Day) ? daySpawnPosition  : nightSpawnPosition;
        Quaternion targetRot = (phase == GamePhase.Day)
            ? Quaternion.Euler(daySpawnRotation)
            : Quaternion.Euler(nightSpawnRotation);

        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Даём Unity время понять, что CC выключен
        yield return new WaitForSecondsRealtime(0.1f);

        playerTransform.position = targetPos;
        playerTransform.rotation = targetRot;

        Physics.SyncTransforms(); // Форсируем обновление физики
        yield return null; // кадр 2: позиция зафиксирована

        if (cc != null) cc.enabled = true;
        Debug.Log($"[GameManager] Игрок телепортирован (Coroutine) в {targetPos}");
    }

    /// <summary>Синхронная версия для Start() (без анимации).</summary>
    private void TeleportPlayer(GamePhase phase)
    {
        FindPlayer();
        if (playerTransform == null)
        {
            Debug.LogWarning("[GameManager] Игрок не найден! Назначь playerTransform в инспекторе.");
            return;
        }

        Vector3    targetPos = (phase == GamePhase.Day) ? daySpawnPosition  : nightSpawnPosition;
        Quaternion targetRot = (phase == GamePhase.Day)
            ? Quaternion.Euler(daySpawnRotation)
            : Quaternion.Euler(nightSpawnRotation);

        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        
        playerTransform.position = targetPos;
        playerTransform.rotation = targetRot;
        
        Physics.SyncTransforms(); // Форсируем обновление физики
        
        if (cc != null) cc.enabled = true;
        Debug.Log($"[GameManager] Игрок телепортирован (Sync) в {targetPos}");
    }

    // ─────────────────────────────────────────────────────────────────────────
}
