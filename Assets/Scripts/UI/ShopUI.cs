using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    private Canvas _canvas;
    private GameObject _root;
    private TMP_Text _moneyLabel;
    private TMP_Text _junkLabel;
    private TMP_Text _sellJunkBtnText;
    private Button _sellJunkBtn;
    
    private Transform _marketGrid;
    private Transform _inventoryGrid;
    private TMP_Text _emptyInventoryLabel;

    private bool _isShopping;
    private List<IngredientData> _purchasableIngredients = new List<IngredientData>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        BuildUI();
        _root.SetActive(false);
    }

    private void ScanForIngredients()
    {
        _purchasableIngredients.Clear();
        List<IngredientData> allIngredients = new List<IngredientData>();

        // 1. Resources search
        var loaded = Resources.FindObjectsOfTypeAll<IngredientData>();
        foreach (var ing in loaded)
        {
            if (ing != null && !allIngredients.Exists(x => x.ingredientName == ing.ingredientName))
            {
                allIngredients.Add(ing);
            }
        }

        // 2. Dumpsters search
        var dumpsters = FindObjectsByType<Dumpster>(FindObjectsSortMode.None);
        foreach (var d in dumpsters)
        {
            if (d != null)
            {
                if (d.spoiledIngredientPool != null)
                {
                    foreach (var ing in d.spoiledIngredientPool)
                        if (ing != null && !allIngredients.Exists(x => x.ingredientName == ing.ingredientName)) allIngredients.Add(ing);
                }
                if (d.freshIngredientPool != null)
                {
                    foreach (var ing in d.freshIngredientPool)
                        if (ing != null && !allIngredients.Exists(x => x.ingredientName == ing.ingredientName)) allIngredients.Add(ing);
                }
            }
        }

        // 3. Homeless search
        var homeless = FindObjectsByType<HomelessNPC>(FindObjectsSortMode.None);
        foreach (var h in homeless)
        {
            if (h != null)
            {
                if (h.ingredientLoot != null)
                {
                    foreach (var ing in h.ingredientLoot)
                        if (ing != null && !allIngredients.Exists(x => x.ingredientName == ing.ingredientName)) allIngredients.Add(ing);
                }
                if (h.bonusKillLoot != null)
                {
                    foreach (var ing in h.bonusKillLoot)
                        if (ing != null && !allIngredients.Exists(x => x.ingredientName == ing.ingredientName)) allIngredients.Add(ing);
                }
            }
        }

        // Only add safe (fresh) ingredients to purchasable list
        foreach (var ing in allIngredients)
        {
            if (ing.IsSafe)
            {
                _purchasableIngredients.Add(ing);
            }
        }

        // Sort alphabetically to keep layout consistent
        _purchasableIngredients.Sort((a, b) => a.ingredientName.CompareTo(b.ingredientName));
    }

    public IEnumerator ShowShopCoroutine()
    {
        _isShopping = true;
        Time.timeScale = 0f; // Pause game during shopping phase
        
        EnsureEventSystem();
        ScanForIngredients();
        
        _root.SetActive(true);
        RefreshUI();

        yield return new WaitForSecondsRealtime(0.2f);

        while (_isShopping)
        {
            yield return null;
        }

        _root.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OnContinuePressed()
    {
        _isShopping = false;
        AudioManager.Instance?.Play("buy_ingredients");
    }

    public void OnSellJunkPressed()
    {
        if (PlayerEconomy.Instance != null && PlayerEconomy.Instance.JunkLoot > 0)
        {
            int amount = PlayerEconomy.Instance.JunkLoot;
            PlayerEconomy.Instance.SellJunkLoot();
            AudioManager.Instance?.Play("buy_ingredients");
            RefreshUI();
            Debug.Log($"[ShopUI] Sold {amount} junk items.");
        }
    }

    public void BuyIngredient(IngredientData ingredient)
    {
        if (PlayerEconomy.Instance == null) return;

        int cost = PlayerEconomy.Instance.freshIngredientCostPerUnit;
        if (PlayerEconomy.Instance.Money >= cost)
        {
            bool success = PlayerEconomy.Instance.BuySingleIngredient(ingredient, cost);
            if (success)
            {
                AudioManager.Instance?.Play("buy_ingredients");
                RefreshUI();
                Debug.Log($"[ShopUI] Purchased 1x {ingredient.ingredientName} for ${cost}");
            }
            else
            {
                AudioManager.Instance?.Play("error");
            }
        }
        else
        {
            AudioManager.Instance?.Play("error");
        }
    }

    private void RefreshUI()
    {
        if (PlayerEconomy.Instance == null) return;

        // Balance & Junk labels
        _moneyLabel.text = $"Cash: <color=#FFD700>${PlayerEconomy.Instance.Money}</color>";
        _junkLabel.text = $"Garbage: {PlayerEconomy.Instance.JunkLoot}";
        
        // Sell Junk button state
        int junkCount = PlayerEconomy.Instance.JunkLoot;
        _sellJunkBtn.interactable = junkCount > 0;
        _sellJunkBtnText.text = junkCount > 0 ? $"SELL ALL JUNK (+${junkCount * 3})" : "NO JUNK TO SELL";

        // Rebuild Market Grid
        foreach (Transform child in _marketGrid) Destroy(child.gameObject);
        int cost = PlayerEconomy.Instance.freshIngredientCostPerUnit;

        foreach (var ing in _purchasableIngredients)
        {
            CreateMarketItem(ing, cost);
        }

        // Rebuild Inventory Grid
        foreach (Transform child in _inventoryGrid) Destroy(child.gameObject);
        
        bool anyInventory = false;
        foreach (var stack in PlayerEconomy.Instance.inventory)
        {
            if (stack == null || stack.ingredient == null || stack.amount <= 0) continue;
            CreateInventoryItem(stack.ingredient, stack.amount);
            anyInventory = true;
        }

        _emptyInventoryLabel.gameObject.SetActive(!anyInventory);
    }

    private void CreateMarketItem(IngredientData ingredient, int cost)
    {
        GameObject card = new GameObject("MarketCard_" + ingredient.ingredientName, typeof(RectTransform));
        card.transform.SetParent(_marketGrid, false);

        Image cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.18f, 0.18f, 0.22f, 1f);

        LayoutElement le = card.AddComponent<LayoutElement>();
        le.preferredWidth = 320f;
        le.preferredHeight = 70f;

        // Button to purchase
        Button btn = card.AddComponent<Button>();
        btn.targetGraphic = cardImg;
        btn.onClick.AddListener(() => BuyIngredient(ingredient));

        // Highlight colors
        ColorBlock cols = btn.colors;
        cols.normalColor = Color.white;
        cols.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        cols.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        btn.colors = cols;

        // Icon
        GameObject iconGo = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(card.transform, false);
        Image iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        if (ingredient.sprite != null) iconImg.sprite = ingredient.sprite;
        else iconImg.color = new Color(0.35f, 0.35f, 0.4f);

        RectTransform ir = iconImg.rectTransform;
        ir.anchorMin = new Vector2(0.04f, 0.1f);
        ir.anchorMax = new Vector2(0.2f, 0.9f);
        ir.offsetMin = ir.offsetMax = Vector2.zero;

        // Name
        GameObject nameGo = new GameObject("Name", typeof(RectTransform));
        nameGo.transform.SetParent(card.transform, false);
        TMP_Text nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(nameTmp);
        nameTmp.text = ingredient.ingredientName;
        nameTmp.fontSize = 16f;
        nameTmp.alignment = TextAlignmentOptions.Left;
        nameTmp.color = Color.white;
        nameTmp.raycastTarget = false;

        RectTransform nr = nameTmp.rectTransform;
        nr.anchorMin = new Vector2(0.24f, 0.5f);
        nr.anchorMax = new Vector2(0.7f, 0.9f);
        nr.offsetMin = nr.offsetMax = Vector2.zero;

        // Cost
        GameObject costGo = new GameObject("Cost", typeof(RectTransform));
        costGo.transform.SetParent(card.transform, false);
        TMP_Text costTmp = costGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(costTmp);
        costTmp.text = $"Cost: ${cost}";
        costTmp.fontSize = 13f;
        costTmp.alignment = TextAlignmentOptions.Left;
        costTmp.color = new Color(0.85f, 0.85f, 0.6f);
        costTmp.raycastTarget = false;

        RectTransform cr = costTmp.rectTransform;
        cr.anchorMin = new Vector2(0.24f, 0.1f);
        cr.anchorMax = new Vector2(0.7f, 0.5f);
        cr.offsetMin = cr.offsetMax = Vector2.zero;

        // Buy Badge
        GameObject buyGo = new GameObject("BuyBadge", typeof(RectTransform));
        buyGo.transform.SetParent(card.transform, false);
        Image buyImg = buyGo.AddComponent<Image>();
        buyImg.color = new Color(0.12f, 0.58f, 0.3f, 1f);
        buyImg.raycastTarget = false;

        RectTransform br = buyImg.rectTransform;
        br.anchorMin = new Vector2(0.75f, 0.2f);
        br.anchorMax = new Vector2(0.96f, 0.8f);
        br.offsetMin = br.offsetMax = Vector2.zero;

        GameObject buyTextGo = new GameObject("Txt", typeof(RectTransform));
        buyTextGo.transform.SetParent(buyGo.transform, false);
        TMP_Text buyTmp = buyTextGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(buyTmp);
        buyTmp.text = "BUY";
        buyTmp.fontSize = 14f;
        buyTmp.fontStyle = FontStyles.Bold;
        buyTmp.alignment = TextAlignmentOptions.Center;
        buyTmp.color = Color.white;
        buyTmp.raycastTarget = false;

        RectTransform btr = buyTmp.rectTransform;
        btr.anchorMin = Vector2.zero;
        btr.anchorMax = Vector2.one;
        btr.offsetMin = btr.offsetMax = Vector2.zero;
    }

    private void CreateInventoryItem(IngredientData ingredient, int count)
    {
        GameObject card = new GameObject("InvCard_" + ingredient.ingredientName, typeof(RectTransform));
        card.transform.SetParent(_inventoryGrid, false);

        Image cardImg = card.AddComponent<Image>();
        cardImg.color = ingredient.IsSafe ? new Color(0.15f, 0.25f, 0.18f, 1f) : new Color(0.28f, 0.15f, 0.15f, 1f);

        LayoutElement le = card.AddComponent<LayoutElement>();
        le.preferredWidth = 320f;
        le.preferredHeight = 55f;

        // Icon
        GameObject iconGo = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(card.transform, false);
        Image iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        if (ingredient.sprite != null) iconImg.sprite = ingredient.sprite;
        else iconImg.color = new Color(0.35f, 0.35f, 0.4f);

        RectTransform ir = iconImg.rectTransform;
        ir.anchorMin = new Vector2(0.04f, 0.15f);
        ir.anchorMax = new Vector2(0.18f, 0.85f);
        ir.offsetMin = ir.offsetMax = Vector2.zero;

        // Name
        GameObject nameGo = new GameObject("Name", typeof(RectTransform));
        nameGo.transform.SetParent(card.transform, false);
        TMP_Text nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(nameTmp);
        nameTmp.text = ingredient.ingredientName;
        nameTmp.fontSize = 15f;
        nameTmp.alignment = TextAlignmentOptions.Left;
        nameTmp.color = Color.white;
        nameTmp.raycastTarget = false;

        RectTransform nr = nameTmp.rectTransform;
        nr.anchorMin = new Vector2(0.22f, 0.5f);
        nr.anchorMax = new Vector2(0.75f, 0.9f);
        nr.offsetMin = nr.offsetMax = Vector2.zero;

        // Type label
        GameObject typeGo = new GameObject("Type", typeof(RectTransform));
        typeGo.transform.SetParent(card.transform, false);
        TMP_Text typeTmp = typeGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(typeTmp);
        typeTmp.text = ingredient.IsSafe ? "<color=green>Fresh</color>" : "<color=red>Spoiled</color>";
        typeTmp.fontSize = 12f;
        typeTmp.alignment = TextAlignmentOptions.Left;
        typeTmp.raycastTarget = false;

        RectTransform tr = typeTmp.rectTransform;
        tr.anchorMin = new Vector2(0.22f, 0.1f);
        tr.anchorMax = new Vector2(0.75f, 0.5f);
        tr.offsetMin = tr.offsetMax = Vector2.zero;

        // Amount badge
        GameObject badgeGo = new GameObject("Badge", typeof(RectTransform));
        badgeGo.transform.SetParent(card.transform, false);
        Image badgeImg = badgeGo.AddComponent<Image>();
        badgeImg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        badgeImg.raycastTarget = false;

        RectTransform br = badgeImg.rectTransform;
        br.anchorMin = new Vector2(0.8f, 0.2f);
        br.anchorMax = new Vector2(0.96f, 0.8f);
        br.offsetMin = br.offsetMax = Vector2.zero;

        GameObject cntGo = new GameObject("Cnt", typeof(RectTransform));
        cntGo.transform.SetParent(badgeGo.transform, false);
        TMP_Text cntTmp = cntGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(cntTmp);
        cntTmp.text = $"x{count}";
        cntTmp.fontSize = 13f;
        cntTmp.fontStyle = FontStyles.Bold;
        cntTmp.alignment = TextAlignmentOptions.Center;
        cntTmp.color = Color.white;
        cntTmp.raycastTarget = false;

        RectTransform cr = cntTmp.rectTransform;
        cr.anchorMin = Vector2.zero;
        cr.anchorMax = Vector2.one;
        cr.offsetMin = cr.offsetMax = Vector2.zero;
    }

    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("[ShopCanvas]");
        DontDestroyOnLoad(canvasGo);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 995;

        CanvasScaler cvs = canvasGo.AddComponent<CanvasScaler>();
        cvs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cvs.referenceResolution = new Vector2(1920, 1080);
        cvs.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // Root UI Screen
        _root = new GameObject("ShopUI_Root", typeof(RectTransform));
        _root.transform.SetParent(canvasGo.transform, false);
        Stretch(_root.GetComponent<RectTransform>());

        // Overlay Screen
        GameObject overlayGo = new GameObject("Overlay", typeof(RectTransform));
        overlayGo.transform.SetParent(_root.transform, false);
        Image overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.85f);
        Stretch(overlayImg.rectTransform);

        // Core Window
        GameObject winGo = new GameObject("Window", typeof(RectTransform));
        winGo.transform.SetParent(_root.transform, false);
        Image winImg = winGo.AddComponent<Image>();
        winImg.color = new Color(0.08f, 0.08f, 0.11f, 0.98f);
        
        RectTransform winRT = winImg.rectTransform;
        winRT.anchorMin = new Vector2(0.1f, 0.15f);
        winRT.anchorMax = new Vector2(0.9f, 0.88f);
        winRT.offsetMin = winRT.offsetMax = Vector2.zero;

        // Title text
        GameObject titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(winGo.transform, false);
        TMP_Text titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(titleTmp);
        titleTmp.text = "RESTAURANT SUPPLY SHOP";
        titleTmp.fontSize = 32f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.9f, 0.8f, 0.4f, 1f);
        titleTmp.raycastTarget = false;

        RectTransform titRT = titleTmp.rectTransform;
        titRT.anchorMin = new Vector2(0f, 0.88f);
        titRT.anchorMax = new Vector2(1f, 0.98f);
        titRT.offsetMin = titRT.offsetMax = Vector2.zero;

        // Subtitle
        GameObject subGo = new GameObject("Subtitle", typeof(RectTransform));
        subGo.transform.SetParent(winGo.transform, false);
        TMP_Text subTmp = subGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(subTmp);
        subTmp.text = "Prepare ingredients for the cooking phase";
        subTmp.fontSize = 15f;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = new Color(0.6f, 0.6f, 0.65f, 1f);
        subTmp.raycastTarget = false;

        RectTransform subRT = subTmp.rectTransform;
        subRT.anchorMin = new Vector2(0f, 0.82f);
        subRT.anchorMax = new Vector2(1f, 0.88f);
        subRT.offsetMin = subRT.offsetMax = Vector2.zero;

        // ── COLUMN 1: GARBAGE & ECONOMY ──
        GameObject col1 = new GameObject("Col1_Economy", typeof(RectTransform));
        col1.transform.SetParent(winGo.transform, false);
        RectTransform c1RT = col1.GetComponent<RectTransform>();
        c1RT.anchorMin = new Vector2(0.04f, 0.18f);
        c1RT.anchorMax = new Vector2(0.31f, 0.78f);
        c1RT.offsetMin = c1RT.offsetMax = Vector2.zero;

        Image c1Bg = col1.AddComponent<Image>();
        c1Bg.color = new Color(0.05f, 0.05f, 0.07f, 0.7f);

        // Economy Title
        GameObject ecTit = new GameObject("Tit", typeof(RectTransform));
        ecTit.transform.SetParent(col1.transform, false);
        TMP_Text ecTitTmp = ecTit.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(ecTitTmp);
        ecTitTmp.text = "GARBAGE & CASH";
        ecTitTmp.fontSize = 20f;
        ecTitTmp.alignment = TextAlignmentOptions.Center;
        ecTitTmp.color = Color.white;
        ecTitTmp.raycastTarget = false;
        RectTransform etRT = ecTitTmp.rectTransform;
        etRT.anchorMin = new Vector2(0f, 0.82f);
        etRT.anchorMax = new Vector2(1f, 0.98f);
        etRT.offsetMin = etRT.offsetMax = Vector2.zero;

        // Balance display
        GameObject monGo = new GameObject("MoneyVal", typeof(RectTransform));
        monGo.transform.SetParent(col1.transform, false);
        _moneyLabel = monGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(_moneyLabel);
        _moneyLabel.text = "Cash: $0";
        _moneyLabel.fontSize = 24f;
        _moneyLabel.alignment = TextAlignmentOptions.Center;
        _moneyLabel.raycastTarget = false;
        RectTransform mRT = _moneyLabel.rectTransform;
        mRT.anchorMin = new Vector2(0f, 0.58f);
        mRT.anchorMax = new Vector2(1f, 0.78f);
        mRT.offsetMin = mRT.offsetMax = Vector2.zero;

        // Junk display
        GameObject junkGo = new GameObject("JunkVal", typeof(RectTransform));
        junkGo.transform.SetParent(col1.transform, false);
        _junkLabel = junkGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(_junkLabel);
        _junkLabel.text = "Garbage: 0";
        _junkLabel.fontSize = 20f;
        _junkLabel.alignment = TextAlignmentOptions.Center;
        _junkLabel.color = new Color(0.7f, 0.7f, 0.75f);
        _junkLabel.raycastTarget = false;
        RectTransform jRT = _junkLabel.rectTransform;
        jRT.anchorMin = new Vector2(0f, 0.44f);
        jRT.anchorMax = new Vector2(1f, 0.58f);
        jRT.offsetMin = jRT.offsetMax = Vector2.zero;

        // Sell Junk Button
        GameObject sellGo = new GameObject("SellBtn", typeof(RectTransform));
        sellGo.transform.SetParent(col1.transform, false);
        Image sellImg = sellGo.AddComponent<Image>();
        sellImg.color = new Color(0.6f, 0.3f, 0.1f, 1f);
        _sellJunkBtn = sellGo.AddComponent<Button>();
        _sellJunkBtn.targetGraphic = sellImg;
        _sellJunkBtn.onClick.AddListener(OnSellJunkPressed);

        RectTransform sRT = sellGo.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0.1f, 0.12f);
        sRT.anchorMax = new Vector2(0.9f, 0.32f);
        sRT.offsetMin = sRT.offsetMax = Vector2.zero;

        GameObject sTxtGo = new GameObject("Txt", typeof(RectTransform));
        sTxtGo.transform.SetParent(sellGo.transform, false);
        _sellJunkBtnText = sTxtGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(_sellJunkBtnText);
        _sellJunkBtnText.text = "SELL ALL JUNK";
        _sellJunkBtnText.fontSize = 16f;
        _sellJunkBtnText.fontStyle = FontStyles.Bold;
        _sellJunkBtnText.alignment = TextAlignmentOptions.Center;
        _sellJunkBtnText.color = Color.white;
        _sellJunkBtnText.raycastTarget = false;
        Stretch(_sellJunkBtnText.rectTransform);

        // ── COLUMN 2: MARKET FRESH GOODS ──
        GameObject col2 = new GameObject("Col2_Market", typeof(RectTransform));
        col2.transform.SetParent(winGo.transform, false);
        RectTransform c2RT = col2.GetComponent<RectTransform>();
        c2RT.anchorMin = new Vector2(0.34f, 0.18f);
        c2RT.anchorMax = new Vector2(0.65f, 0.78f);
        c2RT.offsetMin = c2RT.offsetMax = Vector2.zero;

        Image c2Bg = col2.AddComponent<Image>();
        c2Bg.color = new Color(0.05f, 0.05f, 0.07f, 0.7f);

        // Market Title
        GameObject mkTit = new GameObject("Tit", typeof(RectTransform));
        mkTit.transform.SetParent(col2.transform, false);
        TMP_Text mkTitTmp = mkTit.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(mkTitTmp);
        mkTitTmp.text = "MARKET FRESH GOODS";
        mkTitTmp.fontSize = 20f;
        mkTitTmp.alignment = TextAlignmentOptions.Center;
        mkTitTmp.color = Color.white;
        mkTitTmp.raycastTarget = false;
        RectTransform mtRT = mkTitTmp.rectTransform;
        mtRT.anchorMin = new Vector2(0f, 0.9f);
        mtRT.anchorMax = new Vector2(1f, 0.98f);
        mtRT.offsetMin = mtRT.offsetMax = Vector2.zero;

        // Market Scroll View
        GameObject mkScroll = new GameObject("Scroll", typeof(RectTransform));
        mkScroll.transform.SetParent(col2.transform, false);
        RectTransform mskRT = mkScroll.GetComponent<RectTransform>();
        mskRT.anchorMin = new Vector2(0.02f, 0.02f);
        mskRT.anchorMax = new Vector2(0.98f, 0.88f);
        mskRT.offsetMin = mskRT.offsetMax = Vector2.zero;

        ScrollRect mkSR = mkScroll.AddComponent<ScrollRect>();
        mkSR.horizontal = false;
        mkSR.vertical = true;
        mkSR.movementType = ScrollRect.MovementType.Clamped;
        mkScroll.AddComponent<RectMask2D>();

        GameObject mkContent = new GameObject("Content", typeof(RectTransform));
        mkContent.transform.SetParent(mkScroll.transform, false);
        RectTransform mkcRT = mkContent.GetComponent<RectTransform>();
        mkcRT.anchorMin = new Vector2(0f, 1f);
        mkcRT.anchorMax = new Vector2(1f, 1f);
        mkcRT.pivot = new Vector2(0.5f, 1f);
        mkcRT.offsetMin = mkcRT.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg1 = mkContent.AddComponent<VerticalLayoutGroup>();
        vlg1.spacing = 8f;
        vlg1.padding = new RectOffset(10, 10, 10, 10);
        vlg1.childControlHeight = false;
        vlg1.childControlWidth = true;
        vlg1.childForceExpandHeight = false;
        vlg1.childForceExpandWidth = true;

        ContentSizeFitter csf1 = mkContent.AddComponent<ContentSizeFitter>();
        csf1.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        mkSR.content = mkcRT;
        _marketGrid = mkContent.transform;

        // ── COLUMN 3: PLAYER STORAGE ──
        GameObject col3 = new GameObject("Col3_Storage", typeof(RectTransform));
        col3.transform.SetParent(winGo.transform, false);
        RectTransform c3RT = col3.GetComponent<RectTransform>();
        c3RT.anchorMin = new Vector2(0.68f, 0.18f);
        c3RT.anchorMax = new Vector2(0.96f, 0.78f);
        c3RT.offsetMin = c3RT.offsetMax = Vector2.zero;

        Image c3Bg = col3.AddComponent<Image>();
        c3Bg.color = new Color(0.05f, 0.05f, 0.07f, 0.7f);

        // Storage Title
        GameObject stTit = new GameObject("Tit", typeof(RectTransform));
        stTit.transform.SetParent(col3.transform, false);
        TMP_Text stTitTmp = stTit.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(stTitTmp);
        stTitTmp.text = "YOUR STORAGE";
        stTitTmp.fontSize = 20f;
        stTitTmp.alignment = TextAlignmentOptions.Center;
        stTitTmp.color = Color.white;
        stTitTmp.raycastTarget = false;
        RectTransform stRT = stTitTmp.rectTransform;
        stRT.anchorMin = new Vector2(0f, 0.9f);
        stRT.anchorMax = new Vector2(1f, 0.98f);
        stRT.offsetMin = stRT.offsetMax = Vector2.zero;

        // Storage Scroll View
        GameObject stScroll = new GameObject("Scroll", typeof(RectTransform));
        stScroll.transform.SetParent(col3.transform, false);
        RectTransform sskRT = stScroll.GetComponent<RectTransform>();
        sskRT.anchorMin = new Vector2(0.02f, 0.02f);
        sskRT.anchorMax = new Vector2(0.98f, 0.88f);
        sskRT.offsetMin = sskRT.offsetMax = Vector2.zero;

        ScrollRect stSR = stScroll.AddComponent<ScrollRect>();
        stSR.horizontal = false;
        stSR.vertical = true;
        stSR.movementType = ScrollRect.MovementType.Clamped;
        stScroll.AddComponent<RectMask2D>();

        GameObject stContent = new GameObject("Content", typeof(RectTransform));
        stContent.transform.SetParent(stScroll.transform, false);
        RectTransform stcRT = stContent.GetComponent<RectTransform>();
        stcRT.anchorMin = new Vector2(0f, 1f);
        stcRT.anchorMax = new Vector2(1f, 1f);
        stcRT.pivot = new Vector2(0.5f, 1f);
        stcRT.offsetMin = stcRT.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg2 = stContent.AddComponent<VerticalLayoutGroup>();
        vlg2.spacing = 6f;
        vlg2.padding = new RectOffset(10, 10, 10, 10);
        vlg2.childControlHeight = false;
        vlg2.childControlWidth = true;
        vlg2.childForceExpandHeight = false;
        vlg2.childForceExpandWidth = true;

        ContentSizeFitter csf2 = stContent.AddComponent<ContentSizeFitter>();
        csf2.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        stSR.content = stcRT;
        _inventoryGrid = stContent.transform;

        // Empty Inventory Label
        GameObject empGo = new GameObject("EmptyLabel", typeof(RectTransform));
        empGo.transform.SetParent(col3.transform, false);
        _emptyInventoryLabel = empGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(_emptyInventoryLabel);
        _emptyInventoryLabel.text = "Storage is empty\n\nSearch garbage at night\nor buy fresh foods";
        _emptyInventoryLabel.fontSize = 14f;
        _emptyInventoryLabel.alignment = TextAlignmentOptions.Center;
        _emptyInventoryLabel.color = new Color(0.5f, 0.5f, 0.55f);
        _emptyInventoryLabel.raycastTarget = false;
        RectTransform empRT = _emptyInventoryLabel.rectTransform;
        empRT.anchorMin = new Vector2(0.05f, 0.2f);
        empRT.anchorMax = new Vector2(0.95f, 0.7f);
        empRT.offsetMin = empRT.offsetMax = Vector2.zero;

        // ── BOTTOM CONTINUE BUTTON ──
        GameObject contGo = new GameObject("ContinueBtn", typeof(RectTransform));
        contGo.transform.SetParent(winGo.transform, false);
        Image contImg = contGo.AddComponent<Image>();
        contImg.color = new Color(0.12f, 0.55f, 0.25f, 1f);
        Button contBtn = contGo.AddComponent<Button>();
        contBtn.targetGraphic = contImg;
        contBtn.onClick.AddListener(OnContinuePressed);

        RectTransform cntBtnRT = contGo.GetComponent<RectTransform>();
        cntBtnRT.anchorMin = new Vector2(0.35f, 0.03f);
        cntBtnRT.anchorMax = new Vector2(0.65f, 0.12f);
        cntBtnRT.offsetMin = cntBtnRT.offsetMax = Vector2.zero;

        GameObject cntTextGo = new GameObject("Txt", typeof(RectTransform));
        cntTextGo.transform.SetParent(contGo.transform, false);
        TMP_Text cntTxt = cntTextGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(cntTxt);
        cntTxt.text = "START NEXT DAY";
        cntTxt.fontSize = 18f;
        cntTxt.fontStyle = FontStyles.Bold;
        cntTxt.alignment = TextAlignmentOptions.Center;
        cntTxt.color = Color.white;
        cntTxt.raycastTarget = false;
        Stretch(cntTxt.rectTransform);
    }

    private static void EnsureEventSystem()
    {
        var es = FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            var esGo = new GameObject("EventSystem");
            es = esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[ShopUI] EventSystem created dynamically.");
        }
        else
        {
            if (es.GetComponent<InputSystemUIInputModule>() == null)
            {
                var old = es.GetComponent<StandaloneInputModule>();
                if (old != null) Destroy(old);
                es.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.Log("[ShopUI] EventSystem upgraded to InputSystem.");
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
