using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// GameOverScreen — программно строит экран победы/поражения.
/// Вызывается из StoryManager: ShowVictory() / ShowGameOver(reason).
/// Две кнопки: "Повторить" и "Выйти".
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    // ── Настройки ────────────────────────────────────────────────────────────
    [Header("Scene Settings")]
    [Tooltip("Имя сцены для рестарта (оставь пустым — перезагрузит текущую)")]
    public string restartSceneName = "";

    [Header("Timing")]
    [Tooltip("Задержка перед появлением экрана (секунды реального времени)")]
    public float appearDelay    = 0.4f;
    [Tooltip("Время появления всего экрана")]
    public float fadeInDuration = 0.9f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private Canvas     _canvas;
    private CanvasGroup _rootGroup;
    private bool       _built = false;

    // Ссылки на элементы (заполняются в Build)
    private Image          _bg;
    private TMP_Text       _titleLabel;
    private TMP_Text       _subtitleLabel;
    private TMP_Text       _reasonLabel;
    private Button         _restartBtn;
    private Button         _quitBtn;
    private ParticleSystem _particles;

    // Палитра победы
    private static readonly Color VictoryBg      = new Color(0.04f, 0.06f, 0.10f, 1f);
    private static readonly Color VictoryAccent  = new Color(1.00f, 0.84f, 0.25f, 1f);  // золото
    private static readonly Color VictoryTitle   = new Color(1.00f, 0.92f, 0.55f, 1f);

    // Палитра поражения
    private static readonly Color DefeatBg       = new Color(0.06f, 0.02f, 0.02f, 1f);
    private static readonly Color DefeatAccent   = new Color(0.85f, 0.18f, 0.15f, 1f);  // кровавый красный
    private static readonly Color DefeatTitle    = new Color(0.95f, 0.45f, 0.42f, 1f);

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        BuildUI();
        if (_canvas != null) _canvas.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────
    public void ShowVictory()
    {
        ApplyTheme(isVictory: true);
        _titleLabel.text    = "БРАТЬЯ ДОВОЛЬНЫ";
        _subtitleLabel.text = "Долг погашен. Ты выжил.";
        _reasonLabel.text   = "Три ночи, три дня.\nТы сделал это.";
        StartCoroutine(AppearSequence(isVictory: true));
    }

    public void ShowGameOver(string reason = "")
    {
        ApplyTheme(isVictory: false);
        _titleLabel.text    = "КОНЕЦ";
        _subtitleLabel.text = "Братья Мамытовы недовольны.";
        _reasonLabel.text   = string.IsNullOrEmpty(reason)
            ? "Ты не справился." : reason;
        StartCoroutine(AppearSequence(isVictory: false));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Sequence
    // ─────────────────────────────────────────────────────────────────────────
    IEnumerator AppearSequence(bool isVictory)
    {
        Time.timeScale = 0f;

        // Убедимся что ScreenFader чёрный
        if (ScreenFader.Instance != null)
            ScreenFader.Instance.SetBlack();

        yield return new WaitForSecondsRealtime(appearDelay);

        _canvas.gameObject.SetActive(true);
        _rootGroup.alpha = 0f;

        // Fade in поверх чёрного
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeInDuration;
            float smooth = t * t * (3f - 2f * t);
            _rootGroup.alpha = smooth;
            yield return null;
        }
        _rootGroup.alpha = 1f;

        // Убираем затемнение ScreenFader (наш экран теперь сверху)
        if (ScreenFader.Instance != null)
            ScreenFader.Instance.SetTransparent();

        if (isVictory && _particles != null)
            _particles.Play();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Button handlers
    // ─────────────────────────────────────────────────────────────────────────
    void OnRestartClicked()
    {
        Time.timeScale = 1f;
        string scene = string.IsNullOrEmpty(restartSceneName)
            ? SceneManager.GetActiveScene().name
            : restartSceneName;
        SceneManager.LoadScene(scene);
    }

    void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Theme
    // ─────────────────────────────────────────────────────────────────────────
    void ApplyTheme(bool isVictory)
    {
        Color bg     = isVictory ? VictoryBg     : DefeatBg;
        Color accent = isVictory ? VictoryAccent : DefeatAccent;
        Color title  = isVictory ? VictoryTitle  : DefeatTitle;

        _bg.color = bg;
        _titleLabel.color    = title;
        _subtitleLabel.color = accent;
        _reasonLabel.color   = new Color(0.8f, 0.8f, 0.8f, 0.9f);

        // Кнопка "Повторить"
        ColorBlock cb = _restartBtn.colors;
        cb.normalColor      = accent;
        cb.highlightedColor = Color.Lerp(accent, Color.white, 0.25f);
        cb.pressedColor     = Color.Lerp(accent, Color.black, 0.25f);
        _restartBtn.colors  = cb;

        // Декоративная линия (дочерний Image у subtitle)
        Transform line = _subtitleLabel.transform.Find("Line");
        if (line != null)
            line.GetComponent<Image>().color = accent;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  UI Builder
    // ─────────────────────────────────────────────────────────────────────────
    void BuildUI()
    {
        if (_built) return;
        _built = true;

        // ── Canvas ────────────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("[EndScreen]");
        canvasGO.transform.SetParent(transform, false);
        _canvas             = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10000; // выше всего

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Root CanvasGroup ──────────────────────────────────────────────────
        _rootGroup               = canvasGO.AddComponent<CanvasGroup>();
        _rootGroup.alpha         = 0f;
        _rootGroup.blocksRaycasts = true;

        // ── Фон ──────────────────────────────────────────────────────────────
        _bg = MakeImage(canvasGO.transform, "BG", Color.black);
        Stretch(_bg.rectTransform);

        // Тонкий шум-оверлей (градиент снизу)
        Image vignette = MakeImage(canvasGO.transform, "Vignette", Color.clear);
        Stretch(vignette.rectTransform);
        vignette.color = new Color(0f, 0f, 0f, 0.45f);

        // ── Центральная панель ────────────────────────────────────────────────
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRT.pivot            = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta        = new Vector2(860f, 540f);
        panelRT.anchoredPosition = Vector2.zero;

        // Полупрозрачный фон панели
        Image panelBg   = panel.AddComponent<Image>();
        panelBg.color   = new Color(0f, 0f, 0f, 0.55f);

        // Тонкая рамка панели
        Image border = MakeImage(panel.transform, "Border", new Color(1f, 1f, 1f, 0.06f));
        border.rectTransform.anchorMin        = Vector2.zero;
        border.rectTransform.anchorMax        = Vector2.one;
        border.rectTransform.offsetMin        = new Vector2(-2f, -2f);
        border.rectTransform.offsetMax        = new Vector2( 2f,  2f);

        // ── Заголовок ─────────────────────────────────────────────────────────
        _titleLabel = MakeText(panel.transform, "Title",
            fontSize: 90f,
            style: FontStyles.Bold,
            alignment: TextAlignmentOptions.Center,
            color: VictoryTitle);
        RectTransform titleRT   = _titleLabel.rectTransform;
        titleRT.anchorMin       = new Vector2(0f, 0.62f);
        titleRT.anchorMax       = new Vector2(1f, 1.00f);
        titleRT.offsetMin       = new Vector2(40f,  0f);
        titleRT.offsetMax       = new Vector2(-40f, 0f);
        // Широкий letter-spacing через characterSpacing
        _titleLabel.characterSpacing = 18f;

        // ── Декоративная линия ────────────────────────────────────────────────
        Image divider = MakeImage(panel.transform, "Divider", VictoryAccent);
        divider.gameObject.name = "Line"; // Чтобы ApplyTheme нашел
        divider.rectTransform.anchorMin        = new Vector2(0.15f, 0.595f);
        divider.rectTransform.anchorMax        = new Vector2(0.85f, 0.605f);
        divider.rectTransform.offsetMin        = Vector2.zero;
        divider.rectTransform.offsetMax        = Vector2.zero;

        // ── Подзаголовок ──────────────────────────────────────────────────────
        _subtitleLabel = MakeText(panel.transform, "Subtitle",
            fontSize: 22f,
            style: FontStyles.Normal,
            alignment: TextAlignmentOptions.Center,
            color: VictoryAccent);
        RectTransform subRT   = _subtitleLabel.rectTransform;
        subRT.anchorMin       = new Vector2(0f, 0.46f);
        subRT.anchorMax       = new Vector2(1f, 0.60f);
        subRT.offsetMin       = new Vector2(60f, 0f);
        subRT.offsetMax       = new Vector2(-60f, 0f);
        _subtitleLabel.characterSpacing = 6f;

        // ── Причина/описание ──────────────────────────────────────────────────
        _reasonLabel = MakeText(panel.transform, "Reason",
            fontSize: 18f,
            style: FontStyles.Normal,
            alignment: TextAlignmentOptions.Center,
            color: new Color(0.8f, 0.8f, 0.8f, 0.9f));
        RectTransform reasonRT = _reasonLabel.rectTransform;
        reasonRT.anchorMin     = new Vector2(0f, 0.25f);
        reasonRT.anchorMax     = new Vector2(1f, 0.46f);
        reasonRT.offsetMin     = new Vector2(80f, 0f);
        reasonRT.offsetMax     = new Vector2(-80f, 0f);
        _reasonLabel.lineSpacing = 8f;

        // ── Кнопки ────────────────────────────────────────────────────────────
        float btnY    = -160f; // относительно центра панели
        float btnW    = 220f;
        float btnH    = 54f;
        float btnGap  = 24f;

        // Кнопка "Повторить"
        _restartBtn = MakeButton(panel.transform, "BtnRestart",
            label: "ПОВТОРИТЬ",
            anchoredPos: new Vector2(-(btnW * 0.5f + btnGap * 0.5f), btnY),
            size: new Vector2(btnW, btnH),
            bgColor: VictoryAccent,
            textColor: new Color(0.05f, 0.05f, 0.05f, 1f));
        _restartBtn.onClick.AddListener(OnRestartClicked);

        // Кнопка "Выйти"
        _quitBtn = MakeButton(panel.transform, "BtnQuit",
            label: "ВЫЙТИ",
            anchoredPos: new Vector2(btnW * 0.5f + btnGap * 0.5f, btnY),
            size: new Vector2(btnW, btnH),
            bgColor: new Color(0.15f, 0.15f, 0.15f, 1f),
            textColor: new Color(0.75f, 0.75f, 0.75f, 1f));
        _quitBtn.onClick.AddListener(OnQuitClicked);

        // ── Частицы (конфетти для победы) ────────────────────────────────────
        BuildParticles(canvasGO.transform);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Particle builder
    // ─────────────────────────────────────────────────────────────────────────
    void BuildParticles(Transform parent)
    {
        GameObject go = new GameObject("Confetti");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 700f, 0f);

        _particles = go.AddComponent<ParticleSystem>();

        var main = _particles.main;
        main.loop             = true;
        main.duration         = 5f;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(2.5f, 5f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(100f, 400f);
        main.startSize        = new ParticleSystem.MinMaxCurve(6f, 18f);
        main.startRotation    = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.gravityModifier  = 0.6f;
        main.maxParticles     = 300;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;

        // Цвет — золото + белый
        var col = _particles.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f,  0.84f, 0.25f), 0f),
                new GradientColorKey(new Color(1f,  1f,   1f),    0.5f),
                new GradientColorKey(new Color(0.9f,0.7f, 0.2f),  1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var emission = _particles.emission;
        emission.rateOverTime = 60f;

        var shape = _particles.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(1600f, 10f, 1f);

        var renderer = _particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode  = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 10001;

        _particles.Stop();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  UI helper factories
    // ─────────────────────────────────────────────────────────────────────────
    static Image MakeImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static TMP_Text MakeText(Transform parent, string name,
        float fontSize, FontStyles style,
        TextAlignmentOptions alignment, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(t);
        t.fontSize  = fontSize;
        t.fontStyle = style;
        t.alignment = alignment;
        t.color     = color;
        t.enableWordWrapping = true;
        return t;
    }

    static Button MakeButton(Transform parent, string name,
        string label,
        Vector2 anchoredPos, Vector2 size,
        Color bgColor, Color textColor)
    {
        // Контейнер кнопки
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;

        Image img   = go.AddComponent<Image>();
        img.color   = bgColor;
        Button btn  = go.AddComponent<Button>();

        // ColorBlock с плавной подсветкой
        ColorBlock cb      = btn.colors;
        cb.normalColor     = bgColor;
        cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.22f);
        cb.pressedColor    = Color.Lerp(bgColor, Color.black, 0.22f);
        cb.fadeDuration    = 0.12f;
        btn.colors         = cb;
        btn.targetGraphic  = img;

        // Текст кнопки
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        TMP_Text txt        = labelGO.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(txt);
        txt.text            = label;
        txt.fontSize        = 15f;
        txt.fontStyle       = FontStyles.Bold;
        txt.alignment       = TextAlignmentOptions.Center;
        txt.color           = textColor;
        txt.characterSpacing = 4f;

        RectTransform lblRT = labelGO.GetComponent<RectTransform>();
        lblRT.anchorMin     = Vector2.zero;
        lblRT.anchorMax     = Vector2.one;
        lblRT.offsetMin     = Vector2.zero;
        lblRT.offsetMax     = Vector2.zero;

        return btn;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
