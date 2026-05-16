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
    public int totalDebt      = 1500;
    public int totalDays      = 3;

    [Header("Backstory Text")]
    [TextArea(4, 12)]
    public string backstoryText = "Ты задолжал братьям Мамытовым. У тебя есть 3 дня, чтобы собрать $1500, иначе последствия будут фатальными.";

    [Header("End Screen")]
    public GameOverScreen gameOverScreen;

    [Header("UI Theme")]
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
    private int  _debtRemaining => totalDebt - _debtPaid;
    private bool _gameEnded   = false;
    private bool _waitingForSummaryConfirm  = false;
    private bool _isHandlingPhaseTransition = false;

    public bool IsShowingSummary => _waitingForSummaryConfirm;
    public bool IsGameEnded      => _gameEnded;
    public int  CurrentDay       => _currentDay;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        BuildProgrammaticUI();
    }

    void Start()
    {
        // Поиск или создание экрана конца игры
        if (gameOverScreen == null)
            gameOverScreen = Object.FindFirstObjectByType<GameOverScreen>();

        if (gameOverScreen == null)
        {
            GameObject go = new GameObject("GameOverScreen");
            go.transform.SetParent(transform, false);
            gameOverScreen = go.AddComponent<GameOverScreen>();
        }

        // Подписка на фазы GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onDayBegin.AddListener(() =>
            {
                if (!_gameEnded) _currentDay++;
            });
            GameManager.Instance.onNightBegin.AddListener(OnDayEnded);
        }

        StartCoroutine(RunBackstory());
    }

    void Update()
    {
        if (_debtHudLabel != null && _currentDay > 0)
        {
            _debtHudLabel.text = $"ДЕНЬ {_currentDay}/{totalDays} | ДОЛГ: ${_debtRemaining}";
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

        if (GameManager.Instance != null)
            GameManager.Instance.BeginPhase(GamePhase.Night);

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeFromBlack(1.2f);
    }

    // ── Конец дня ─────────────────────────────────────────────────────────────
    void OnDayEnded()
    {
        if (_gameEnded || _currentDay == 0 || _isHandlingPhaseTransition) return;
        StartCoroutine(DayEndSequence());
    }

    IEnumerator DayEndSequence()
    {
        _isHandlingPhaseTransition = true;

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeToBlack(1f);

        if (_currentDay >= totalDays)
        {
            _isHandlingPhaseTransition = false;
            CheckEndCondition();
            yield break;
        }

        ShowSummary();
    }

    void ShowSummary()
    {
        Time.timeScale = 0f;
        _uiCanvas.gameObject.SetActive(true);
        _summaryPanel.SetActive(true);
        _waitingForSummaryConfirm = true;

        int money = PlayerEconomy.Instance?.Money ?? 0;
        _summaryText.text = 
            $"<size=32><color=#FFD700>КОНЕЦ ДНЯ {_currentDay}</color></size>\n\n" +
            $"На руках: <color=white>${money}</color>\n" +
            $"Выплачено: <color=green>${_debtPaid}</color>\n" +
            $"Остаток долга: <color=red>${_debtRemaining}</color>\n\n" +
            $"Дней до финала: {totalDays - _currentDay}";
    }

    public void OnSummaryContinuePressed()
    {
        _waitingForSummaryConfirm = false;
        _summaryPanel.SetActive(false);
        _uiCanvas.gameObject.SetActive(false);
        Time.timeScale = 1f;

        if (!_gameEnded)
        {
            if (ScreenFader.Instance != null)
                StartCoroutine(ScreenFader.Instance.FadeFromBlack(1f));
            
            if (GameManager.Instance != null && GameManager.Instance.currentPhase != GamePhase.Night)
                GameManager.Instance.BeginPhase(GamePhase.Night);
        }
        _isHandlingPhaseTransition = false;
    }

    // ── Финальная проверка ────────────────────────────────────────────────────
    void CheckEndCondition()
    {
        if (_gameEnded) return;
        
        // Автоматическая выплата долга из остатка на руках
        int money = PlayerEconomy.Instance?.Money ?? 0;
        if (money > 0)
        {
            int toPay = Mathf.Min(money, _debtRemaining);
            PlayerEconomy.Instance.SpendMoney(toPay);
            _debtPaid += toPay;
        }

        _gameEnded = true;

        if (_debtRemaining <= 0)
        {
            onGameWin?.Invoke();
            if (gameOverScreen != null) gameOverScreen.ShowVictory();
        }
        else
        {
            onGameOver?.Invoke();
            if (gameOverScreen != null) 
                gameOverScreen.ShowGameOver($"Ты не успел собрать всю сумму.\nОстаток долга: ${_debtRemaining}");
        }
    }

    // ── Постройка интерфейса ──────────────────────────────────────────────────
    void BuildProgrammaticUI()
    {
        // 1. Canvas — оставляем корневым (БЕЗ SetParent к StoryManager), чтобы он сразу подхватывал размеры экрана
        GameObject canvasGO = new GameObject("[StoryUI]", typeof(RectTransform));
        DontDestroyOnLoad(canvasGO); // Так как StoryManager не уничтожается, Canvas тоже должен жить между сценами
        
        _uiCanvas = canvasGO.AddComponent<Canvas>();
        _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _uiCanvas.sortingOrder = 998;
        
        CanvasScaler sc = canvasGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // 2. Black BG — создаем сразу как RectTransform
        GameObject bgGO = new GameObject("BlackBG", typeof(RectTransform));
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Color.black;
        Stretch(bgGO.GetComponent<RectTransform>());

        // 3. Story Label — создаем сразу как RectTransform
        GameObject storyGO = new GameObject("StoryLabel", typeof(RectTransform));
        storyGO.transform.SetParent(canvasGO.transform, false);
        _storyLabel = storyGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(_storyLabel);
        _storyLabel.alignment = TextAlignmentOptions.Center;
        _storyLabel.fontSize = 28f;
        _storyLabel.enableWordWrapping = true;
        Stretch(storyGO.GetComponent<RectTransform>(), 100f);

        // 4. Summary Panel — создаем сразу как RectTransform
        _summaryPanel = new GameObject("SummaryPanel", typeof(RectTransform));
        _summaryPanel.transform.SetParent(canvasGO.transform, false);
        Image sBg = _summaryPanel.AddComponent<Image>();
        sBg.color = new Color(0, 0, 0, 0.85f);
        Stretch(_summaryPanel.GetComponent<RectTransform>(), 400f, 250f);

        GameObject sTextGO = new GameObject("SummaryText", typeof(RectTransform));
        sTextGO.transform.SetParent(_summaryPanel.transform, false);
        _summaryText = sTextGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(_summaryText);
        _summaryText.alignment = TextAlignmentOptions.Center;
        _summaryText.fontSize = 24f;
        Stretch(sTextGO.GetComponent<RectTransform>(), 20f, 80f);

        // Button — создаем сразу как RectTransform
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
        bRT.sizeDelta = new Vector2(200, 50);
        bRT.anchoredPosition = new Vector2(0, 30);

        GameObject bTextGO = new GameObject("Text", typeof(RectTransform));
        bTextGO.transform.SetParent(btnGO.transform, false);
        TMP_Text bt = bTextGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(bt);
        bt.text = "ПРОДОЛЖИТЬ";
        bt.alignment = TextAlignmentOptions.Center;
        bt.fontSize = 18f;
        Stretch(bTextGO.GetComponent<RectTransform>());

        // 5. HUD Debt — создаем сразу как RectTransform
        GameObject hudGO = new GameObject("DebtHUD", typeof(RectTransform));
        hudGO.transform.SetParent(canvasGO.transform, false);
        _debtHudLabel = hudGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(_debtHudLabel);
        _debtHudLabel.fontSize = 20f;
        _debtHudLabel.alignment = TextAlignmentOptions.TopRight;
        RectTransform hRT = hudGO.GetComponent<RectTransform>();
        hRT.anchorMin = hRT.anchorMax = new Vector2(1, 1);
        hRT.pivot = new Vector2(1, 1);
        hRT.sizeDelta = new Vector2(400, 50);
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
