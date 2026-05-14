using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public enum CopState { Patrol, Suspicious, Chase, Arrest }

public class CopNPC : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolSpeed          = 2f;
    public float waitAtPointSeconds   = 1.5f;

    [Header("Detection")]
    public float visionRange          = 6f;
    [Range(10f, 90f)]
    public float visionAngle          = 45f;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;
    public float suspicionBuildTime   = 1.5f;

    [Header("Chase")]
    public float chaseSpeed           = 4f;
    public float arrestRange          = 0.8f;

    public UnityEvent onCopSpotPlayer;
    public UnityEvent onArrestPlayer;

    private CopState  _state = CopState.Patrol;
    private int       _currentPoint;
    private Transform _player;
    private float     _suspicionTimer;
    private bool      _isWaiting;
    private FlashlightController _playerFlashlight;

    // Слышимый шум (передаётся из NoiseEmitter)
    private Vector3   _lastHeardPos;
    private float     _heardTimer;

    void Awake()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p)
        {
            _player          = p.transform;
            _playerFlashlight = p.GetComponent<FlashlightController>();
        }
    }

    void Update()
    {
        switch (_state)
        {
            case CopState.Patrol:     UpdatePatrol();     break;
            case CopState.Suspicious: UpdateSuspicious(); break;
            case CopState.Chase:      UpdateChase();      break;
        }
    }

    void UpdatePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        MoveTowards(patrolPoints[_currentPoint].position, patrolSpeed);
        if (!_isWaiting && Vector3.Distance(transform.position, patrolPoints[_currentPoint].position) < 0.2f)
            StartCoroutine(WaitAndNext());
        if (CanSeePlayer()) TransitionTo(CopState.Suspicious);
    }

    IEnumerator WaitAndNext()
    {
        _isWaiting = true;
        yield return new WaitForSeconds(waitAtPointSeconds);
        _currentPoint = (_currentPoint + 1) % patrolPoints.Length;
        _isWaiting = false;
    }

    void UpdateSuspicious()
    {
        if (CanSeePlayer())
        {
            _suspicionTimer += Time.deltaTime;
            if (_suspicionTimer >= suspicionBuildTime)
            {
                onCopSpotPlayer?.Invoke();
                TransitionTo(CopState.Chase);
            }
        }
        else
        {
            _suspicionTimer -= Time.deltaTime * 2f;
            if (_suspicionTimer <= 0f) TransitionTo(CopState.Patrol);
        }
    }

    void UpdateChase()
    {
        if (_player == null) { TransitionTo(CopState.Patrol); return; }

        PlayerController pc = _player.GetComponent<PlayerController>();
        if (pc != null && pc.IsHiding)
        {
            _suspicionTimer = 0f;
            TransitionTo(CopState.Patrol);
            return;
        }

        MoveTowards(_player.position, chaseSpeed);
        if (Vector3.Distance(transform.position, _player.position) < arrestRange)
            Arrest();
    }

    void Arrest()
    {
        _state = CopState.Arrest;
        onArrestPlayer?.Invoke();
        GameManager.Instance?.TriggerArrest();
    }

    bool CanSeePlayer()
    {
        if (_player == null) return false;
        Vector3 toPlayer = _player.position - transform.position;

        // Фонарик расширяет эффективный радиус зрения
        float mult   = _playerFlashlight != null ? _playerFlashlight.DetectionMult : 1f;
        float range  = visionRange * mult;
        float angle  = visionAngle;  // угол не меняем — меняем дальность

        if (toPlayer.magnitude > range) return false;
        if (Vector3.Angle(transform.forward, toPlayer) > angle) return false;
        if (Physics.Raycast(transform.position, toPlayer.normalized, toPlayer.magnitude, obstacleLayer))
            return false;
        return true;
    }

    /// <summary>Вызывается из NoiseEmitter когда игрок шумит рядом.</summary>
    public void HearNoise(Vector3 sourcePos, float radius)
    {
        if (_state == CopState.Chase || _state == CopState.Arrest) return;

        float dist = Vector3.Distance(transform.position, sourcePos);
        if (dist > radius) return;

        _lastHeardPos = sourcePos;
        _heardTimer   = 3f;   // секунды «помнить» звук

        // Если уже патрулирует — начинаем подозревать по звуку
        if (_state == CopState.Patrol)
        {
            _suspicionTimer += 0.3f;
            if (_suspicionTimer >= suspicionBuildTime)
                TransitionTo(CopState.Chase);
            else
                TransitionTo(CopState.Suspicious);
        }
    }

    void MoveTowards(Vector3 target, float speed)
    {
        Vector3 dir = (target - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f) transform.forward = dir.normalized;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    void TransitionTo(CopState newState) => _state = newState;

    public float SuspicionPercent => Mathf.Clamp01(_suspicionTimer / suspicionBuildTime);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, arrestRange);
    }
}
