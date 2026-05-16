using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// StoryManager — управляет всем нарративным потоком игры:
///   1. Показывает предысторию (белый текст на чёрном) перед стартом.
///   2. Запускает геймплей ночью (первая фаза = Night).
///   3. Следит за 3 игровыми днями и долгом в $2400.
///   4. Вызывает Game Over или Victory по итогу.
/// </summary>
public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Настройки долга
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Debt Settings")]
    [Tooltip("Общая сумма долга, которую надо выплатить за 3 дня")]
    public int totalDebt = 1500;

    [Tooltip("Сколько дней даётся на погашение долга")]
    public int totalDays = 3;

    [Tooltip("Минимальная сумма, которую нужно заработать ЗА ДЕНЬ (или иметь к концу дня), чтобы не проиграть")]
    public int dailyMoneyGoal = 200;

    // ─────────────────────────────────────────────────────────────────────────
    //  Backstory
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Backstory Text")]
    [TextArea(4, 12)]
    public string backstoryText = "";


    [Header("Backstory Timing")]
    [Tooltip("Задержка перед появлением текста (сек)")]
    public float textFadeInDelay  = 0.5f;
    [Tooltip("Время появления текста")]
    public float textFadeInTime   = 1.5f;
    [Tooltip("Как долго текст стоит на экране")]
    public float textDisplayTime  = 6f;
    [Tooltip("Время исчезновения текста")]
    public float textFadeOutTime  = 1.2f;
    [Tooltip("Задержка после текста перед стартом игры")]
    public float postStoryDelay   = 0.8f;

    // ─────────────────────────────────────────────────────────────────────────
    //  UI References (создаются программно, или назначьте вручную)
    // ─────────────────────────────────────────────────────────────────────────
    [Header("UI References (auto-built if null)")]
    public Canvas storyCanvas;
    public Image  blackBackground;
    public TMP_Text storyLabel;

    // HUD-подсказка долга (опционально)
    public TMP_Text debtHudLabel;

    // ─────────────────────────────────────────────────────────────────────────
    //  Day-End Summary Panel
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Day Summary Panel")]
    public GameObject daySummaryPanel;
    public TMP_Text   daySummaryText;
    public Button     daySummaryContinueButton;
    [Tooltip("Сколько секунд ждать до автоматического продолжения")]
    public float daySummaryAutoAdvance = 5f;

    [Header("End Screen Reference")]
    public GameOverScreen gameOverScreen;

    // ─────────────────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Events")]
    public UnityEvent onStoryFinished;
    public UnityEvent onGameWin;
    public UnityEvent onGameOver;

    // ─────────────────────────────────────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────────────────────────────────────
    private int _currentDay   = 0;   // 0 = backstory, 1-3 = игровые дни
    private int _debtPaid     = 0;
    private int _debtRemaining => totalDebt - _debtPaid;
    private bool _gameEnded   = false;
    private bool _waitingForSummaryConfirm = false;
    private bool _isHandlingPhaseTransition = false;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        BuildUIIfNeeded();
        
        if (gameOverScreen == null)
            gameOverScreen = Object.FindFirstObjectByType<GameOverScreen>();

        HideAllPanels();

        // Подписываемся на переходы фаз
        if (GameManager.Instance != null)
        {
            // Начало дня: увеличиваем счетчик дней
            GameManager.Instance.onDayBegin.AddListener(() => {
                if (!_gameEnded) _currentDay++;
                Debug.Log($"[StoryManager] Начался День {_currentDay}");
            });

            // Начало ночи: день закончился -> проверяем цель и показываем суммари
            GameManager.Instance.onNightBegin.AddListener(OnDayEnded);
        }

        // Стартуем с предыстории
        StartCoroutine(RunBackstory());
    }

    void Update()
    {
        // Обновляем HUD долга каждый кадр
        UpdateDebtHud();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Backstory sequence
    // ─────────────────────────────────────────────────────────────────────────
    IEnumerator RunBackstory()
    {
        // Замораживаем игру пока идёт предыстория
        Time.timeScale = 0f;

        // Показываем чёрный фон + текст
        storyCanvas.gameObject.SetActive(true);
        blackBackground.color = Color.black;

        storyLabel.text  = backstoryText;
        storyLabel.alpha = 0f;
        storyLabel.gameObject.SetActive(true);

        // Пауза перед появлением
        yield return new WaitForSecondsRealtime(textFadeInDelay);

        // Fade in
        yield return FadeTextRealtime(storyLabel, 0f, 1f, textFadeInTime);

        // Ждём пока текст читают; нажатие любой клавиши/касание ускоряет
        float elapsed = 0f;
        while (elapsed < textDisplayTime)
        {
            elapsed += Time.unscaledDeltaTime;
            bool anyKey = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                       || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
            if (anyKey) break;
            yield return null;
        }

        // Fade out
        yield return FadeTextRealtime(storyLabel, 1f, 0f, textFadeOutTime);
        yield return FadeImageRealtime(blackBackground, 1f, 0f, textFadeOutTime);

        // Скрываем overlay
        storyCanvas.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(postStoryDelay);

        // Запускаем игру с первой ночи
        Time.timeScale = 1f;
        _currentDay = 0; // Начнем с 0, инкремент будет при первом onDayBegin
        Debug.Log("[StoryManager] Предыстория завершена. Начинается первая ночь.");

        // Сообщаем подписчикам
        onStoryFinished?.Invoke();

        // Убеждаемся, что GameManager стартует с Night
        if (GameManager.Instance != null)
        {
            yield return null; // Ждём кадр для финализации всех систем
            GameManager.Instance.BeginPhase(GamePhase.Night);
        }

        UpdateDebtHud();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Called when a day ends (GameManager.onNightBegin fires)
    // ─────────────────────────────────────────────────────────────────────────
    void OnDayEnded()
    {
        if (_gameEnded || _currentDay == 0 || _isHandlingPhaseTransition) return;

        StartCoroutine(DayEndSequence());
    }

    private IEnumerator DayEndSequence()
    {
        _isHandlingPhaseTransition = true;

        // Затемняем экран
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeToBlack(1f);

        // Проверяем цель на сегодня
        int currentMoney = PlayerEconomy.Instance?.Money ?? 0;
        
        if (currentMoney < dailyMoneyGoal)
        {
            // Не набрали нужную сумму за день -> Геймовер
            TriggerGameOver($"Ты не смог заработать даже ${dailyMoneyGoal} за день.\nБратья Мамытовы не любят ждать.");
            _isHandlingPhaseTransition = false;
            yield break;
        }

        ShowDaySummary();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Day Summary
    // ─────────────────────────────────────────────────────────────────────────
    void ShowDaySummary()
    {
        if (daySummaryPanel == null) return;

        Time.timeScale = 0f;
        daySummaryPanel.SetActive(true);

        int currentMoney = PlayerEconomy.Instance?.Money ?? 0;

        string summary =
            $"═══ КОНЕЦ ДНЯ {_currentDay} ═══\n\n" +
            $"Баланс:         ${currentMoney}\n" +
            $"Долг погашен:   ${_debtPaid}\n" +
            $"Остаток долга:  ${_debtRemaining}\n\n";

        if (_currentDay >= totalDays)
        {
            summary += _debtRemaining <= 0
                ? "✅ Долг выплачен! Братья Мамытовы довольны."
                : $"❌ Время вышло. Остаток: ${_debtRemaining}";
        }
        else
        {
            summary += $"Осталось дней: {totalDays - _currentDay}";
        }

        if (daySummaryText != null)
            daySummaryText.text = summary;

        StartCoroutine(DaySummaryRoutine());
    }

    IEnumerator DaySummaryRoutine()
    {
        _waitingForSummaryConfirm = true;
        float elapsed = 0f;

        while (_waitingForSummaryConfirm && elapsed < daySummaryAutoAdvance)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        AdvanceFromSummary();
    }

    /// <summary>Вызывается кнопкой "Продолжить" на панели суммари.</summary>
    public void OnSummaryContinuePressed()
    {
        _waitingForSummaryConfirm = false;
    }

    void AdvanceFromSummary()
    {
        if (daySummaryPanel != null)
            daySummaryPanel.SetActive(false);

        Time.timeScale = 1f;

        // Если не финал, возвращаем свет
        if (_currentDay < totalDays && !_gameEnded)
        {
            if (ScreenFader.Instance != null)
                StartCoroutine(ScreenFader.Instance.FadeFromBlack(1f));
        }

        if (_currentDay >= totalDays)
        {
            // Финал
            CheckEndCondition();
        }
        else
        {
            // Просто продолжаем к ночи (GameManager сам переключит фазу или мы форсируем)
            // Но в нашей схеме GameManager переключает фазы сам, а мы просто ставим Time.timeScale = 1
            if (GameManager.Instance != null && GameManager.Instance.currentPhase != GamePhase.Night)
                GameManager.Instance.BeginPhase(GamePhase.Night);
        }

        _isHandlingPhaseTransition = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Debt payments (вызывайте извне, например из DeliveryCounter)
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Вызовите этот метод когда игрок хочет погасить часть долга деньгами.
    /// amount = сколько платит; возвращает true если платёж прошёл.
    /// </summary>
    public bool PayDebt(int amount)
    {
        if (amount <= 0 || _gameEnded) return false;

        PlayerEconomy economy = PlayerEconomy.Instance;
        if (economy == null) return false;

        int toPay = Mathf.Min(amount, _debtRemaining); // не переплачиваем
        if (!economy.SpendMoney(toPay)) return false;

        _debtPaid += toPay;
        Debug.Log($"[StoryManager] Долг оплачен: ${toPay}. Итого оплачено: ${_debtPaid} / ${totalDebt}");
        UpdateDebtHud();

        if (_debtRemaining <= 0)
        {
            Debug.Log("[StoryManager] Долг полностью погашен!");
            // Победа наступит в конце текущей ночи (через OnNightEnded → CheckEndCondition)
        }

        return true;
    }

    /// <summary>Автоматически засчитывает весь текущий баланс как погашение долга.</summary>
    public void AutoPayDebtFromBalance()
    {
        int money = PlayerEconomy.Instance?.Money ?? 0;
        if (money > 0) PayDebt(money);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  End condition
    // ─────────────────────────────────────────────────────────────────────────
    void CheckEndCondition()
    {
        if (_gameEnded) return;
        _gameEnded = true;

        if (_debtRemaining <= 0)
            TriggerVictory();
        else
            TriggerGameOver();
    }

    void TriggerVictory()
    {
        Debug.Log("[StoryManager] 🏆 Победа! Долг погашен.");
        
        if (gameOverScreen != null)
        {
            gameOverScreen.ShowVictory();
        }
        else
        {
            Time.timeScale = 0f;
            Debug.LogWarning("[StoryManager] GameOverScreen не назначен!");
        }

        onGameWin?.Invoke();
    }

    void TriggerGameOver(string reason = "")
    {
        Debug.Log($"[StoryManager] 💀 Game Over. {reason}");

        if (gameOverScreen != null)
        {
            string finalReason = string.IsNullOrEmpty(reason) 
                ? $"Ты не смог собрать ${totalDebt} вовремя." 
                : reason;
            
            gameOverScreen.ShowGameOver(finalReason);
        }
        else
        {
            Time.timeScale = 0f;
            Debug.LogWarning("[StoryManager] GameOverScreen не назначен!");
        }

        onGameOver?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  HUD helper
    // ─────────────────────────────────────────────────────────────────────────
    void UpdateDebtHud()
    {
        if (debtHudLabel == null || _currentDay == 0) return;
        debtHudLabel.text =
            $"День {_currentDay}/{totalDays}  |  Долг: ${_debtRemaining}";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public getters
    // ─────────────────────────────────────────────────────────────────────────
    public int  CurrentDay      => _currentDay;
    public int  DebtRemaining   => _debtRemaining;
    public int  DebtPaid        => _debtPaid;
    public bool IsGameEnded     => _gameEnded;

    // ─────────────────────────────────────────────────────────────────────────
    //  UI builder (создаёт canvas программно если не назначен в инспекторе)
    // ─────────────────────────────────────────────────────────────────────────
    void BuildUIIfNeeded()
    {
        if (storyCanvas != null) return;

        // Canvas
        GameObject canvasGO = new GameObject("StoryCanvas");
        canvasGO.transform.SetParent(transform);
        storyCanvas = canvasGO.AddComponent<Canvas>();
        storyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        storyCanvas.sortingOrder = 999; // поверх всего
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Чёрный фон
        GameObject bgGO = new GameObject("BlackBackground");
        bgGO.transform.SetParent(canvasGO.transform, false);
        blackBackground = bgGO.AddComponent<Image>();
        blackBackground.color = Color.black;
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgRect.localScale = Vector3.one;

        // Текст предыстории
        GameObject labelGO = new GameObject("StoryLabel");
        labelGO.transform.SetParent(canvasGO.transform, false);
        storyLabel = labelGO.AddComponent<TextMeshProUGUI>();
        storyLabel.text      = backstoryText;
        storyLabel.color     = Color.white;
        storyLabel.fontSize  = 22f;
        storyLabel.alignment = TextAlignmentOptions.Center;
        storyLabel.alpha     = 0f;

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.1f, 0.1f);
        labelRect.anchorMax = new Vector2(0.9f, 0.9f);
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

        canvasGO.SetActive(false);
    }

    void HideAllPanels()
    {
        if (daySummaryPanel != null) daySummaryPanel.SetActive(false);
        // Панели GameOver/Victory теперь управляются через GameOverScreen
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Fade helpers (unscaled — работают при Time.timeScale = 0)
    // ─────────────────────────────────────────────────────────────────────────
    IEnumerator FadeTextRealtime(TMP_Text label, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            label.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        label.alpha = to;
    }

    IEnumerator FadeImageRealtime(Image img, float from, float to, float duration)
    {
        float t = 0f;
        Color c = img.color;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(from, to, t / duration);
            img.color = c;
            yield return null;
        }
        c.a = to;
        img.color = c;
    }
}
