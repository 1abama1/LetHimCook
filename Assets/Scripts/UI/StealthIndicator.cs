using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Кольцевой индикатор заметности + шума игрока.
/// Показывает фонарик (батарейка) и радиус шума.
/// Добавьте в Canvas рядом с GameHUD.
/// </summary>
public class StealthIndicator : MonoBehaviour
{
    [Header("Noise Ring")]
    public Image   noiseRingFill;       // Image в режиме Filled, type=Radial360
    public Color   silentColor  = new Color(0.2f, 0.8f, 0.3f);
    public Color   noisyColor   = new Color(0.9f, 0.3f, 0.1f);

    [Header("State Icons")]
    public Image   sneakIcon;           // иконка кравается
    public Image   sprintIcon;          // иконка бежит
    public Image   hidingIcon;          // иконка прячется

    [Header("Flashlight Battery")]
    public Image              batteryFill;   // Image filled, горизонтальный
    public TextMeshProUGUI    batteryText;
    public Color              batteryFullColor  = new Color(0.3f, 0.9f, 0.4f);
    public Color              batteryLowColor   = new Color(0.9f, 0.2f, 0.1f);
    public Image              flashlightOnIcon; // иконка фонарика (вкл/выкл)
    public Color              flashlightOnTint  = Color.yellow;
    public Color              flashlightOffTint = new Color(0.4f, 0.4f, 0.4f);

    [Header("Cop Suspicion Ring")]
    [Tooltip("Отдельный кольцевой индикатор подозрения Копа (ночью)")]
    public Image copSuspicionRing;
    public Color copSuspicionLow  = Color.green;
    public Color copSuspicionHigh = Color.red;

    private PlayerController     _player;
    private NoiseEmitter         _noise;
    private FlashlightController _flashlight;
    private CopNPC               _cop;

    // Кэшируем максимальный радиус для нормализации
    private float _maxNoiseRadius = 5f;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            _player     = p.GetComponent<PlayerController>();
            _noise      = p.GetComponent<NoiseEmitter>();
            _flashlight = p.GetComponent<FlashlightController>();

            if (_noise != null)
                _maxNoiseRadius = _noise.sprintNoiseRadius > 0f
                    ? _noise.sprintNoiseRadius
                    : 5f;
        }

        _cop = FindFirstObjectByType<CopNPC>();

        // Инициализируем иконки
        SetActive(sneakIcon,   false);
        SetActive(sprintIcon,  false);
        SetActive(hidingIcon,  false);
    }

    void Update()
    {
        UpdateNoise();
        UpdateStateIcons();
        UpdateFlashlight();
        UpdateCopSuspicion();
    }

    // ─── Шум ─────────────────────────────────────────────────────────────────
    void UpdateNoise()
    {
        if (_noise == null || noiseRingFill == null) return;

        float t = Mathf.Clamp01(_noise.CurrentNoiseRadius / _maxNoiseRadius);
        noiseRingFill.fillAmount = t;
        noiseRingFill.color      = Color.Lerp(silentColor, noisyColor, t);
    }

    // ─── Иконки состояния ─────────────────────────────────────────────────────
    void UpdateStateIcons()
    {
        if (_player == null) return;

        SetActive(hidingIcon,  _player.IsHiding);
        SetActive(sneakIcon,   _player.IsSneaking && !_player.IsHiding);
        SetActive(sprintIcon,  _player.IsSprinting && !_player.IsSneaking);
    }

    // ─── Батарейка фонарика ───────────────────────────────────────────────────
    void UpdateFlashlight()
    {
        if (_flashlight == null) return;

        float pct = _flashlight.BatteryPercent;

        if (batteryFill != null)
        {
            batteryFill.fillAmount = pct;
            batteryFill.color = Color.Lerp(batteryLowColor, batteryFullColor, pct);
        }

        if (batteryText != null)
            batteryText.text = $"{Mathf.RoundToInt(pct * 100f)}%";

        if (flashlightOnIcon != null)
            flashlightOnIcon.color = _flashlight.IsOn ? flashlightOnTint : flashlightOffTint;
    }

    // ─── Подозрение Копа ─────────────────────────────────────────────────────
    void UpdateCopSuspicion()
    {
        if (_cop == null || copSuspicionRing == null) return;

        float t = _cop.SuspicionPercent;
        copSuspicionRing.fillAmount = t;
        copSuspicionRing.color      = Color.Lerp(copSuspicionLow, copSuspicionHigh, t);
    }

    // ─── Утилита ─────────────────────────────────────────────────────────────
    static void SetActive(Image img, bool active)
    {
        if (img != null) img.enabled = active;
    }
}
