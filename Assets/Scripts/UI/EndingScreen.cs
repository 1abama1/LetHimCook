using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Простой экран концовки. Показывает текст поверх чёрного ScreenFader.
/// Вызывай ShowVictory() или ShowDefeat() из StoryManager / кнопки.
/// </summary>
public class EndingScreen : MonoBehaviour
{
    public static EndingScreen Instance { get; private set; }

    // ── Текст концовок (меняй в инспекторе) ──────────────────────────────────
    [Header("Тексты концовок")]
    [TextArea(2, 5)]
    public string victoryTitle    = "ТЫ ПОБЕДИЛ";
    [TextArea(3, 8)]
    public string victoryMessage  = "Долг погашен.\nБратья Мамытовы довольны.";

    [TextArea(2, 5)]
    public string defeatTitle     = "ТЫ ПРОИГРАЛ";
    [TextArea(3, 8)]
    public string defeatMessage   = "Время вышло.\nБратья Мамытовы недовольны.";

    // ── Настройки внешнего вида ───────────────────────────────────────────────
    [Header("Внешний вид")]
    public float titleFontSize   = 72f;
    public float messageFontSize = 28f;
    public Color victoryColor    = new Color(1f, 0.85f, 0.25f);   // золото
    public Color defeatColor     = new Color(0.9f, 0.25f, 0.2f);  // красный
    public TMP_FontAsset customFont;                              // опциональный переопределитель шрифта

    [Header("Кнопки")]
    public string restartSceneName = ""; // пусто = текущая сцена
    public string mainMenuSceneName = "MainMenu";

    [Header("Тайминг")]
    public float fadeInDelay    = 0.5f;   // задержка перед появлением текста
    public float fadeInDuration = 1.2f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private Canvas      _canvas;
    private CanvasGroup _group;
    private TMP_Text    _titleLabel;
    private TMP_Text    _messageLabel;
    private bool        _built;

    // ── Unity ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Build();
        _canvas.gameObject.SetActive(false);
    }

    void Start()
    {
        RefreshFonts();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Вызови для показа победной концовки.</summary>
    public void ShowVictory()
    {
        _titleLabel.text   = victoryTitle;
        _messageLabel.text = victoryMessage;
        _titleLabel.color  = victoryColor;
        StartCoroutine(ShowRoutine());
    }

    /// <summary>Вызови для показа концовки поражения.</summary>
    public void ShowDefeat()
    {
        ShowDefeat(defeatMessage);
    }

    /// <summary>Перегрузка с произвольным текстом причины.</summary>
    public void ShowDefeat(string reason)
    {
        _titleLabel.text   = defeatTitle;
        if (!string.IsNullOrEmpty(reason))
            _messageLabel.text = defeatMessage + "\n\n" + reason;
        else
            _messageLabel.text = defeatMessage;

        _titleLabel.color  = defeatColor;
        StartCoroutine(ShowRoutine());
    }

    // ── Внутренняя логика ────────────────────────────────────────────────────

    IEnumerator ShowRoutine()
    {
        // Убеждаемся что ScreenFader чёрный
        if (ScreenFader.Instance != null)
            ScreenFader.Instance.SetBlack();

        RefreshFonts();
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(fadeInDelay);

        _canvas.gameObject.SetActive(true);
        _group.alpha = 0f;

        // Плавное появление
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            _group.alpha = t * t * (3f - 2f * t); // SmoothStep
            yield return null;
        }
        _group.alpha = 1f;
    }

    // ── Кнопки ───────────────────────────────────────────────────────────────

    public void OnRestart()
    {
        Time.timeScale = 1f;
        string scene = string.IsNullOrEmpty(restartSceneName)
            ? SceneManager.GetActiveScene().name
            : restartSceneName;
        SceneManager.LoadScene(scene);
    }

    public void OnMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Построение UI ────────────────────────────────────────────────────────

    void Build()
    {
        if (_built) return;
        _built = true;

        // Canvas
        var canvasGO = new GameObject("[EndingScreen]");
        canvasGO.transform.SetParent(transform, false);

        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9998; // чуть ниже ScreenFader (9999), но выше всего остального

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        _group               = canvasGO.AddComponent<CanvasGroup>();
        _group.alpha         = 0f;
        _group.blocksRaycasts = true;

        // Чёрный фон поверх игры
        var bgGO = new GameObject("BlackBackground", typeof(RectTransform));
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 1f); // Полностью чёрный фон

        // Центральная колонка
        var column = MakeRect(canvasGO.transform, "Column");
        column.anchorMin        = new Vector2(0.2f, 0.2f);
        column.anchorMax        = new Vector2(0.8f, 0.8f);
        column.offsetMin        = Vector2.zero;
        column.offsetMax        = Vector2.zero;

        var vlg = column.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment       = TextAnchor.MiddleCenter;
        vlg.spacing              = 30f;
        vlg.childControlHeight   = false;
        vlg.childControlWidth    = false;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth  = false;

        // Заголовок
        _titleLabel = MakeText(column, "Title", titleFontSize, FontStyles.Bold, victoryColor);
        _titleLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(1300f, 120f);

        // Сообщение
        _messageLabel = MakeText(column, "Message", messageFontSize, FontStyles.Normal, new Color(0.85f, 0.85f, 0.85f));
        _messageLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 200f);

        // Разделитель
        var divider = MakeRect(column, "Divider");
        divider.sizeDelta = new Vector2(500f, 2f);
        divider.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
        var divLE = divider.gameObject.AddComponent<LayoutElement>();
        divLE.preferredHeight = 2f;
        divLE.preferredWidth  = 500f;

        // Строка кнопок
        var btnRow = MakeRect(column, "BtnRow");
        btnRow.sizeDelta = new Vector2(700f, 60f);
        var hlg = btnRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment       = TextAnchor.MiddleCenter;
        hlg.spacing              = 24f;
        hlg.childControlHeight   = false;
        hlg.childControlWidth    = false;
        hlg.childForceExpandHeight = false;
        hlg.childForceExpandWidth  = false;

        MakeButton(btnRow, "RESTART",   OnRestart,  new Color(0.9f, 0.75f, 0.2f), new Color(0.06f, 0.06f, 0.06f));
        MakeButton(btnRow, "MAIN MENU",OnMainMenu, new Color(0.2f, 0.2f, 0.25f), new Color(0.85f, 0.85f, 0.85f));
        MakeButton(btnRow, "QUIT",       OnQuit,     new Color(0.15f, 0.15f, 0.18f), new Color(0.6f, 0.6f, 0.6f));
    }

    private void RefreshFonts()
    {
        TMP_FontAsset font = customFont;
        if (font == null && StoryManager.Instance != null)
            font = StoryManager.Instance.globalFont;

        if (font != null)
        {
            foreach (var t in GetComponentsInChildren<TMP_Text>(true))
            {
                t.font = font;
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static RectTransform MakeRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static TMP_Text MakeText(Transform parent, string name, float size, FontStyles style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(tmp);
        tmp.fontSize             = size;
        tmp.fontStyle            = style;
        tmp.color                = color;
        tmp.alignment            = TextAlignmentOptions.Center;
        tmp.enableWordWrapping   = true;
        tmp.characterSpacing     = style == FontStyles.Bold ? 8f : 2f;
        return tmp;
    }

    void MakeButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, Color bg, Color textColor)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 54f);

        var img = go.AddComponent<Image>();
        img.color = bg;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var cb = btn.colors;
        cb.normalColor      = bg;
        cb.highlightedColor = Color.Lerp(bg, Color.white, 0.2f);
        cb.pressedColor     = Color.Lerp(bg, Color.black, 0.2f);
        cb.fadeDuration     = 0.1f;
        btn.colors          = cb;

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var txtRt = textGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(tmp);
        tmp.text            = label;
        tmp.fontSize        = 15f;
        tmp.fontStyle       = FontStyles.Bold;
        tmp.alignment       = TextAlignmentOptions.Center;
        tmp.color           = textColor;
        tmp.characterSpacing = 3f;
    }
}
