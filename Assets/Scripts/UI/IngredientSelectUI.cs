using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class IngredientSelectUI : MonoBehaviour
{
    public static IngredientSelectUI Instance { get; private set; }
    public bool IsOpen { get; private set; }

    private GameObject       _root;
    private GameObject       _panel;
    private Transform        _grid;
    private PlayerController _pendingPlayer;
    private Canvas           _canvas;

    void Awake()
    {
        Debug.Log("[IngredientSelectUI] Awake: Starting initialization...");
        if (Instance != null && Instance != this) { 
            Debug.LogWarning("[IngredientSelectUI] Awake: Duplicate instance found, destroying.");
            Destroy(gameObject); 
            return; 
        }
        Instance = this;

        EnsureEventSystem();
        BuildUI();
        Close(); // Изначально закрыто
        Debug.Log("[IngredientSelectUI] Awake: Initialization complete.");
    }

    void Update()
    {
        if (IsOpen)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();

            // ДЕБАГ: Проверяем, что видит мышь
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                var eventData = new PointerEventData(EventSystem.current);
                eventData.position = Pointer.current.position.ReadValue();
                var results = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);
                
                Debug.Log($"[IngredientSelectUI] Клик! Попали в {results.Count} объектов:");
                for (int i = 0; i < results.Count; i++)
                {
                    Debug.Log($"--- [{i}] {results[i].gameObject.name} (Layer: {LayerMask.LayerToName(results[i].gameObject.layer)})");
                }
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open(PlayerController player)
    {
        Debug.Log($"[IngredientSelectUI] Open requested for player: {player.name}");
        if (player.HeldIngredient != null || player.HeldPlate != null) {
            Debug.LogWarning("[IngredientSelectUI] Open denied: Player hands are full.");
            return;
        }
        _pendingPlayer = player;
        RefreshGrid();
        _root.SetActive(true);
        IsOpen = true;
        Debug.Log("[IngredientSelectUI] UI is now OPEN.");
    }

    public void Close()
    {
        if (_root != null) _root.SetActive(false);
        IsOpen = false;
        _pendingPlayer = null;
    }

    public void Toggle(PlayerController player)
    {
        if (IsOpen) Close();
        else        Open(player);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void RefreshGrid()
    {
        Debug.Log("[IngredientSelectUI] Refreshing Grid...");
        if (_grid == null) { Debug.LogError("[IngredientSelectUI] Grid Transform is NULL!"); return; }

        int childCount = _grid.childCount;
        foreach (Transform child in _grid)
            if (child != null) Destroy(child.gameObject);
        Debug.Log($"[IngredientSelectUI] Cleared {childCount} existing items.");

        if (PlayerEconomy.Instance == null) { Debug.LogError("[IngredientSelectUI] PlayerEconomy.Instance is NULL!"); return; }
        if (PlayerEconomy.Instance.inventory == null) { Debug.LogError("[IngredientSelectUI] Inventory list is NULL!"); return; }

        int totalStacks = PlayerEconomy.Instance.inventory.Count;
        Debug.Log($"[IngredientSelectUI] Scanning inventory ({totalStacks} stacks)...");

        bool any = false;
        int buttonsCreated = 0;
        foreach (var stack in PlayerEconomy.Instance.inventory)
        {
            if (stack == null || stack.ingredient == null || stack.amount <= 0) continue;
            CreateButton(stack.ingredient, stack.amount);
            any = true;
            buttonsCreated++;
        }

        Debug.Log($"[IngredientSelectUI] Grid refreshed. Buttons created: {buttonsCreated}");

        if (!any)
        {
            Debug.Log("[IngredientSelectUI] Inventory is empty, showing 'Empty' label.");
            var go  = new GameObject("Empty", typeof(RectTransform));
            go.transform.SetParent(_grid, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            StoryManager.ApplyFont(tmp);
            tmp.text          = "Inventory is empty";
            tmp.fontSize      = 18f;
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.color         = new Color(0.6f, 0.6f, 0.6f);
            tmp.raycastTarget = false;
        }
    }

    private void CreateButton(IngredientData ingredient, int count)
    {
        // ── Card root ─────────────────────────────────────────────────────────
        var card    = new GameObject("Card_" + ingredient.ingredientName, typeof(RectTransform));
        card.transform.SetParent(_grid, false);

        var cardImg = card.AddComponent<Image>();
        cardImg.color = ingredient.IsSafe ? new Color(0.15f, 0.25f, 0.18f, 1f) : new Color(0.28f, 0.15f, 0.15f, 1f);

        var le = card.AddComponent<LayoutElement>();
        le.preferredWidth  = 120f;
        le.preferredHeight = 140f;

        var btn = card.AddComponent<Button>();
        btn.targetGraphic = cardImg;

        var cols = btn.colors;
        cols.normalColor      = Color.white;
        cols.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        cols.pressedColor     = new Color(0.75f, 0.75f, 0.75f);
        btn.colors = cols;

        var captured = ingredient;
        btn.onClick.AddListener(() => {
            Debug.Log($"[IngredientSelectUI] Button Clicked: {captured.ingredientName}");
            OnSelect(captured);
        });

        // ── Icon ──────────────────────────────────────────────────────────────
        var iconGo  = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(card.transform, false);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;
        if (ingredient.sprite != null) iconImg.sprite = ingredient.sprite;
        else                           iconImg.color  = new Color(0.4f, 0.4f, 0.5f);

        var ir = iconImg.rectTransform;
        ir.anchorMin = new Vector2(0.1f, 0.32f);
        ir.anchorMax = new Vector2(0.9f, 0.93f);
        ir.offsetMin = ir.offsetMax = Vector2.zero;

        // ── Name label ────────────────────────────────────────────────────────
        var nameGo  = new GameObject("Name", typeof(RectTransform));
        nameGo.transform.SetParent(card.transform, false);
        var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(nameTmp);
        nameTmp.text          = ingredient.ingredientName;
        nameTmp.fontSize      = 11f;
        nameTmp.alignment     = TextAlignmentOptions.Center;
        nameTmp.color         = Color.white;
        nameTmp.overflowMode  = TextOverflowModes.Ellipsis;
        nameTmp.raycastTarget = false;

        var nr = nameTmp.rectTransform;
        nr.anchorMin = new Vector2(0f, 0.16f);
        nr.anchorMax = new Vector2(1f, 0.32f);
        nr.offsetMin = new Vector2(4, 2);
        nr.offsetMax = new Vector2(-4, -2);

        // ── Type label ────────────────────────────────────────────────────────
        var typeGo  = new GameObject("Type", typeof(RectTransform));
        typeGo.transform.SetParent(card.transform, false);
        var typeTmp = typeGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(typeTmp);
        typeTmp.text          = ingredient.IsSafe ? "<color=#55FF55>Fresh</color>" : "<color=#FF5555>Spoiled</color>";
        typeTmp.fontSize      = 11f;
        typeTmp.alignment     = TextAlignmentOptions.Center;
        typeTmp.raycastTarget = false;

        var tr = typeTmp.rectTransform;
        tr.anchorMin = new Vector2(0f, 0f);
        tr.anchorMax = new Vector2(1f, 0.16f);
        tr.offsetMin = new Vector2(4, 2);
        tr.offsetMax = new Vector2(-4, 0);

        // ── Count badge ───────────────────────────────────────────────────────
        var badgeGo  = new GameObject("Badge", typeof(RectTransform));
        badgeGo.transform.SetParent(card.transform, false);
        var badgeImg = badgeGo.AddComponent<Image>();
        badgeImg.color        = new Color(0.15f, 0.65f, 0.35f, 1f);
        badgeImg.raycastTarget = false;

        var br = badgeImg.rectTransform;
        br.anchorMin = new Vector2(0.62f, 0.78f);
        br.anchorMax = new Vector2(1f, 1f);
        br.offsetMin = br.offsetMax = Vector2.zero;

        var cntGo  = new GameObject("Cnt", typeof(RectTransform));
        cntGo.transform.SetParent(badgeGo.transform, false);
        var cntTmp = cntGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(cntTmp);
        cntTmp.text           = count.ToString();
        cntTmp.fontSize       = 14f;
        cntTmp.fontStyle      = FontStyles.Bold;
        cntTmp.alignment      = TextAlignmentOptions.Center;
        cntTmp.color          = Color.white;
        cntTmp.raycastTarget  = false;

        var cr = cntTmp.rectTransform;
        cr.anchorMin = Vector2.zero;
        cr.anchorMax = Vector2.one;
        cr.offsetMin = cr.offsetMax = Vector2.zero;
    }

    private void OnSelect(IngredientData ingredient)
    {
        Debug.Log($"[IngredientSelectUI] Processing selection: {ingredient.ingredientName}");
        if (_pendingPlayer == null) { 
            Debug.LogError("[IngredientSelectUI] OnSelect: PendingPlayer is NULL!"); 
            Close(); 
            return; 
        }

        var economy = PlayerEconomy.Instance;
        if (economy == null) {
            Debug.LogError("[IngredientSelectUI] OnSelect: Economy is NULL!");
            return;
        }

        int count = economy.GetIngredientCount(ingredient);
        Debug.Log($"[IngredientSelectUI] Current count in economy: {count}");

        if (count <= 0)
        {
            Debug.LogWarning($"[IngredientSelectUI] Selection failed: {ingredient.ingredientName} no longer in inventory.");
            RefreshGrid();
            return;
        }

        bool removed = economy.RemoveIngredientOfType(ingredient);
        Debug.Log($"[IngredientSelectUI] Removed from economy: {removed}");

        if (removed)
        {
            var copy = Instantiate(ingredient);
            _pendingPlayer.GiveIngredient(copy);
            Debug.Log($"[IngredientSelectUI] Ingredient {ingredient.ingredientName} given to player.");
            AudioManager.Instance?.PlayAt("cabinet_open", _pendingPlayer.transform.position);
            Close();
        }
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        var cvGo = new GameObject("IngSelectCanvas");


        _canvas = cvGo.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 200;

        var cvs = cvGo.AddComponent<CanvasScaler>();
        cvs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cvs.referenceResolution = new Vector2(1920, 1080);
        cvs.matchWidthOrHeight  = 0.5f;

        cvGo.AddComponent<GraphicRaycaster>();

        // ── Root container ──
        _root = new GameObject("SelectUI_Root", typeof(RectTransform));
        _root.transform.SetParent(cvGo.transform, false);
        Stretch(_root.GetComponent<RectTransform>());

        // ── Overlay (затемнение фона) ──
        var overlayGo  = new GameObject("SelectUI_Overlay", typeof(RectTransform));
        overlayGo.transform.SetParent(_root.transform, false);
        var overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.55f);
        Stretch(overlayImg.rectTransform);
        var overlayBtn = overlayGo.AddComponent<Button>();
        overlayBtn.targetGraphic = overlayImg;
        overlayBtn.transition    = Selectable.Transition.None;
        overlayBtn.onClick.AddListener(Close);

        // ── Panel ──
        _panel = new GameObject("Panel", typeof(RectTransform));
        _panel.transform.SetParent(_root.transform, false);

        var panelImg = _panel.AddComponent<Image>();
        panelImg.color        = new Color(0.10f, 0.10f, 0.14f, 0.97f);
        panelImg.raycastTarget = false;

        var pr = panelImg.rectTransform;
        pr.anchorMin = new Vector2(0.2f, 0.12f);
        pr.anchorMax = new Vector2(0.8f, 0.88f);
        pr.offsetMin = pr.offsetMax = Vector2.zero;

        // Title
        var titleGo  = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(_panel.transform, false);
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(titleTmp);
        titleTmp.text          = "SELECT INGREDIENT";
        titleTmp.fontSize      = 24f;
        titleTmp.fontStyle     = FontStyles.Bold;
        titleTmp.alignment     = TextAlignmentOptions.Center;
        titleTmp.color         = Color.white;
        titleTmp.raycastTarget = false;
        var tr = titleTmp.rectTransform;
        tr.anchorMin = new Vector2(0f, 0.88f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.offsetMin = tr.offsetMax = Vector2.zero;

        // Hint
        var hintGo  = new GameObject("Hint", typeof(RectTransform));
        hintGo.transform.SetParent(_panel.transform, false);
        var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(hintTmp);
        hintTmp.text          = "[Esc] or click outside";
        hintTmp.fontSize      = 13f;
        hintTmp.alignment     = TextAlignmentOptions.Center;
        hintTmp.color         = new Color(0.55f, 0.55f, 0.6f);
        hintTmp.raycastTarget = false;
        var hr = hintTmp.rectTransform;
        hr.anchorMin = new Vector2(0f, 0f);
        hr.anchorMax = new Vector2(1f, 0.09f);
        hr.offsetMin = hr.offsetMax = Vector2.zero;

        // Scroll view
        var scrollGo   = new GameObject("Scroll", typeof(RectTransform));
        scrollGo.transform.SetParent(_panel.transform, false);
        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        var scrollR    = scrollGo.GetComponent<RectTransform>();
        scrollR.anchorMin = new Vector2(0.01f, 0.1f);
        scrollR.anchorMax = new Vector2(0.99f, 0.87f);
        scrollR.offsetMin = scrollR.offsetMax = Vector2.zero;

        var scrollBg = scrollGo.AddComponent<Image>();
        scrollBg.color        = new Color(0.06f, 0.06f, 0.09f, 0.4f);
        scrollBg.raycastTarget = false;

        // Content (grid)
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

        scrollRect.content     = contentRect;
        scrollRect.horizontal  = false;
        scrollRect.vertical    = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        _grid = contentGo.transform;
    }

    private static void EnsureEventSystem()
    {
        var es = FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            var esGo = new GameObject("EventSystem");

            es = esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[IngredientSelectUI] EventSystem создан (InputSystem)");
        }
        else
        {
            if (es.GetComponent<InputSystemUIInputModule>() == null)
            {
                Debug.LogWarning("[IngredientSelectUI] Найден старый EventSystem. Добавляю InputSystemUIInputModule.");
                var old = es.GetComponent<StandaloneInputModule>();
                if (old != null) Destroy(old);
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
