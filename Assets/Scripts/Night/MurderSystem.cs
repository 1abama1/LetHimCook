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
    private UnityEngine.InputSystem.InputAction _attackAction;

    void Awake()
    {
        _player  = GetComponent<PlayerController>();
        _animator = GetComponent<Animator>();
    }

    void Start()
    {
        var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
        {
            _attackAction = playerInput.actions["Attack"];
        }
    }

    void Update()
    {
        if (_cooldownTimer > 0) _cooldownTimer -= Time.deltaTime;

        if (_attackAction == null) return;

        if (!_player.IsSneaking && _attackAction.WasPressedThisFrame())
        {
            TryAttack();
        }

        if (_player.IsSneaking && _attackAction.IsPressed())
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
        if (_animator != null) _animator.SetTrigger("Attack");
        _player?.FreezeMovement(0.4f); // Небольшая пауза при атаке
        onAttack?.Invoke();
        AudioManager.Instance?.Play("attack");

        Vector3 hitPoint = transform.position + transform.forward * (attackRange * 0.5f);
        Collider[] hits = Physics.OverlapSphere(hitPoint, attackRange, hitLayer);
        foreach (var hit in hits)
        {
            HomelessNPC homeless = hit.GetComponent<HomelessNPC>();
            if (homeless != null) { homeless.Kill(false); onKill?.Invoke(); AudioManager.Instance?.Play("kill"); }
        }
    }

    void TryStealthKill()
    {
        Vector3 hitPoint = transform.position + transform.forward * (stealthKillRange * 0.5f);
        Collider[] hits = Physics.OverlapSphere(hitPoint, stealthKillRange, hitLayer);
        foreach (var hit in hits)
        {
            HomelessNPC homeless = hit.GetComponent<HomelessNPC>();
            if (homeless != null && homeless.state != HomelessState.Dead)
            {
                Debug.Log($"[Murder] Stealth killed {homeless.gameObject.name}!");
                homeless.Kill(true);
                
                if (_animator != null) _animator.SetTrigger("StealthKill");
                _player?.FreezeMovement(1.5f); // Замораживаем на время анимации
                
                onStealthKill?.Invoke();
                onKill?.Invoke();
                AudioManager.Instance?.Play("stealth_kill");
                break;
            }
        }
        
        if (hits.Length == 0) Debug.Log("[Murder] Tried stealth kill, but missed.");
    }

    public float StealthKillProgress => _player.IsSneaking
        ? Mathf.Clamp01(_stealthHoldTimer / stealthKillTime)
        : 0f;

    void OnDrawGizmosSelected()
    {
        Vector3 attackPoint = transform.position + transform.forward * (attackRange * 0.5f);
        Vector3 stealthPoint = transform.position + transform.forward * (stealthKillRange * 0.5f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint, attackRange);
        
        Gizmos.color = new Color(1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(stealthPoint, stealthKillRange);
    }
}
