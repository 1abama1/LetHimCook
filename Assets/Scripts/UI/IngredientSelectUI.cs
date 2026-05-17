using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
public class IngredientSelectUI : MonoBehaviour
{
    public static IngredientSelectUI Instance { get; private set; }
    public bool IsOpen { get; private set; }

    private GameObject    _canvasGo;
    private GameObject    _panel;
    private Transform     _grid;
    private PlayerController _pendingPlayer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
        if (_canvasGo != null) _canvasGo.SetActive(false);
    }

    void Update()
    {
        if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    public void Open(PlayerController player)
    {
        if (player.HeldIngredient != null || player.HeldPlate != null)
        {
            return;
        }
        _pendingPlayer = player;
        RefreshGrid();
        if (_canvasGo != null) _canvasGo.SetActive(true);
        IsOpen = true;
    }

    public void Close()
    {
        if (_canvasGo != null) _canvasGo.SetActive(false);
        IsOpen = false;
        _pendingPlayer = null;
    }

    public void Toggle(PlayerController player)
    {
        if (IsOpen) Close();
        else        Open(player);
    }

    private void RefreshGrid()
    {
        if (_grid == null) return;

        foreach (Transform child in _grid)
        {
            if (child != null) Destroy(child.gameObject);
        }

        // ДОБАВЛЕНО: Проверяем не только синглтон, но и сам список на null
        if (PlayerEconomy.Instance == null || PlayerEconomy.Instance.inventory == null) return;

        bool any = false;
        foreach (var stack in PlayerEconomy.Instance.inventory)
        {
            // КРИТИЧЕСКАЯ ЗАЩИТА: Проверяем, что сам элемент stack не равен null
            if (stack == null || stack.ingredient == null || stack.amount <= 0) continue;
            
            CreateButton(stack.ingredient, stack.amount);
            any = true;
        }

        if (!any)
        {
            var emptyGo  = new GameObject("Empty", typeof(RectTransform));
            emptyGo.transform.SetParent(_grid, false);
            var emptyTmp = emptyGo.AddComponent<TextMeshProUGUI>();
            StoryManager.ApplyFont(emptyTmp);
            emptyTmp.text      = "Inventory is empty";
            emptyTmp.fontSize  = 18f;
            emptyTmp.alignment = TextAlignmentOptions.Center;
            emptyTmp.color     = new Color(0.6f, 0.6f, 0.6f);
        }
    }

    private void CreateButton(IngredientData ingredient, int count)
    {
        var card    = new GameObject("Card_" + ingredient.ingredientName, typeof(RectTransform));
        card.transform.SetParent(_grid, false);

        var cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.18f, 0.18f, 0.23f, 1f);

        var le = card.AddComponent<LayoutElement>();
        le.preferredWidth  = 120f;
        le.preferredHeight = 140f;

        var btn    = card.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
        colors.pressedColor     = new Color(0.75f, 0.75f, 0.75f);
        btn.colors = colors;

        var captured = ingredient;
        btn.onClick.AddListener(() => OnSelect(captured));

        var iconGo  = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(card.transform, false);
        var iconImg  = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        if (ingredient.sprite != null) iconImg.sprite = ingredient.sprite;
        else iconImg.color = new Color(0.4f, 0.4f, 0.5f);

        var iconRect = iconImg.rectTransform;
        iconRect.anchorMin = new Vector2(0.1f, 0.32f);
        iconRect.anchorMax = new Vector2(0.9f, 0.93f);
        iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;

        var nameGo  = new GameObject("Name", typeof(RectTransform));
        nameGo.transform.SetParent(card.transform, false);
        var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(nameTmp);
        nameTmp.text      = ingredient.ingredientName;
        nameTmp.fontSize  = 11f;
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.color     = Color.white;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;

        var nameRect = nameTmp.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 0.30f);
        nameRect.offsetMin = new Vector2(4, 2);
        nameRect.offsetMax = new Vector2(-4, -2);

        var badgeGo  = new GameObject("Badge", typeof(RectTransform));
        badgeGo.transform.SetParent(card.transform, false);
        var badgeImg = badgeGo.AddComponent<Image>();
        badgeImg.color = new Color(0.15f, 0.65f, 0.35f, 1f);

        var badgeRect = badgeImg.rectTransform;
        badgeRect.anchorMin = new Vector2(0.62f, 0.78f);
        badgeRect.anchorMax = new Vector2(1f,    1f);
        badgeRect.offsetMin = badgeRect.offsetMax = Vector2.zero;

        var cntGo  = new GameObject("Cnt", typeof(RectTransform));
        cntGo.transform.SetParent(badgeGo.transform, false);
        var cntTmp = cntGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(cntTmp);
        cntTmp.text      = count.ToString();
        cntTmp.fontSize  = 14f;
        cntTmp.fontStyle = FontStyles.Bold;
        cntTmp.alignment = TextAlignmentOptions.Center;
        cntTmp.color     = Color.white;

        var cntRect = cntTmp.rectTransform;
        cntRect.anchorMin = Vector2.zero;
        cntRect.anchorMax = Vector2.one;
        cntRect.offsetMin = cntRect.offsetMax = Vector2.zero;
    }

    private void OnSelect(IngredientData ingredient)
    {
        if (_pendingPlayer == null) { Close(); return; }

        var economy = PlayerEconomy.Instance;
        if (economy == null || economy.GetIngredientCount(ingredient) <= 0)
        {
            RefreshGrid();
            return;
        }

        economy.RemoveIngredientOfType(ingredient);
        var copy = Instantiate(ingredient);
        _pendingPlayer.GiveIngredient(copy);

        AudioManager.Instance?.PlayAt("cabinet_open", _pendingPlayer.transform.position);
        Close();
    }

    private void BuildUI()
    {
        _canvasGo = new GameObject("IngSelectCanvas", typeof(RectTransform));
        _canvasGo.transform.SetParent(transform);
        var cv = _canvasGo.AddComponent<Canvas>();
        cv.renderMode    = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder  = 200;
        var cvs = _canvasGo.AddComponent<CanvasScaler>();
        cvs.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cvs.referenceResolution    = new Vector2(1920, 1080);
        cvs.matchWidthOrHeight     = 0.5f;
        _canvasGo.AddComponent<GraphicRaycaster>();

        var overlayGo  = new GameObject("Overlay", typeof(RectTransform));
        overlayGo.transform.SetParent(_canvasGo.transform, false);
        var overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.6f);
        Stretch(overlayImg.rectTransform);
        var overlayBtn = overlayGo.AddComponent<Button>();
        overlayBtn.transition = Selectable.Transition.None;
        overlayBtn.onClick.AddListener(Close);

        _panel = new GameObject("Panel", typeof(RectTransform));
        _panel.transform.SetParent(_canvasGo.transform, false);
        var panelImg  = _panel.AddComponent<Image>();
        panelImg.color = new Color(0.10f, 0.10f, 0.14f, 0.97f);
        var panelRect  = panelImg.rectTransform;
        panelRect.anchorMin = new Vector2(0.2f, 0.12f);
        panelRect.anchorMax = new Vector2(0.8f, 0.88f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

        var titleGo  = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(_panel.transform, false);
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(titleTmp);
        titleTmp.text      = "SELECT INGREDIENT";
        titleTmp.fontSize  = 24f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color     = Color.white;
        var titleRect = titleTmp.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0.88f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;

        var hintGo  = new GameObject("Hint", typeof(RectTransform));
        hintGo.transform.SetParent(_panel.transform, false);
        var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(hintTmp);
        hintTmp.text      = "[Esc] or click outside to close";
        hintTmp.fontSize  = 13f;
        hintTmp.alignment = TextAlignmentOptions.Center;
        hintTmp.color     = new Color(0.55f, 0.55f, 0.6f);
        var hintRect = hintTmp.rectTransform;
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0.09f);
        hintRect.offsetMin = hintRect.offsetMax = Vector2.zero;

        var scrollGo   = new GameObject("Scroll", typeof(RectTransform));
        scrollGo.transform.SetParent(_panel.transform, false);
        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        var scrollR    = scrollGo.GetComponent<RectTransform>();
        scrollR.anchorMin = new Vector2(0.01f, 0.1f);
        scrollR.anchorMax = new Vector2(0.99f, 0.87f);
        scrollR.offsetMin = scrollR.offsetMax = Vector2.zero;
        var scrollBg = scrollGo.AddComponent<Image>();
        scrollBg.color = new Color(0.06f, 0.06f, 0.09f, 0.4f);

        var contentGo   = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(scrollGo.transform, false);
        var contentRect = contentGo.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot     = new Vector2(0.5f, 1f);
        contentRect.offsetMin = contentRect.offsetMax = Vector2.zero;

        var glg = contentGo.AddComponent<GridLayoutGroup>();
        glg.cellSize       = new Vector2(120f, 140f);
        glg.spacing        = new Vector2(12f, 12f);
        glg.padding        = new RectOffset(16, 16, 16, 16);
        glg.childAlignment = TextAnchor.UpperCenter;

        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content    = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical   = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        _grid = contentGo.transform;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
