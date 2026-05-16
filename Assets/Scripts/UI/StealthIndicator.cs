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

    [Header("Cop Suspicion Ring")]
    [Tooltip("Отдельный кольцевой индикатор подозрения Копа (ночью)")]
    public Image copSuspicionRing;
    public Color copSuspicionLow  = Color.green;
    public Color copSuspicionHigh = Color.red;

    private PlayerController     _player;
    private CopNPC[]             _cops;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            _player     = p.GetComponent<PlayerController>();
        }

        _cops = FindObjectsByType<CopNPC>(FindObjectsSortMode.None);

        // Инициализируем иконки
        SetActive(sneakIcon,   false);
        SetActive(sprintIcon,  false);
        SetActive(hidingIcon,  false);
    }

    void Update()
    {
        UpdateStateIcons();
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

    // ─── Подозрение Копа ─────────────────────────────────────────────────────
    void UpdateCopSuspicion()
    {
        if (copSuspicionRing == null) return;

        float maxSus = 0f;
        if (_cops != null)
        {
            foreach (var c in _cops)
            {
                if (c != null && (c.SuspicionPercent * 100f) > maxSus) 
                    maxSus = c.SuspicionPercent * 100f;
            }
        }

        copSuspicionRing.fillAmount = maxSus / 100f;
        copSuspicionRing.color      = Color.Lerp(copSuspicionLow, copSuspicionHigh, maxSus / 100f);
    }

    // ─── Утилита ─────────────────────────────────────────────────────────────
    static void SetActive(Image img, bool active)
    {
        if (img != null) img.enabled = active;
    }
}
