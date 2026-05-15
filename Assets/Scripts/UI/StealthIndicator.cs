using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Кольцевой индикатор заметности игрока.
/// Показывает фонарик (батарейка) и иконки состояния.
/// Добавьте в Canvas рядом с GameHUD.
/// </summary>
public class StealthIndicator : MonoBehaviour
{


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
    private FlashlightController _flashlight;
    private CopNPC               _cop;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            _player     = p.GetComponent<PlayerController>();
            _flashlight = p.GetComponent<FlashlightController>();
        }

        _cop = FindFirstObjectByType<CopNPC>();

        // Инициализируем иконки
        SetActive(sneakIcon,   false);
        SetActive(sprintIcon,  false);
        SetActive(hidingIcon,  false);
    }

    void Update()
    {
        UpdateStateIcons();
        UpdateFlashlight();
        UpdateCopSuspicion();
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
