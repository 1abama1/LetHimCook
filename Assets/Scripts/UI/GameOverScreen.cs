using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
public class GameOverScreen : MonoBehaviour
{
    public string restartSceneName = "";
    public float appearDelay = 0.4f;
    public float fadeInDuration = 0.9f;

    private Canvas _canvas;
    private CanvasGroup _rootGroup;
    private bool _built = false;
    private Image          _bg;
    private TMP_Text       _titleLabel;
    private TMP_Text       _subtitleLabel;
    private TMP_Text       _reasonLabel;
    private Button         _restartBtn;
    private Button         _quitBtn;
    private ParticleSystem _particles;

    private static readonly Color VictoryBg = new Color(0.04f, 0.06f, 0.10f, 1f);
    private static readonly Color VictoryAccent = new Color(1.00f, 0.84f, 0.25f, 1f);
    private static readonly Color VictoryTitle = new Color(1.00f, 0.92f, 0.55f, 1f);

    private static readonly Color DefeatBg = new Color(0.06f, 0.02f, 0.02f, 1f);
    private static readonly Color DefeatAccent = new Color(0.85f, 0.18f, 0.15f, 1f);
    private static readonly Color DefeatTitle = new Color(0.95f, 0.45f, 0.42f, 1f);

    void Awake()
    {
        BuildUI();
        if (_canvas != null) _canvas.gameObject.SetActive(false);
    }
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

    IEnumerator AppearSequence(bool isVictory)
    {
        Time.timeScale = 0f;

        if (ScreenFader.Instance != null)
            ScreenFader.Instance.SetBlack();

        yield return new WaitForSecondsRealtime(appearDelay);

        _canvas.gameObject.SetActive(true);
        _rootGroup.alpha = 0f;

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

        if (ScreenFader.Instance != null)
            ScreenFader.Instance.SetTransparent();

        if (isVictory && _particles != null)
            _particles.Play();
    }

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

    void ApplyTheme(bool isVictory)
    {
        Color bg     = isVictory ? VictoryBg     : DefeatBg;
        Color accent = isVictory ? VictoryAccent : DefeatAccent;
        Color title  = isVictory ? VictoryTitle  : DefeatTitle;

        _bg.color = bg;
        _titleLabel.color    = title;
        _subtitleLabel.color = accent;
        _reasonLabel.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);

        ColorBlock cb = _restartBtn.colors;
        cb.normalColor = accent;
        cb.highlightedColor = Color.Lerp(accent, Color.white, 0.25f);
        cb.pressedColor = Color.Lerp(accent, Color.black, 0.25f);
        _restartBtn.colors = cb;

        Transform line = _subtitleLabel.transform.Find("Line");
        if (line != null)
            line.GetComponent<Image>().color = accent;
    }

    void BuildUI()
    {
        if (_built) return;
        _built = true;

        GameObject canvasGO = new GameObject("[EndScreen]");
        canvasGO.transform.SetParent(transform, false);
        _canvas             = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10000; // выше всего

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        _rootGroup = canvasGO.AddComponent<CanvasGroup>();
        _rootGroup.alpha = 0f;
        _rootGroup.blocksRaycasts = true;

        _bg = MakeImage(canvasGO.transform, "BG", Color.black);
        Stretch(_bg.rectTransform);

        Image vignette = MakeImage(canvasGO.transform, "Vignette", Color.clear);
        Stretch(vignette.rectTransform);
        vignette.color = new Color(0f, 0f, 0f, 0.45f);

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRT.pivot            = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(860f, 540f);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.55f);

        Image border = MakeImage(panel.transform, "Border", new Color(1f, 1f, 1f, 0.06f));
        border.rectTransform.anchorMin = Vector2.zero;
        border.rectTransform.anchorMax = Vector2.one;
        border.rectTransform.offsetMin = new Vector2(-2f, -2f);
        border.rectTransform.offsetMax = new Vector2(2f, 2f);

        _titleLabel = MakeText(panel.transform, "Title",
            fontSize: 90f,
            style: FontStyles.Bold,
            alignment: TextAlignmentOptions.Center,
            color: VictoryTitle);
        RectTransform titleRT   = _titleLabel.rectTransform;
        titleRT.anchorMin       = new Vector2(0f, 0.62f);
        titleRT.anchorMax       = new Vector2(1f, 1.00f);
        titleRT.offsetMin = new Vector2(40f, 0f);
        titleRT.offsetMax = new Vector2(-40f, 0f);
        _titleLabel.characterSpacing = 18f;

        Image divider = MakeImage(panel.transform, "Divider", VictoryAccent);
        divider.gameObject.name = "Line";
        divider.rectTransform.anchorMin        = new Vector2(0.15f, 0.595f);
        divider.rectTransform.anchorMax        = new Vector2(0.85f, 0.605f);
        divider.rectTransform.offsetMin        = Vector2.zero;
        divider.rectTransform.offsetMax = Vector2.zero;

        _subtitleLabel = MakeText(panel.transform, "Subtitle",
            fontSize: 22f,
            style: FontStyles.Normal,
            alignment: TextAlignmentOptions.Center,
            color: VictoryAccent);
        RectTransform subRT = _subtitleLabel.rectTransform;
        subRT.anchorMin = new Vector2(0f, 0.46f);
        subRT.anchorMax = new Vector2(1f, 0.60f);
        subRT.offsetMin = new Vector2(60f, 0f);
        subRT.offsetMax = new Vector2(-60f, 0f);
        _subtitleLabel.characterSpacing = 6f;

        _reasonLabel = MakeText(panel.transform, "Reason",
            fontSize: 18f,
            style: FontStyles.Normal,
            alignment: TextAlignmentOptions.Center,
            color: new Color(0.8f, 0.8f, 0.8f, 0.9f));
        RectTransform reasonRT = _reasonLabel.rectTransform;
        reasonRT.anchorMin = new Vector2(0f, 0.25f);
        reasonRT.anchorMax = new Vector2(1f, 0.46f);
        reasonRT.offsetMin = new Vector2(80f, 0f);
        reasonRT.offsetMax = new Vector2(-80f, 0f);
        _reasonLabel.lineSpacing = 8f;

        float btnY = -160f;
        float btnW    = 220f;
        float btnH    = 54f;
        float btnGap = 24f;

        _restartBtn = MakeButton(panel.transform, "BtnRestart",
            label: "ПОВТОРИТЬ",
            anchoredPos: new Vector2(-(btnW * 0.5f + btnGap * 0.5f), btnY),
            size: new Vector2(btnW, btnH),
            bgColor: VictoryAccent,
            textColor: new Color(0.05f, 0.05f, 0.05f, 1f));
        _restartBtn.onClick.AddListener(OnRestartClicked);

        _quitBtn = MakeButton(panel.transform, "BtnQuit",
            label: "ВЫЙТИ",
            anchoredPos: new Vector2(btnW * 0.5f + btnGap * 0.5f, btnY),
            size: new Vector2(btnW, btnH),
            bgColor: new Color(0.15f, 0.15f, 0.15f, 1f),
            textColor: new Color(0.75f, 0.75f, 0.75f, 1f));
        _quitBtn.onClick.AddListener(OnQuitClicked);

        BuildParticles(canvasGO.transform);
    }

    void BuildParticles(Transform parent)
    {
        GameObject go = new GameObject("Confetti");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 700f, 0f);

        _particles = go.AddComponent<ParticleSystem>();
        _particles.Stop();

        var main = _particles.main;
        main.loop             = true;
        main.duration         = 5f;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(2.5f, 5f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(100f, 400f);
        main.startSize        = new ParticleSystem.MinMaxCurve(6f, 18f);
        main.startRotation    = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.gravityModifier  = 0.6f;
        main.maxParticles = 300;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

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
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

        static Button MakeButton(Transform parent, string name,
        string label,
        Vector2 anchoredPos, Vector2 size,
        Color bgColor, Color textColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;

        Image img = go.AddComponent<Image>();
        img.color = bgColor;
        Button btn = go.AddComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.normalColor     = bgColor;
        cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.22f);
        cb.pressedColor    = Color.Lerp(bgColor, Color.black, 0.22f);
        cb.fadeDuration = 0.12f;
        btn.colors = cb;
        btn.targetGraphic = img;

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
