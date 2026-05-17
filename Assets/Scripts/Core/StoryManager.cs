using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// StoryManager — полностью автоматизированный менеджер сюжета и UI.
/// Программно создает все необходимые панели: предысторию, итоги дня и связывается с финальным экраном.
/// </summary>
public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    [Header("Debt Settings")]
    public int totalDebt      = 1000;
    public int totalDays      = 3;

    [Header("Backstory Text")]
    [TextArea(4, 12)]
    public string backstoryText = "You owe the Mamytov brothers. You have 3 days to collect $1000, otherwise the consequences will be fatal.";

    [Header("Fonts")]
    public TMP_FontAsset globalFont;

    [Header("Events")]
    public UnityEvent onStoryFinished;
    public UnityEvent onGameWin;
    public UnityEvent onGameOver;

    // Ссылки на авто-созданные элементы
    private Canvas   _uiCanvas;
    private TMP_Text _storyLabel;
    private GameObject _summaryPanel;
    private TMP_Text   _summaryText;
    private TMP_Text   _debtHudLabel;

    private int  _currentDay  = 0;
    private int  _debtPaid    = 0;
    private int  _debtRemaining => Mathf.Max(0, totalDebt - _debtPaid);
    private bool _gameEnded   = false;
    private bool _waitingForSummaryConfirm  = false;
    private bool _isHandlingPhaseTransition = false;

    public bool IsShowingSummary => _waitingForSummaryConfirm;
    public bool IsGameEnded      => _gameEnded;
    public int  CurrentDay       => _currentDay;
    public int  DebtPaid         => _debtPaid;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        BuildProgrammaticUI();
    }

    void Start()
    {
        // Автоматическое создание RecipeBookUI, если его нет
        if (RecipeBookUI.Instance == null)
        {
            GameObject recipeGo = new GameObject("RecipeBookSystem");
            recipeGo.AddComponent<RecipeBookUI>();
        }

        // Подписка на фазы GameManager
        if (GameManager.Instance != null)
        {
            // День начинается: инкрементируем счетчик дней
            GameManager.Instance.onDayBegin.AddListener(() =>
            {
                if (!_gameEnded) _currentDay++;
            });

            // День ЗАКОНЧИЛСЯ (начинается ночь): именно здесь показываем итоги дня!
            GameManager.Instance.onNightBegin.AddListener(OnDayPhaseEnded);
        }

        StartCoroutine(RunBackstory());
    }

    void Update()
    {
        if (_debtHudLabel != null && _currentDay > 0)
        {
            _debtHudLabel.text = $"DAY {_currentDay}/{totalDays} | DEBT: ${_debtRemaining}";
        }
    }

    // ── Поток предыстории ─────────────────────────────────────────────────────
    IEnumerator RunBackstory()
    {
        Time.timeScale = 0f;
        _uiCanvas.gameObject.SetActive(true);
        _summaryPanel.SetActive(false);
        _storyLabel.gameObject.SetActive(true);
        _storyLabel.text = backstoryText;
        _storyLabel.alpha = 0f;

        yield return new WaitForSecondsRealtime(0.5f);
        yield return FadeAlpha(_storyLabel, 0f, 1f, 1.5f);

        // Ожидание клика или клавиши
        while (true)
        {
            bool skip = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                     || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
            if (skip) break;
            yield return null;
        }

        yield return FadeAlpha(_storyLabel, 1f, 0f, 1f);
        _storyLabel.gameObject.SetActive(false);
        _uiCanvas.gameObject.SetActive(false);
        
        Time.timeScale = 1f;
        onStoryFinished?.Invoke();

        // Начинаем игру с Дня 1 (или Ночи, если фазы идут Ночь->День. 
        // По коду GameManager стартует с указанной в инспекторе фазы)
        if (GameManager.Instance != null)
            GameManager.Instance.BeginPhase(GameManager.Instance.currentPhase);

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeFromBlack(1.2f);
    }

    // ── Вызывается, когда ДЕНЬ завершился и должна начаться НОЧЬ ──────────────
    void OnDayPhaseEnded()
    {
        if (_gameEnded || _currentDay == 0 || _isHandlingPhaseTransition) return;
        StartCoroutine(DayEndSequence());
    }

    IEnumerator DayEndSequence()
    {
        Debug.Log($"[StoryManager] DayEndSequence started. Current Day: {_currentDay}/{totalDays}");
        _isHandlingPhaseTransition = true;

        // Останавливаем время и фиксируем заработок игрока в счет долга
        int moneyOnHands = PlayerEconomy.Instance?.Money ?? 0;
        int toPay = Mathf.Min(moneyOnHands, _debtRemaining);
        
        Debug.Log($"[StoryManager] Money on hands: {moneyOnHands}, to pay: {toPay}");
        if (toPay > 0 && PlayerEconomy.Instance != null)
        {
            PlayerEconomy.Instance.SpendMoney(toPay);
            _debtPaid += toPay;
            Debug.Log($"[StoryManager] Paid {toPay}. Total paid: {_debtPaid}. Remaining: {_debtRemaining}");
        }

        // Проверяем, настал ли конец игры (завершился последний 3-й день)
        if (_currentDay >= totalDays)
        {
            Debug.Log("[StoryManager] Final day reached! Preparing for endgame sequence.");
            _gameEnded = true;
            Time.timeScale = 0f; // Останавливаем таймеры полностью

            if (ScreenFader.Instance != null)
            {
                Debug.Log("[StoryManager] Setting screen to black for endgame...");
                ScreenFader.Instance.SetBlack();
            }

            _isHandlingPhaseTransition = false;
            CheckEndCondition();
            yield break;
        }

        // Если это еще не 3-й день, просто показываем промежуточные итоги дня
        Debug.Log("[StoryManager] Showing daily summary...");
        ShowSummary();
    }

    void ShowSummary()
    {
        Time.timeScale = 0f; // Ставим игру на паузу во время итогов
        _uiCanvas.gameObject.SetActive(true);
        _summaryPanel.SetActive(true);
        _waitingForSummaryConfirm = true;

        int moneyLeft = PlayerEconomy.Instance?.Money ?? 0;
        _summaryText.text = 
            $"<size=32><color=#FFD700>DAY {_currentDay} SUMMARY</color></size>\n\n" +
            $"Collected and paid today: <color=green>${_debtPaid}</color>\n" +
            $"Cash remaining: <color=white>${moneyLeft}</color>\n" +
            $"REMAINING DEBT: <color=red>${_debtRemaining}</color>\n\n" +
            $"Days left: {totalDays - _currentDay}";
    }

    public void OnSummaryContinuePressed()
    {
        _waitingForSummaryConfirm = false;
        _summaryPanel.SetActive(false);
        _uiCanvas.gameObject.SetActive(false);
        Time.timeScale = 1f; // Возвращаем время игры в норму

        _isHandlingPhaseTransition = false;
    }

    // ── Финальная проверка после 3 дней ───────────────────────────────────────
    void CheckEndCondition()
    {
        Debug.Log($"[StoryManager] CheckEndCondition started. Debt Remaining: {_debtRemaining}");
        _uiCanvas.gameObject.SetActive(false);

        if (_debtRemaining <= 0)
        {
            Debug.Log("[StoryManager] Condition met: VICTORY!");
            onGameWin?.Invoke();
            if (EndingScreen.Instance != null)
            {
                Debug.Log("[StoryManager] Calling EndingScreen.ShowVictory()");
                EndingScreen.Instance.ShowVictory();
            }
            else
            {
                Debug.LogError("[StoryManager] EndingScreen.Instance is NULL! Cannot show victory screen.");
            }
        }
        else
        {
            Debug.Log("[StoryManager] Condition met: DEFEAT (Not enough money).");
            onGameOver?.Invoke();
            string failReason = $"You will share the fate of your homeless friends.\nRemaining debt: ${_debtRemaining}";
            if (EndingScreen.Instance != null)
            {
                Debug.Log("[StoryManager] Calling EndingScreen.ShowDefeat()");
                EndingScreen.Instance.ShowDefeat(failReason);
            }
            else
            {
                Debug.LogError("[StoryManager] EndingScreen.Instance is NULL! Cannot show defeat screen.");
            }
        }
    }

    // ── Автоматическая сборка интерфейса ──────────────────────────────────────
    void BuildProgrammaticUI()
    {
        GameObject canvasGO = new GameObject("[StoryUI]", typeof(RectTransform));
        DontDestroyOnLoad(canvasGO);
        
        _uiCanvas = canvasGO.AddComponent<Canvas>();
        _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _uiCanvas.sortingOrder = 998;
        
        CanvasScaler sc = canvasGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject bgGO = new GameObject("BlackBG", typeof(RectTransform));
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Color.black;
        Stretch(bgGO.GetComponent<RectTransform>());

        GameObject storyGO = new GameObject("StoryLabel", typeof(RectTransform));
        storyGO.transform.SetParent(canvasGO.transform, false);
        _storyLabel = storyGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(_storyLabel);
        _storyLabel.alignment = TextAlignmentOptions.Center;
        _storyLabel.fontSize = 28f;
        _storyLabel.enableWordWrapping = true;
        Stretch(storyGO.GetComponent<RectTransform>(), 100f);

        _summaryPanel = new GameObject("SummaryPanel", typeof(RectTransform));
        _summaryPanel.transform.SetParent(canvasGO.transform, false);
        Image sBg = _summaryPanel.AddComponent<Image>();
        sBg.color = new Color(0, 0, 0, 0.9f);
        Stretch(_summaryPanel.GetComponent<RectTransform>(), 400f, 250f);

        GameObject sTextGO = new GameObject("SummaryText", typeof(RectTransform));
        sTextGO.transform.SetParent(_summaryPanel.transform, false);
        _summaryText = sTextGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(_summaryText);
        _summaryText.alignment = TextAlignmentOptions.Center;
        _summaryText.fontSize = 24f;
        Stretch(sTextGO.GetComponent<RectTransform>(), 20f, 80f);

        GameObject btnGO = new GameObject("ContinueBtn", typeof(RectTransform));
        btnGO.transform.SetParent(_summaryPanel.transform, false);
        Image bImg = btnGO.AddComponent<Image>();
        bImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Button btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(OnSummaryContinuePressed);

        RectTransform bRT = btnGO.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.5f, 0);
        bRT.anchorMax = new Vector2(0.5f, 0);
        bRT.pivot = new Vector2(0.5f, 0);
        bRT.sizeDelta = new Vector2(240, 60);
        bRT.anchoredPosition = new Vector2(0, 40);

        GameObject bTextGO = new GameObject("Text", typeof(RectTransform));
        bTextGO.transform.SetParent(btnGO.transform, false);
        TMP_Text bt = bTextGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(bt);
        bt.text = "CONTINUE";
        bt.alignment = TextAlignmentOptions.Center;
        bt.fontSize = 18f;
        Stretch(bTextGO.GetComponent<RectTransform>());

        GameObject hudGO = new GameObject("DebtHUD", typeof(RectTransform));
        hudGO.transform.SetParent(canvasGO.transform, false);
        _debtHudLabel = hudGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(_debtHudLabel);
        _debtHudLabel.fontSize = 20f;
        _debtHudLabel.alignment = TextAlignmentOptions.TopRight;
        RectTransform hRT = hudGO.GetComponent<RectTransform>();
        hRT.anchorMin = hRT.anchorMax = new Vector2(1, 1);
        hRT.pivot = new Vector2(1, 1);
        hRT.sizeDelta = new Vector2(500, 50);
        hRT.anchoredPosition = new Vector2(-20, -20);

        _uiCanvas.gameObject.SetActive(false);
    }

    void Stretch(RectTransform rt, float marginX = 0, float marginY = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(marginX, marginY);
        rt.offsetMax = new Vector2(-marginX, -marginY);
    }

    IEnumerator FadeAlpha(TMP_Text txt, float from, float to, float dur)
    {
        float elapsed = 0;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            txt.alpha = Mathf.Lerp(from, to, elapsed / dur);
            yield return null;
        }
        txt.alpha = to;
    }

    public static void ApplyFont(TMP_Text text)
    {
        if (Instance != null && Instance.globalFont != null && text != null)
        {
            text.font = Instance.globalFont;
        }
    }
}
