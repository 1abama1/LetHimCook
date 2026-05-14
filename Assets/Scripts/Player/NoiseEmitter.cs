using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Система шума игрока. Испускает радиус шума при беге,
/// который Коп слышит даже без линии видимости.
/// Добавьте на игрока рядом с PlayerController.
/// </summary>
public class NoiseEmitter : MonoBehaviour
{
    [Header("Noise Radii")]
    public float walkNoiseRadius   = 2f;
    public float sprintNoiseRadius = 5f;
    public float sneakNoiseRadius  = 0.5f;
    public float idleNoiseRadius   = 0f;

    [Header("Emit Settings")]
    public float emitInterval = 0.2f;   // как часто пульсирует шум
    public LayerMask noiseTargetLayer;  // слой Копа

    public UnityEvent<float> onNoiseEmit;

    private PlayerController _player;
    private float            _emitTimer;

    public float CurrentNoiseRadius { get; private set; }

    void Awake() => _player = GetComponent<PlayerController>();

    void Update()
    {
        // Вычисляем текущий радиус шума
        if (_player == null) { CurrentNoiseRadius = 0f; return; }

        if (_player.IsHiding)
            CurrentNoiseRadius = 0f;
        else if (_player.IsSneaking)
            CurrentNoiseRadius = sneakNoiseRadius;
        else if (_player.IsMoving)
            CurrentNoiseRadius = _player.IsSprinting ? sprintNoiseRadius : walkNoiseRadius;
        else
            CurrentNoiseRadius = idleNoiseRadius;

        // Периодически оповещаем ближних копов
        _emitTimer -= Time.deltaTime;
        if (_emitTimer <= 0f && CurrentNoiseRadius > 0f)
        {
            _emitTimer = emitInterval;
            EmitNoise();
        }
    }

    void EmitNoise()
    {
        onNoiseEmit?.Invoke(CurrentNoiseRadius);

        Collider[] hits = Physics.OverlapSphere(transform.position, CurrentNoiseRadius, noiseTargetLayer);
        foreach (var hit in hits)
        {
            CopNPC cop = hit.GetComponent<CopNPC>();
            cop?.HearNoise(transform.position, CurrentNoiseRadius);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? CurrentNoiseRadius : walkNoiseRadius);
    }
}
