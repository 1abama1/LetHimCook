using UnityEngine;
using UnityEngine.Events;

public class MurderSystem : MonoBehaviour
{
    public float attackRange       = 1.2f;
    public float attackCooldown    = 0.8f;
    public float stealthKillRange  = 0.8f;
    public float stealthKillTime   = 1.2f;
    public LayerMask hitLayer;

    public UnityEvent onAttack;
    public UnityEvent onStealthKill;
    public UnityEvent onKill;

    private float _cooldownTimer;
    private float _stealthHoldTimer;
    private Animator _animator;
    private PlayerController _player;

    void Awake()
    {
        _player  = GetComponent<PlayerController>();
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (_cooldownTimer > 0) _cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Q))
            TryAttack();

        if (Input.GetKey(KeyCode.Q) && _player.IsSneaking)
        {
            _stealthHoldTimer += Time.deltaTime;
            if (_stealthHoldTimer >= stealthKillTime)
            {
                TryStealthKill();
                _stealthHoldTimer = 0f;
            }
        }
        else
        {
            _stealthHoldTimer = 0f;
        }
    }

    public void TryAttack()
    {
        if (_cooldownTimer > 0) return;
        _cooldownTimer = attackCooldown;
        _animator?.SetTrigger("Attack");
        onAttack?.Invoke();

        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, hitLayer);
        foreach (var hit in hits)
        {
            HomelessNPC homeless = hit.GetComponent<HomelessNPC>();
            if (homeless != null) { homeless.Kill(); onKill?.Invoke(); }
        }
    }

    void TryStealthKill()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, stealthKillRange, hitLayer);
        foreach (var hit in hits)
        {
            HomelessNPC homeless = hit.GetComponent<HomelessNPC>();
            if (homeless != null && homeless.state != HomelessState.Dead)
            {
                homeless.Kill();
                onStealthKill?.Invoke();
                onKill?.Invoke();
                break;
            }
        }
    }

    public float StealthKillProgress => _player.IsSneaking
        ? Mathf.Clamp01(_stealthHoldTimer / stealthKillTime)
        : 0f;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, stealthKillRange);
    }
}
