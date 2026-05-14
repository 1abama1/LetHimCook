using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public enum InspectorState { Roam, Watch, TurnBack }

public class InspectorAI : MonoBehaviour
{
    [Header("Roaming")]
    public Transform[] waypoints;
    public float roamSpeed              = 1.5f;
    public float waitAtWaypointSeconds  = 2f;

    [Header("Vision")]
    public float visionRange            = 5f;
    [Range(20f, 120f)]
    public float visionAngle            = 60f;

    [Header("Suspicion")]
    [Range(0f, 100f)]
    public float suspicion              = 0f;
    public float suspicionDecayRate     = 3f;
    public float suspicionMax           = 100f;

    [Header("Fine / Bribe")]
    public int fineAmount               = 50;
    public int bribeAmount              = 30;

    [Header("Surprise Turn")]
    public float surpriseTurnChance     = 0.05f;
    public float surpriseTurnDuration   = 0.5f;

    public UnityEvent                 onInspectorWatching;
    public UnityEvent<float>          onSuspicionChanged;
    public UnityEvent                 onFineIssued;
    public UnityEvent                 onBribeTaken;

    private InspectorState   _state = InspectorState.Roam;
    private int              _currentWaypoint;
    private CookingStation[] _stations;
    private bool             _surpriseTurning;

    void Start()
    {
        _stations = FindObjectsByType<CookingStation>(FindObjectsSortMode.None);
        StartCoroutine(SurpriseTurnRoutine());
    }

    void Update()
    {
        if (_state == InspectorState.Roam)  UpdateRoam();
        if (_state == InspectorState.Watch) UpdateWatch();

        if (suspicion > 0)
        {
            suspicion = Mathf.Max(0f, suspicion - suspicionDecayRate * Time.deltaTime);
            onSuspicionChanged?.Invoke(suspicion);
        }

        if (suspicion >= suspicionMax)
            TriggerFine();
    }

    void UpdateRoam()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        Transform target = waypoints[_currentWaypoint];

        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f) transform.forward = dir.normalized;
        transform.position = Vector3.MoveTowards(transform.position, target.position, roamSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.2f)
            StartCoroutine(WaitAndMoveNext());
    }

    IEnumerator WaitAndMoveNext()
    {
        _state = InspectorState.Watch;
        onInspectorWatching?.Invoke();
        yield return new WaitForSeconds(waitAtWaypointSeconds);
        _currentWaypoint = (_currentWaypoint + 1) % waypoints.Length;
        _state = InspectorState.Roam;
    }

    void UpdateWatch()
    {
        foreach (var station in _stations)
        {
            if (!IsWatchingStation(station)) continue;
            if (station.currentIngredient != null
                && station.currentIngredient.IsSpoiled
                && !station.IsIngredientHidden)
            {
                AddSuspicion(20f * Time.deltaTime);
            }
        }
    }

    public bool IsWatchingStation(CookingStation station)
    {
        if (station == null) return false;
        Vector3 toStation = station.transform.position - transform.position;
        if (toStation.magnitude > visionRange) return false;
        return Vector3.Angle(transform.forward, toStation) <= visionAngle;
    }

    public void AddSuspicion(float amount)
    {
        suspicion = Mathf.Min(suspicion + amount, suspicionMax);
        onSuspicionChanged?.Invoke(suspicion);
    }

    IEnumerator SurpriseTurnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (!_surpriseTurning && Random.value < surpriseTurnChance)
                StartCoroutine(DoSurpriseTurn());
        }
    }

    IEnumerator DoSurpriseTurn()
    {
        _surpriseTurning = true;
        Vector3 original = transform.eulerAngles;
        transform.eulerAngles = new Vector3(0f, original.y + 180f, 0f);
        _state = InspectorState.TurnBack;

        yield return new WaitForSeconds(surpriseTurnDuration);

        transform.eulerAngles = original;
        _state = InspectorState.Watch;
        _surpriseTurning = false;
    }

    void TriggerFine()
    {
        suspicion = 0f;
        onSuspicionChanged?.Invoke(0f);
        onFineIssued?.Invoke();
        AudioManager.Instance?.Play("inspector_fine");
        GameManager.Instance?.TriggerInspectorFine(fineAmount);
    }

    public bool TryBribe()
    {
        if (!PlayerEconomy.Instance.SpendMoney(bribeAmount)) return false;
        suspicion = Mathf.Max(0f, suspicion - 50f);
        onSuspicionChanged?.Invoke(suspicion);
        onBribeTaken?.Invoke();
        AudioManager.Instance?.Play("bribe");
        return true;
    }

    public float SuspicionPercent => suspicion / suspicionMax;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
}
