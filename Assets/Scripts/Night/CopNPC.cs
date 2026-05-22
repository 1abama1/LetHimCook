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
    public UnityEvent<float> onSuspicionChanged;

    [Header("Visuals")]
    public Light copSpotlight;

    private CopState  _state = CopState.Patrol;
    private int       _currentPoint;
    private Transform _player;
    private float     _suspicionTimer;
    private bool      _isWaiting;
    private FlashlightController _playerFlashlight;
    private Animator             _animator;



    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p)
        {
            _player          = p.transform;
            _playerFlashlight = p.GetComponent<FlashlightController>();
        }
    }

    void Update()
    {
        if (_animator != null)
        {
            bool isMoving = (_state == CopState.Patrol && !_isWaiting) || (_state == CopState.Chase);
            _animator.SetBool("IsWalking", isMoving);
        }

        if (copSpotlight != null)
        {
            if (_state == CopState.Patrol) copSpotlight.color = Color.white;
            else if (_state == CopState.Suspicious) copSpotlight.color = Color.yellow;
            else if (_state == CopState.Chase || _state == CopState.Arrest) copSpotlight.color = Color.red;
        }

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
        if (CanSeePlayer()) { TransitionTo(CopState.Suspicious); return; }
        if (_isWaiting) return;

        MoveTowards(patrolPoints[_currentPoint].position, patrolSpeed);

        float dist = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(patrolPoints[_currentPoint].position.x, 0, patrolPoints[_currentPoint].position.z));

        if (dist < 0.25f)
            StartCoroutine(WaitAndNext());
    }

    IEnumerator WaitAndNext()
    {
        _isWaiting = true;
        yield return new WaitForSeconds(waitAtPointSeconds);
        _currentPoint = (_currentPoint + 1) % patrolPoints.Length;
        _isWaiting = false;
    }
    public void ResetSuspicion()
    {
        _suspicionTimer = 0f;
        onSuspicionChanged?.Invoke(0f);
        if (_state == CopState.Chase) TransitionTo(CopState.Patrol);
    }

    /// <summary>
    /// Полный сброс копа на новую ночь: возврат на стартовую точку,
    /// сброс состояния и подозрения, перезапуск патруля.
    /// </summary>
    public void ResetForNewNight()
    {
        StopAllCoroutines();
        _suspicionTimer = 0f;
        _isWaiting = false;
        _currentPoint = 0;
        _state = CopState.Patrol;
        onSuspicionChanged?.Invoke(0f);

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            transform.position = patrolPoints[0].position;
            transform.rotation = patrolPoints[0].rotation;
        }

        gameObject.SetActive(true);
    }

    void UpdateSuspicious()
    {
        if (CanSeePlayer())
        {
            if (_suspicionTimer == 0f && _animator != null)
                _animator.SetTrigger("CopCheck");

            _suspicionTimer += Time.deltaTime;
            onSuspicionChanged?.Invoke(SuspicionPercent * 100f);

            if (_suspicionTimer >= suspicionBuildTime)
            {
                onCopSpotPlayer?.Invoke();
                TransitionTo(CopState.Chase);
            }
        }
        else
        {
            _suspicionTimer -= Time.deltaTime * 2f;
            _suspicionTimer = Mathf.Max(0f, _suspicionTimer);
            onSuspicionChanged?.Invoke(SuspicionPercent * 100f);

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
        
        Vector3 playerPosSameHeight = new Vector3(_player.position.x, transform.position.y, _player.position.z);
        if (Vector3.Distance(transform.position, playerPosSameHeight) < arrestRange)
            Arrest();
    }

    void Arrest()
    {
        _state = CopState.Arrest;
        if (_animator != null) _animator.SetTrigger("Attack");
        
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



    void MoveTowards(Vector3 target, float speed)
    {
        Vector3 targetAtSameHeight = new Vector3(target.x, transform.position.y, target.z);

        Vector3 dir = (targetAtSameHeight - transform.position);
        if (dir.sqrMagnitude > 0.01f) transform.forward = dir.normalized;

        transform.position = Vector3.MoveTowards(transform.position, targetAtSameHeight, speed * Time.deltaTime);
    }

    void TransitionTo(CopState newState) => _state = newState;

    public float SuspicionPercent => Mathf.Clamp01(_suspicionTimer / suspicionBuildTime);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngle, 0) * transform.forward;

        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * visionRange);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, arrestRange);
    }
}
