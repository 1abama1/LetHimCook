using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

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

        // БЕЗОПАСНЫЙ МИКС: Сначала смотрим Input System напарника, если не настроено - берем кнопку Q (твой вариант)
        bool attackPressed = (_attackAction != null && _attackAction.WasPressedThisFrame()) || 
                             (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame);
                             
        bool attackHeld    = (_attackAction != null && _attackAction.IsPressed()) || 
                             (Keyboard.current != null && Keyboard.current.qKey.isPressed);

        if (attackPressed)
            TryAttack();

        if (attackHeld && _player.IsSneaking)
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
        // Временно закомментил AudioManager, если его скрипта еще нет, чтобы не было ошибок
        // AudioManager.Instance?.Play("attack");

        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, hitLayer);
        foreach (var hit in hits)
        {
            HomelessNPC homeless = hit.GetComponent<HomelessNPC>();
            if (homeless != null) 
            { 
                homeless.Kill(); 
                onKill?.Invoke(); 
                // AudioManager.Instance?.Play("kill"); 
            }
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
                // AudioManager.Instance?.Play("stealth_kill");
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
