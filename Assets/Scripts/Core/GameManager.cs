using UnityEngine;
using UnityEngine.Events;

public enum GamePhase { Night, Day }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GamePhase currentPhase = GamePhase.Night;
    public float dayDurationSeconds = 180f;
    public float nightDurationSeconds = 120f;

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

    void Start() => BeginPhase(currentPhase);

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
    }

    public void TriggerInspectorFine(int amount)
    {
        onInspectorFine?.Invoke();
        PlayerEconomy.Instance?.SpendMoney(amount);
    }
}
