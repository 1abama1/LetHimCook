using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Управляет фонариком игрока: включение/выключение, батарейки,
/// мерцание при разряде и интеграция с конусом зрения Копа.
/// Добавьте компонент Light (Spot) как дочерний объект игрока.
/// </summary>
public class FlashlightController : MonoBehaviour
{
    [Header("Light Reference")]
    public Light spotLight;                  // Spotlight — дочерний объект игрока

    [Header("Battery")]
    public float maxBattery          = 100f;
    public float drainRateActive     = 4f;   // % в секунду при включённом фонаре
    public float drainRateSprint     = 7f;   // % в секунду при беге с фонарём
    public float flickerThreshold    = 20f;  // При каком % начинается мерцание

    [Header("Flicker")]
    public float flickerMinInterval  = 0.05f;
    public float flickerMaxInterval  = 0.3f;
    public float flickerMinIntensity = 0.2f;
    public float flickerMaxIntensity = 1.8f;

    [Header("Light Settings")]
    public float normalIntensity     = 2f;
    public float spotAngle           = 45f;
    public float range               = 8f;
    public Color lightColor          = new Color(1f, 0.95f, 0.8f);

    [Header("Cop Detection Multiplier")]
    [Tooltip("Во сколько раз легче коп замечает игрока с включённым фонарём")]
    public float detectionMultiplier = 1.6f;

    [Header("Events")]
    public UnityEvent onFlashlightOn;
    public UnityEvent onFlashlightOff;
    public UnityEvent onBatteryEmpty;
    public UnityEvent<float> onBatteryChanged;

    private bool             _isOn;
    private float            _battery;
    private PlayerController _player;
    private bool             _isFlickering;
    private Coroutine        _flickerCoroutine;

    // Публичный доступ для CopNPC
    public bool  IsOn             => _isOn;
    public float BatteryPercent   => _battery / maxBattery;
    public float DetectionMult    => (_isOn && _battery > 0f) ? detectionMultiplier : 1f;

    void Awake()
    {
        _player  = GetComponentInParent<PlayerController>();
        _battery = maxBattery;

        // Если не назначен в инспекторе — ищем среди дочерних
        if (spotLight == null)
            spotLight = GetComponentInChildren<Light>();

        if (spotLight != null)
        {
            spotLight.type      = LightType.Spot;
            spotLight.spotAngle = spotAngle;
            spotLight.range     = range;
            spotLight.color     = lightColor;
            spotLight.intensity = normalIntensity;
            spotLight.enabled   = false;
        }
    }

    void Update()
    {
        HandleInput();
        DrainBattery();
        UpdateFlicker();
    }

    // ─── Ввод ────────────────────────────────────────────────────────────────
    void HandleInput()
    {
        bool pressed = UnityEngine.InputSystem.Keyboard.current?.fKey.wasPressedThisFrame ?? false;

        // Пробуем читать из PlayerInput (если назначен экшн "Flashlight")
        var playerInput = _player != null
            ? _player.GetComponent<UnityEngine.InputSystem.PlayerInput>()
            : null;
        if (playerInput != null)
        {
            var action = playerInput.actions.FindAction("Flashlight");
            if (action != null) pressed = action.WasPressedThisFrame();
        }

        if (pressed) Toggle();
    }

    // ─── Включение / выключение ───────────────────────────────────────────────
    public void Toggle()
    {
        if (_battery <= 0f)
        {
            TurnOff();
            return;
        }
        _isOn = !_isOn;
        ApplyLightState();

        if (_isOn) onFlashlightOn?.Invoke();
        else       onFlashlightOff?.Invoke();
    }

    public void TurnOff()
    {
        _isOn = false;
        ApplyLightState();
        onFlashlightOff?.Invoke();
    }

    void ApplyLightState()
    {
        if (spotLight != null) spotLight.enabled = _isOn && _battery > 0f;
    }

    // ─── Расход батарейки ────────────────────────────────────────────────────
    void DrainBattery()
    {
        if (!_isOn || _battery <= 0f) return;

        float drain = _player != null && _player.IsSneaking ? drainRateActive * 0.5f
                    : drainRateActive;

        // Sprint через рефлексию, чтобы не ломать интерфейс
        // (PlayerController не раскрывает IsSprinting публично — добавим accessor ниже)
        _battery = Mathf.Max(0f, _battery - drain * Time.deltaTime);
        onBatteryChanged?.Invoke(BatteryPercent);

        if (_battery <= 0f)
        {
            TurnOff();
            onBatteryEmpty?.Invoke();
            AudioManager.Instance?.Play("flashlight_dead");
        }
    }

    // ─── Мерцание ────────────────────────────────────────────────────────────
    void UpdateFlicker()
    {
        bool shouldFlicker = _isOn && _battery > 0f && _battery <= flickerThreshold;

        if (shouldFlicker && !_isFlickering)
        {
            _isFlickering     = true;
            _flickerCoroutine = StartCoroutine(FlickerRoutine());
        }
        else if (!shouldFlicker && _isFlickering)
        {
            _isFlickering = false;
            if (_flickerCoroutine != null) StopCoroutine(_flickerCoroutine);
            if (spotLight != null) spotLight.intensity = normalIntensity;
        }
    }

    IEnumerator FlickerRoutine()
    {
        while (_isOn && _battery > 0f && _battery <= flickerThreshold)
        {
            float interval = Random.Range(flickerMinInterval, flickerMaxInterval);
            yield return new WaitForSeconds(interval);

            if (spotLight == null) continue;

            // Резкое изменение интенсивности
            spotLight.intensity = Random.Range(flickerMinIntensity, flickerMaxIntensity);

            // Иногда полностью гасим на кадр
            if (Random.value < 0.15f)
            {
                spotLight.enabled = false;
                yield return new WaitForSeconds(0.05f);
                spotLight.enabled = _isOn && _battery > 0f;
            }
        }

        if (spotLight != null) spotLight.intensity = normalIntensity;
    }

    // ─── Батарейки (подбор) ──────────────────────────────────────────────────
    public void AddBattery(float amount)
    {
        _battery = Mathf.Min(_battery + amount, maxBattery);
        onBatteryChanged?.Invoke(BatteryPercent);
        AudioManager.Instance?.Play("battery_pickup");
    }

    // ─── Gizmos ─────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (!_isOn) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
