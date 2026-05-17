using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class RecipeBookUI : MonoBehaviour
{
    public static RecipeBookUI Instance { get; private set; }

    [Header("Visual Settings")]
    public Sprite recipeSprite; // Фон книги
    [TextArea(10, 20)]
    public string recipesText = "1. BURGER: Bun + Patty(Fried)\n2. VODKA: VODKA(that's all u need)\n3. SALAD: Cucumber + Tomato";
    public Color panelColor = new Color(1f, 1f, 1f, 1f);
    public Color accentColor = new Color(1f, 0.84f, 0.25f); // Gold
    public TMP_FontAsset customFont;                       // Опциональный шрифт

    // UI Elements
    private GameObject _root;
    private GameObject _panel;
    private Image _recipeImage;
    private TMP_Text _recipesContent;
    private TMP_Text _debtTitle;
    private Transform _inventoryGrid;
    private bool _isOpen;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildUI();
        Close();
    }

    void Start()
    {
        RefreshFonts();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            Toggle();
        }

        if (_isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    public void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }

    public void Open()
    {
        _isOpen = true;
        if (_root != null) _root.SetActive(true);
        RefreshFonts();
        RefreshData();
        AudioManager.Instance?.Play("ui_open");
    }

    public void Close()
    {
        _isOpen = false;
        if (_root != null) _root.SetActive(false);
    }

    private void RefreshData()
    {
        if (StoryManager.Instance != null)
        {
            int paid = StoryManager.Instance.DebtPaid;
            int total = StoryManager.Instance.totalDebt;
            int daysLeft = StoryManager.Instance.totalDays - StoryManager.Instance.CurrentDay;
            _debtTitle.text = $"DEBT: ${paid} / ${total} | DAYS LEFT: {daysLeft}";
        }

        if (recipeSprite != null)
        {
            _recipeImage.sprite = recipeSprite;
            _recipeImage.gameObject.SetActive(true);
        }

        _recipesContent.text = recipesText;
    }

    private void BuildUI()
    {
        var canvasGo = new GameObject("RecipeBookCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        
        var sc = canvasGo.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Root container ──
        _root = new GameObject("RecipeUI_Root", typeof(RectTransform));
        _root.transform.SetParent(canvasGo.transform, false);
        Stretch(_root.GetComponent<RectTransform>());

        // Background Overlay
        var overlay = new GameObject("RecipeUI_Overlay", typeof(RectTransform));
        overlay.transform.SetParent(_root.transform, false);
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.4f);
        Stretch(overlay.GetComponent<RectTransform>());

        // Main Panel
        _panel = new GameObject("NotebookPanel", typeof(RectTransform));
        _panel.transform.SetParent(_root.transform, false);
        var panelImg = _panel.AddComponent<Image>();
        panelImg.color = panelColor;
        
        var rt = _panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(800, 600);

        // Header (Debt)
        var headerGo = new GameObject("Header", typeof(RectTransform));
        headerGo.transform.SetParent(_panel.transform, false);
        _debtTitle = headerGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(_debtTitle);
        _debtTitle.fontSize = 28f;
        _debtTitle.alignment = TextAlignmentOptions.Center;
        _debtTitle.color = accentColor;
        var hRt = headerGo.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0, 0.9f);
        hRt.anchorMax = new Vector2(1, 1);
        hRt.offsetMin = hRt.offsetMax = Vector2.zero;

        // Recipe Image
        var recipeGo = new GameObject("RecipeImage", typeof(RectTransform));
        recipeGo.transform.SetParent(_panel.transform, false);
        _recipeImage = recipeGo.AddComponent<Image>();
        _recipeImage.preserveAspect = true;
        var rRt = recipeGo.GetComponent<RectTransform>();
        rRt.anchorMin = Vector2.zero;
        rRt.anchorMax = new Vector2(1, 0.9f);
        rRt.offsetMin = new Vector2(20, 20);
        rRt.offsetMax = new Vector2(-20, 0);

        // Recipes Text Overlay
        var textGo = new GameObject("RecipesText", typeof(RectTransform));
        textGo.transform.SetParent(recipeGo.transform, false);
        _recipesContent = textGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(_recipesContent);
        _recipesContent.fontSize = 22f;
        _recipesContent.color = new Color(0.15f, 0.15f, 0.15f); // Темный текст для "бумаги"
        _recipesContent.alignment = TextAlignmentOptions.TopLeft;
        _recipesContent.enableWordWrapping = true;
        var tRt = textGo.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = new Vector2(40, 40); // Отступы от краев спрайта
        tRt.offsetMax = new Vector2(-40, -40);

        // Close Hint
        var hintGo = new GameObject("Hint", typeof(RectTransform));
        hintGo.transform.SetParent(_panel.transform, false);
        var hint = hintGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(hint);
        hint.text = "[Tab] to close";
        hint.fontSize = 14f;
        hint.alignment = TextAlignmentOptions.Right;
        hint.color = new Color(1, 1, 1, 0.5f);
        var hintRt = hintGo.GetComponent<RectTransform>();
        hintRt.anchorMin = hintRt.anchorMax = new Vector2(1, 0);
        hintRt.pivot = new Vector2(1, 0);
        hintRt.anchoredPosition = new Vector2(-10, 10);
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

    private void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
