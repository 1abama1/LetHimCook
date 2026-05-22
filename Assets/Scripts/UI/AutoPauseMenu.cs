using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class AutoPauseMenu : MonoBehaviour
{
    private static AutoPauseMenu _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Application.isPlaying && _instance == null)
        {
            var go = new GameObject("AutoPauseMenu");
            DontDestroyOnLoad(go);
            go.AddComponent<AutoPauseMenu>();
        }
    }

    private GameObject _pauseCanvasGo;
    private GameObject _mainPanel;
    private GameObject _settingsPanel;
    private bool _isPaused = false;
    private float _savedTimeScale = 1f;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void Start()
    {
        BuildUI();
        _pauseCanvasGo.SetActive(false);
    }

    // Check if any game UI (shop, story summary) is currently blocking
    bool IsGameUIBlocking()
    {
        if (ShopUI.Instance != null && ShopUI.Instance.IsShopping) return true;
        if (StoryManager.Instance != null && StoryManager.Instance.IsShowingSummary) return true;
        return false;
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_isPaused)
            {
                if (_settingsPanel.activeSelf)
                    CloseSettings();
                else
                    ResumeGame();
            }
            else
            {
                if (!IsGameUIBlocking())
                    PauseGame();
            }
        }
    }

    void PauseGame()
    {
        _isPaused = true;
        _savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        _pauseCanvasGo.SetActive(true);
        _mainPanel.SetActive(true);
        _settingsPanel.SetActive(false);
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = _savedTimeScale;
        _pauseCanvasGo.SetActive(false);
    }

    void OpenSettings()
    {
        _mainPanel.SetActive(false);
        _settingsPanel.SetActive(true);
    }

    void CloseSettings()
    {
        _settingsPanel.SetActive(false);
        _mainPanel.SetActive(true);
    }

    void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    void BuildUI()
    {
        _pauseCanvasGo = new GameObject("PauseCanvas");
        _pauseCanvasGo.transform.SetParent(this.transform);
        
        var canvas = _pauseCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        var scaler = _pauseCanvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        _pauseCanvasGo.AddComponent<GraphicRaycaster>();

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            DontDestroyOnLoad(esGo);
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        // Main Panel
        _mainPanel = CreatePanel(_pauseCanvasGo.transform, new Color(0, 0, 0, 0.85f));
        CreateText(_mainPanel.transform, "PAUSE", new Vector2(0, 300), 80);
        
        CreateButton(_mainPanel.transform, "RESUME", new Vector2(0, 50), () => ResumeGame());
        CreateButton(_mainPanel.transform, "SETTINGS", new Vector2(0, -50), () => OpenSettings());
        CreateButton(_mainPanel.transform, "QUIT GAME", new Vector2(0, -150), () => QuitGame());

        CreateControlsText(_mainPanel.transform);

        // Settings Panel
        _settingsPanel = CreatePanel(_pauseCanvasGo.transform, new Color(0.1f, 0.1f, 0.1f, 0.95f));
        CreateText(_settingsPanel.transform, "SETTINGS", new Vector2(0, 300), 80);
        
        CreateText(_settingsPanel.transform, "MUSIC VOLUME", new Vector2(0, 100), 40);
        var musicSlider = CreateSlider(_settingsPanel.transform, new Vector2(0, 50));
        musicSlider.value = AudioManager.Instance != null ? AudioManager.Instance.musicVolume : 0.5f;
        musicSlider.onValueChanged.AddListener(v => {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v);
        });

        CreateText(_settingsPanel.transform, "MASTER VOLUME", new Vector2(0, -50), 40);
        var masterSlider = CreateSlider(_settingsPanel.transform, new Vector2(0, -100));
        masterSlider.value = AudioListener.volume;
        masterSlider.onValueChanged.AddListener(v => {
            AudioListener.volume = v;
        });

        CreateButton(_settingsPanel.transform, "BACK", new Vector2(0, -250), () => CloseSettings());

        _settingsPanel.SetActive(false);
    }

    GameObject CreatePanel(Transform parent, Color color)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return go;
    }

    void CreateText(Transform parent, string text, Vector2 pos, float fontSize)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(800, 100);
        
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;

        if (StoryManager.Instance != null && StoryManager.Instance.globalFont != null)
        {
            txt.font = StoryManager.Instance.globalFont;
        }
    }

    void CreateControlsText(Transform parent)
    {
        var go = new GameObject("ControlsText");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(50, 50);
        rt.sizeDelta = new Vector2(600, 400);
        
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = "CONTROLS\nWASD - Move\nLShift - Sprint\nLCtrl - Crouch\nF - Hide\nE - Interact\nQ - Kill\n1,2 / Mouse - Dialogue Choice";
        txt.fontSize = 28f;
        txt.alignment = TextAlignmentOptions.BottomLeft;
        txt.color = new Color(1f, 1f, 1f, 0.7f);

        if (StoryManager.Instance != null && StoryManager.Instance.globalFont != null)
        {
            txt.font = StoryManager.Instance.globalFont;
        }
    }

    void CreateButton(Transform parent, string text, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject("Button");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(350, 70);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(action);

        var cb = btn.colors;
        cb.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        btn.colors = cb;

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;
        
        var txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.fontSize = 35f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;

        if (StoryManager.Instance != null && StoryManager.Instance.globalFont != null)
        {
            txt.font = StoryManager.Instance.globalFont;
        }
    }

    Slider CreateSlider(Transform parent, Vector2 pos)
    {
        var go = new GameObject("Slider");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400, 20);

        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        var bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0f);
        bgRt.anchorMax = new Vector2(1, 1f);
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);

        var fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(go.transform, false);
        var faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0, 0f);
        faRt.anchorMax = new Vector2(1, 1f);
        faRt.offsetMin = faRt.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        fill.AddComponent<Image>().color = new Color(0.2f, 0.8f, 0.3f, 1f);

        var handleArea = new GameObject("HandleArea");
        handleArea.transform.SetParent(go.transform, false);
        var haRt = handleArea.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.offsetMin = haRt.offsetMax = Vector2.zero;

        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var hRt = handle.AddComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(20, 40);
        hRt.anchorMin = new Vector2(0, 0.5f);
        hRt.anchorMax = new Vector2(0, 0.5f);
        handle.AddComponent<Image>().color = Color.white;

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = hRt;
        slider.minValue = 0f;
        slider.maxValue = 1f;

        return slider;
    }
}
