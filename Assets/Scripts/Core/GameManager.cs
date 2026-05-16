using UnityEngine;
using UnityEngine.Events;

public enum GamePhase { Night, Day }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Phase Settings")]
    public GamePhase currentPhase = GamePhase.Night;
    public float dayDurationSeconds = 180f;
    public float nightDurationSeconds = 120f;

    [Header("Teleport Points")]
    [Tooltip("Пустышка (GameObject), куда телепортирует днем")]
    public Transform daySpawnPoint;
    [Tooltip("Пустышка (GameObject), куда телепортирует ночью")]
    public Transform nightSpawnPoint;

    private Transform _playerTransform;

    [Header("Events")]
    public UnityEvent onNightBegin;
    public UnityEvent onDayBegin;
    public UnityEvent onArrest;
    public UnityEvent onInspectorFine;

    [HideInInspector] public float phaseTimer;
    private bool _phaseActive;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        FindPlayer();

        // Если в сцене есть StoryManager — он сам вызовет BeginPhase после предыстории.
        // Но игрока телепортируем сразу, чтобы он стоял на NightSpawnPoint во время backstory.
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
        if (!_phaseActive) return;
        phaseTimer -= Time.deltaTime;
        if (phaseTimer <= 0f) TransitionPhase();
    }

    public void BeginPhase(GamePhase phase)
    {
        currentPhase = phase;
        _phaseActive = true;

        // Телепортируем игрока в нужную точку
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

    void TransitionPhase()
    {
        _phaseActive = false;
        BeginPhase(currentPhase == GamePhase.Night ? GamePhase.Day : GamePhase.Night);
    }

    public void TriggerArrest()
    {
        onArrest?.Invoke();
        PlayerEconomy.Instance?.LoseAllNightResources();

        // Пропускаем остаток ночи при аресте
        if (currentPhase == GamePhase.Night)
        {
            Debug.Log("[GameManager] Игрок арестован! Скипаем ночь.");
            _phaseActive = false;
            BeginPhase(GamePhase.Day);
        }
    }

    public void TriggerInspectorFine(int amount)
    {
        onInspectorFine?.Invoke();
        PlayerEconomy.Instance?.SpendMoney(amount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Логика телепортации
    // ─────────────────────────────────────────────────────────────────────────
    private void FindPlayer()
    {
        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerTransform = player.transform;
        }
    }

    private void TeleportPlayer(GamePhase phase)
    {
        FindPlayer();
        if (_playerTransform == null) return;

        Transform targetPoint = (phase == GamePhase.Day) ? daySpawnPoint : nightSpawnPoint;

        if (targetPoint == null)
        {
            Debug.LogWarning("[GameManager] Точки спавна не назначены! Телепорт отменен.");
            return;
        }

        // ВАЖНО: CharacterController блокирует прямое изменение position —
        // выключаем его на один кадр, перемещаем, включаем обратно.
        CharacterController cc = _playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        _playerTransform.position = targetPoint.position;
        _playerTransform.rotation = targetPoint.rotation;

        if (cc != null) cc.enabled = true;
    }
}
