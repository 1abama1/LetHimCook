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
    private CanvasGroup _rootGroup;
    private RectTransform _windowRT;

    private TMP_Text _moneyLabel;
    private TMP_Text _junkLabel;
    private TMP_Text _sellJunkBtnText;
    private Button _sellJunkBtn;

    private Transform _marketGrid;
    private Transform _inventoryGrid;
    private TMP_Text _emptyMarketLabel;
    private TMP_Text _emptyInventoryLabel;

    private bool _isShopping;
    public bool IsShopping => _isShopping;
    private readonly List<IngredientData> _purchasableIngredients = new List<IngredientData>();

    // ─────────────────────────────────────────────────────────────────────────────
    // Palette
    // ─────────────────────────────────────────────────────────────────────────────
    private static readonly Color BgOverlay = Hex("071014E8");
    private static readonly Color WindowBg = Hex("10171DFB");
    private static readonly Color PanelBg = Hex("151E25F4");
    private static readonly Color PanelBgSoft = Hex("19242CF0");
    private static readonly Color CardBg = Hex("1D2932FF");
    private static readonly Color CardHover = Hex("263742FF");
    private static readonly Color CardPressed = Hex("111A20FF");
    private static readonly Color Border = Hex("33444FFF");
    private static readonly Color BorderSoft = Hex("263640FF");
    private static readonly Color TextMain = Hex("F4F7F8FF");
    private static readonly Color TextMuted = Hex("8EA0AAFF");
    private static readonly Color Gold = Hex("F3C65DFF");
    private static readonly Color Green = Hex("3ECF8EFF");
    private static readonly Color GreenDark = Hex("1E8F62FF");
    private static readonly Color Orange = Hex("E98A3FFF");
    private static readonly Color Red = Hex("EF6A6AFF");
    private static readonly Color Blue = Hex("64B5F6FF");

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

        void TryAdd(IngredientData ing)
        {
            if (ing == null || !ing.IsSafe) return;
            if (!_purchasableIngredients.Exists(x => x.ingredientName == ing.ingredientName))
            {
                _purchasableIngredients.Add(ing);
            }
        }

        // 1. Resources search (All ingredients in Assets/Resources/Ingredients)
        var loaded = Resources.LoadAll<IngredientData>("Ingredients");
        foreach (var ing in loaded)
            TryAdd(ing);

        // Fallback: also search root Resources or subfolders
        var loadedAll = Resources.LoadAll<IngredientData>("");
        foreach (var ing in loadedAll)
            TryAdd(ing);

        // 2. Dumpsters search
        var dumpsters = FindObjectsByType<Dumpster>(FindObjectsSortMode.None);
        foreach (var d in dumpsters)
        {
            if (d == null) continue;
            if (d.freshIngredientPool != null)
            {
                foreach (var ing in d.freshIngredientPool)
                    TryAdd(ing);
            }
        }

        // 3. Homeless search
        var homeless = FindObjectsByType<HomelessNPC>(FindObjectsSortMode.None);
        foreach (var h in homeless)
        {
            if (h == null) continue;
            if (h.ingredientLoot != null)
            {
                foreach (var ing in h.ingredientLoot)
                    TryAdd(ing);
            }
            if (h.bonusKillLoot != null)
            {
                foreach (var ing in h.bonusKillLoot)
                    TryAdd(ing);
            }
        }

        // 4. PlayerEconomy search (in case any fresh ingredients are already in inventory or defaults)
        if (PlayerEconomy.Instance != null)
        {
            TryAdd(PlayerEconomy.Instance.defaultFreshIngredient);
            if (PlayerEconomy.Instance.inventory != null)
            {
                foreach (var stack in PlayerEconomy.Instance.inventory)
                {
                    if (stack != null && stack.ingredient != null)
                        TryAdd(stack.ingredient);
                }
            }
        }

        // Sort alphabetically to keep layout consistent
        _purchasableIngredients.Sort((a, b) => a.ingredientName.CompareTo(b.ingredientName));
        Debug.Log($"[ShopUI] ScanForIngredients found {_purchasableIngredients.Count} fresh items for sale.");
    }

    public IEnumerator ShowShopCoroutine()
    {
        _isShopping = true;
        Time.timeScale = 0f;

        EnsureEventSystem();
        ScanForIngredients();

        _root.SetActive(true);
        RefreshUI();
        yield return AnimateShop(true);

        yield return new WaitForSecondsRealtime(0.08f);

        while (_isShopping)
            yield return null;

        yield return AnimateShop(false);
        _root.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OnContinuePressed()
    {
        if (!_isShopping) return;

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
        if (PlayerEconomy.Instance == null || ingredient == null) return;

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

        int money = PlayerEconomy.Instance.Money;
        int junkCount = PlayerEconomy.Instance.JunkLoot;
        int junkValue = junkCount * 3;

        _moneyLabel.text = $"<color=#{ToHex(Gold)}>${money}</color>";
        _junkLabel.text = $"{junkCount}";

        _sellJunkBtn.interactable = junkCount > 0;
        _sellJunkBtnText.text = junkCount > 0
            ? $"SELL ALL   <color=#{ToHex(Gold)}>+${junkValue}</color>"
            : "NOTHING TO SELL";

        ConfigureButtonColors(
            _sellJunkBtn,
            junkCount > 0 ? Orange : Hex("313940FF"),
            junkCount > 0 ? Hex("F19B54FF") : Hex("313940FF"),
            junkCount > 0 ? Hex("C96C27FF") : Hex("313940FF"),
            Hex("2A3136FF")
        );

        for (int i = _marketGrid.childCount - 1; i >= 0; i--)
        {
            Transform child = _marketGrid.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        int cost = PlayerEconomy.Instance.freshIngredientCostPerUnit;
        foreach (var ing in _purchasableIngredients)
            CreateMarketItem(ing, cost);

        if (_emptyMarketLabel != null)
            _emptyMarketLabel.gameObject.SetActive(_purchasableIngredients.Count == 0);

        for (int i = _inventoryGrid.childCount - 1; i >= 0; i--)
        {
            Transform child = _inventoryGrid.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        bool anyInventory = false;
        foreach (var stack in PlayerEconomy.Instance.inventory)
        {
            if (stack == null || stack.ingredient == null || stack.amount <= 0) continue;

            CreateInventoryItem(stack.ingredient, stack.amount);
            anyInventory = true;
        }

        if (_emptyInventoryLabel != null)
            _emptyInventoryLabel.gameObject.SetActive(!anyInventory);

        Canvas.ForceUpdateCanvases();
        if (_marketGrid is RectTransform marketRT)
            LayoutRebuilder.ForceRebuildLayoutImmediate(marketRT);
        if (_inventoryGrid is RectTransform invRT)
            LayoutRebuilder.ForceRebuildLayoutImmediate(invRT);
    }

    private void CreateMarketItem(IngredientData ingredient, int cost)
    {
        bool canAfford = PlayerEconomy.Instance != null && PlayerEconomy.Instance.Money >= cost;

        GameObject card = CreateRect("MarketCard_" + ingredient.ingredientName, _marketGrid,
            Vector2.zero, Vector2.one);

        Image cardImg = card.AddComponent<Image>();
        cardImg.color = Color.white;
        AddOutline(cardImg, BorderSoft, new Vector2(1f, -1f));

        LayoutElement le = card.AddComponent<LayoutElement>();
        le.minHeight = 82f;
        le.preferredHeight = 82f;
        le.flexibleHeight = 0f;

        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(0f, 82f);

        Button btn = card.AddComponent<Button>();
        btn.targetGraphic = cardImg;
        btn.interactable = canAfford;
        btn.onClick.AddListener(() => BuyIngredient(ingredient));
        ConfigureButtonColors(
            btn,
            CardBg,
            CardHover,
            CardPressed,
            Hex("172027FF")
        );

        // Icon tile
        GameObject iconTile = CreateRect("IconTile", card.transform,
            new Vector2(0.025f, 0.14f), new Vector2(0.185f, 0.86f));
        Image tileImg = iconTile.AddComponent<Image>();
        tileImg.color = Hex("0E151AFF");
        tileImg.raycastTarget = false;
        AddOutline(tileImg, Hex("2B3942FF"), new Vector2(1f, -1f));

        GameObject iconGo = CreateRect("Icon", iconTile.transform,
            new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
        Image iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        if (ingredient.sprite != null)
        {
            iconImg.sprite = ingredient.sprite;
            iconImg.color = Color.white;
        }
        else
        {
            iconImg.color = Hex("45535BFF");
        }

        TMP_Text nameTmp = CreateText(
            "Name",
            card.transform,
            ingredient.ingredientName,
            17f,
            TextMain,
            TextAlignmentOptions.Left,
            new Vector2(0.215f, 0.46f),
            new Vector2(0.62f, 0.84f),
            true
        );
        nameTmp.enableAutoSizing = true;
        nameTmp.fontSizeMin = 12f;
        nameTmp.fontSizeMax = 17f;

        TMP_Text freshTmp = CreateText(
            "FreshBadgeText",
            card.transform,
            "FRESH",
            10f,
            Green,
            TextAlignmentOptions.Left,
            new Vector2(0.215f, 0.15f),
            new Vector2(0.36f, 0.44f),
            true
        );
        freshTmp.characterSpacing = 2f;

        TMP_Text priceTmp = CreateText(
            "Price",
            card.transform,
            $"${cost}",
            20f,
            canAfford ? Gold : Red,
            TextAlignmentOptions.Right,
            new Vector2(0.61f, 0.16f),
            new Vector2(0.76f, 0.84f),
            true
        );

        // Buy button-looking badge. The whole card is clickable.
        GameObject buyGo = CreateRect("BuyBadge", card.transform,
            new Vector2(0.79f, 0.22f), new Vector2(0.965f, 0.78f));
        Image buyImg = buyGo.AddComponent<Image>();
        buyImg.color = canAfford ? GreenDark : Hex("39444AFF");
        buyImg.raycastTarget = false;
        AddOutline(buyImg, canAfford ? Hex("4FE6A4AA") : Hex("56616899"), new Vector2(1f, -1f));

        TMP_Text buyTmp = CreateText(
            "Txt",
            buyGo.transform,
            canAfford ? "BUY" : "LOCKED",
            canAfford ? 13f : 10f,
            canAfford ? Color.white : TextMuted,
            TextAlignmentOptions.Center,
            Vector2.zero,
            Vector2.one,
            true
        );
        buyTmp.characterSpacing = canAfford ? 1f : 0f;

        priceTmp.raycastTarget = false;
    }

    private void CreateInventoryItem(IngredientData ingredient, int count)
    {
        Color stateColor = ingredient.IsSafe ? Green : Red;

        GameObject card = CreateRect("InvCard_" + ingredient.ingredientName, _inventoryGrid,
            Vector2.zero, Vector2.one);

        Image cardImg = card.AddComponent<Image>();
        cardImg.color = ingredient.IsSafe ? Hex("172821FF") : Hex("2B1B1DFF");
        cardImg.raycastTarget = false;
        AddOutline(cardImg, ingredient.IsSafe ? Hex("2A5C45FF") : Hex("613A3AFF"), new Vector2(1f, -1f));

        LayoutElement le = card.AddComponent<LayoutElement>();
        le.minHeight = 64f;
        le.preferredHeight = 64f;
        le.flexibleHeight = 0f;

        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(0f, 64f);

        // Small state accent
        GameObject accent = CreateRect("Accent", card.transform,
            new Vector2(0f, 0f), new Vector2(0.012f, 1f));
        Image accentImg = accent.AddComponent<Image>();
        accentImg.color = stateColor;
        accentImg.raycastTarget = false;

        GameObject iconTile = CreateRect("IconTile", card.transform,
            new Vector2(0.035f, 0.16f), new Vector2(0.18f, 0.84f));
        Image tileImg = iconTile.AddComponent<Image>();
        tileImg.color = Hex("0E151AFF");
        tileImg.raycastTarget = false;

        GameObject iconGo = CreateRect("Icon", iconTile.transform,
            new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
        Image iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        if (ingredient.sprite != null)
        {
            iconImg.sprite = ingredient.sprite;
            iconImg.color = Color.white;
        }
        else
        {
            iconImg.color = Hex("45535BFF");
        }

        TMP_Text nameTmp = CreateText(
            "Name",
            card.transform,
            ingredient.ingredientName,
            15f,
            TextMain,
            TextAlignmentOptions.Left,
            new Vector2(0.215f, 0.47f),
            new Vector2(0.72f, 0.86f),
            true
        );
        nameTmp.enableAutoSizing = true;
        nameTmp.fontSizeMin = 11f;
        nameTmp.fontSizeMax = 15f;

        TMP_Text typeTmp = CreateText(
            "Type",
            card.transform,
            ingredient.IsSafe ? "FRESH" : "SPOILED",
            10f,
            stateColor,
            TextAlignmentOptions.Left,
            new Vector2(0.215f, 0.13f),
            new Vector2(0.55f, 0.47f),
            true
        );
        typeTmp.characterSpacing = 1.5f;

        GameObject badgeGo = CreateRect("Badge", card.transform,
            new Vector2(0.79f, 0.22f), new Vector2(0.955f, 0.78f));
        Image badgeImg = badgeGo.AddComponent<Image>();
        badgeImg.color = Hex("0C1317FF");
        badgeImg.raycastTarget = false;
        AddOutline(badgeImg, Border, new Vector2(1f, -1f));

        TMP_Text cntTmp = CreateText(
            "Cnt",
            badgeGo.transform,
            $"x{count}",
            14f,
            TextMain,
            TextAlignmentOptions.Center,
            Vector2.zero,
            Vector2.one,
            true
        );
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
        _rootGroup = _root.AddComponent<CanvasGroup>();

        // Dark overlay
        GameObject overlayGo = CreateRect("Overlay", _root.transform, Vector2.zero, Vector2.one);
        Image overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.color = BgOverlay;

        // Subtle top glow strip
        GameObject topGlow = CreateRect("TopGlow", _root.transform,
            new Vector2(0f, 0.965f), new Vector2(1f, 1f));
        Image topGlowImg = topGlow.AddComponent<Image>();
        topGlowImg.color = Hex("244B55AA");
        topGlowImg.raycastTarget = false;

        // Core Window
        GameObject winGo = CreateRect("Window", _root.transform,
            new Vector2(0.055f, 0.075f), new Vector2(0.945f, 0.925f));
        Image winImg = winGo.AddComponent<Image>();
        winImg.color = WindowBg;
        AddOutline(winImg, Hex("42545EFF"), new Vector2(2f, -2f));

        Shadow shadow = winGo.AddComponent<Shadow>();
        shadow.effectColor = Hex("00000099");
        shadow.effectDistance = new Vector2(0f, -12f);
        shadow.useGraphicAlpha = true;

        _windowRT = winGo.GetComponent<RectTransform>();

        // Header background
        GameObject header = CreateRect("Header", winGo.transform,
            new Vector2(0f, 0.845f), new Vector2(1f, 1f));
        Image headerImg = header.AddComponent<Image>();
        headerImg.color = Hex("111B21FF");
        headerImg.raycastTarget = false;

        GameObject headerAccent = CreateRect("HeaderAccent", header.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0.018f));
        Image headerAccentImg = headerAccent.AddComponent<Image>();
        headerAccentImg.color = Gold;
        headerAccentImg.raycastTarget = false;

        TMP_Text eyebrow = CreateText(
            "Eyebrow",
            header.transform,
            "RESTAURANT SUPPLY",
            11f,
            Gold,
            TextAlignmentOptions.Left,
            new Vector2(0.035f, 0.60f),
            new Vector2(0.44f, 0.85f),
            true
        );
        eyebrow.characterSpacing = 3f;

        TMP_Text titleTmp = CreateText(
            "Title",
            header.transform,
            "MARKET & STORAGE",
            30f,
            TextMain,
            TextAlignmentOptions.Left,
            new Vector2(0.035f, 0.21f),
            new Vector2(0.50f, 0.67f),
            true
        );

        TMP_Text subTmp = CreateText(
            "Subtitle",
            header.transform,
            "Restock fresh ingredients, sell scavenged junk, then start the next day.",
            13f,
            TextMuted,
            TextAlignmentOptions.Left,
            new Vector2(0.035f, 0.02f),
            new Vector2(0.63f, 0.30f)
        );

        // Cash chip in header
        GameObject cashChip = CreateRect("CashChip", header.transform,
            new Vector2(0.74f, 0.27f), new Vector2(0.965f, 0.76f));
        Image cashChipImg = cashChip.AddComponent<Image>();
        cashChipImg.color = Hex("0B1216FF");
        cashChipImg.raycastTarget = false;
        AddOutline(cashChipImg, Hex("66562FFF"), new Vector2(1f, -1f));

        TMP_Text cashCaption = CreateText(
            "Caption",
            cashChip.transform,
            "AVAILABLE CASH",
            9f,
            TextMuted,
            TextAlignmentOptions.Left,
            new Vector2(0.08f, 0.54f),
            new Vector2(0.95f, 0.86f),
            true
        );
        cashCaption.characterSpacing = 2f;

        _moneyLabel = CreateText(
            "MoneyValue",
            cashChip.transform,
            "$0",
            23f,
            Gold,
            TextAlignmentOptions.Left,
            new Vector2(0.08f, 0.10f),
            new Vector2(0.95f, 0.60f),
            true
        );

        // ── COLUMN 1: ECONOMY ───────────────────────────────────────────────────
        GameObject col1 = CreatePanel(
            "Col1_Economy",
            winGo.transform,
            new Vector2(0.025f, 0.17f),
            new Vector2(0.285f, 0.81f),
            PanelBg
        );

        CreateSectionHeader(col1.transform, "SALVAGE", "SELL JUNK", Orange);

        TMP_Text junkCaption = CreateText(
            "JunkCaption",
            col1.transform,
            "JUNK COLLECTED",
            10f,
            TextMuted,
            TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.58f),
            new Vector2(0.92f, 0.68f),
            true
        );
        junkCaption.characterSpacing = 2f;

        _junkLabel = CreateText(
            "JunkValue",
            col1.transform,
            "0",
            48f,
            TextMain,
            TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.40f),
            new Vector2(0.92f, 0.60f),
            true
        );

        TMP_Text junkHint = CreateText(
            "JunkHint",
            col1.transform,
            "$3 per item",
            12f,
            TextMuted,
            TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.33f),
            new Vector2(0.92f, 0.42f)
        );

        GameObject separator = CreateRect("Separator", col1.transform,
            new Vector2(0.10f, 0.29f), new Vector2(0.90f, 0.294f));
        Image separatorImg = separator.AddComponent<Image>();
        separatorImg.color = BorderSoft;
        separatorImg.raycastTarget = false;

        GameObject sellGo = CreateRect("SellBtn", col1.transform,
            new Vector2(0.085f, 0.09f), new Vector2(0.915f, 0.24f));
        Image sellImg = sellGo.AddComponent<Image>();
        sellImg.color = Color.white;
        AddOutline(sellImg, Hex("8C582FFF"), new Vector2(1f, -1f));

        _sellJunkBtn = sellGo.AddComponent<Button>();
        _sellJunkBtn.targetGraphic = sellImg;
        _sellJunkBtn.onClick.AddListener(OnSellJunkPressed);

        _sellJunkBtnText = CreateText(
            "Txt",
            sellGo.transform,
            "SELL ALL",
            14f,
            Color.white,
            TextAlignmentOptions.Center,
            Vector2.zero,
            Vector2.one,
            true
        );

        // ── COLUMN 2: MARKET ────────────────────────────────────────────────────
        GameObject col2 = CreatePanel(
            "Col2_Market",
            winGo.transform,
            new Vector2(0.305f, 0.17f),
            new Vector2(0.655f, 0.81f),
            PanelBg
        );

        CreateSectionHeader(col2.transform, "MARKET", "FRESH GOODS", Green);

        TMP_Text marketHint = CreateText(
            "Hint",
            col2.transform,
            "Click an item to buy one unit",
            11f,
            TextMuted,
            TextAlignmentOptions.Left,
            new Vector2(0.045f, 0.79f),
            new Vector2(0.95f, 0.855f)
        );

        CreateScrollArea(col2.transform, "MarketScroll", out _marketGrid,
            new Vector2(0.025f, 0.025f), new Vector2(0.975f, 0.79f), 8f);

        _emptyMarketLabel = CreateText(
            "EmptyMarketLabel",
            col2.transform,
            "NO FRESH GOODS\nAVAILABLE TODAY",
            13f,
            TextMuted,
            TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.18f),
            new Vector2(0.92f, 0.67f),
            true
        );
        _emptyMarketLabel.characterSpacing = 0.5f;

        // ── COLUMN 3: STORAGE ───────────────────────────────────────────────────
        GameObject col3 = CreatePanel(
            "Col3_Storage",
            winGo.transform,
            new Vector2(0.675f, 0.17f),
            new Vector2(0.975f, 0.81f),
            PanelBgSoft
        );

        CreateSectionHeader(col3.transform, "STORAGE", "YOUR STOCK", Blue);

        TMP_Text storageHint = CreateText(
            "Hint",
            col3.transform,
            "Fresh and spoiled ingredients you own",
            11f,
            TextMuted,
            TextAlignmentOptions.Left,
            new Vector2(0.045f, 0.79f),
            new Vector2(0.95f, 0.855f)
        );

        CreateScrollArea(col3.transform, "StorageScroll", out _inventoryGrid,
            new Vector2(0.025f, 0.025f), new Vector2(0.975f, 0.79f), 6f);

        _emptyInventoryLabel = CreateText(
            "EmptyLabel",
            col3.transform,
            "STORAGE IS EMPTY\n\nSearch garbage at night\nor buy fresh ingredients.",
            13f,
            TextMuted,
            TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.18f),
            new Vector2(0.92f, 0.67f),
            true
        );
        _emptyInventoryLabel.characterSpacing = 0.5f;

        // Footer
        GameObject footer = CreateRect("Footer", winGo.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0.14f));
        Image footerImg = footer.AddComponent<Image>();
        footerImg.color = Hex("0D1418FF");
        footerImg.raycastTarget = false;

        TMP_Text footerHint = CreateText(
            "FooterHint",
            footer.transform,
            "Shopping pauses the game. Review your stock before continuing.",
            11f,
            TextMuted,
            TextAlignmentOptions.Left,
            new Vector2(0.03f, 0.18f),
            new Vector2(0.59f, 0.82f)
        );

        GameObject contGo = CreateRect("ContinueBtn", footer.transform,
            new Vector2(0.67f, 0.18f), new Vector2(0.965f, 0.82f));
        Image contImg = contGo.AddComponent<Image>();
        contImg.color = Color.white;
        AddOutline(contImg, Hex("45E5A399"), new Vector2(1f, -1f));

        Button contBtn = contGo.AddComponent<Button>();
        contBtn.targetGraphic = contImg;
        contBtn.onClick.AddListener(OnContinuePressed);
        ConfigureButtonColors(
            contBtn,
            GreenDark,
            Hex("27A976FF"),
            Hex("176E4DFF"),
            Hex("2B3538FF")
        );

        TMP_Text cntTxt = CreateText(
            "Txt",
            contGo.transform,
            "START NEXT DAY   >",
            15f,
            Color.white,
            TextAlignmentOptions.Center,
            Vector2.zero,
            Vector2.one,
            true
        );
        cntTxt.characterSpacing = 1f;
    }

    private IEnumerator AnimateShop(bool opening)
    {
        if (_rootGroup == null || _windowRT == null)
            yield break;

        const float duration = 0.16f;
        float elapsed = 0f;

        float fromAlpha = opening ? 0f : 1f;
        float toAlpha = opening ? 1f : 0f;
        Vector3 fromScale = opening ? Vector3.one * 0.965f : Vector3.one;
        Vector3 toScale = opening ? Vector3.one : Vector3.one * 0.985f;

        _rootGroup.alpha = fromAlpha;
        _windowRT.localScale = fromScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            _rootGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
            _windowRT.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
            yield return null;
        }

        _rootGroup.alpha = toAlpha;
        _windowRT.localScale = toScale;
    }

    private static GameObject CreatePanel(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color background)
    {
        GameObject go = CreateRect(name, parent, anchorMin, anchorMax);
        Image img = go.AddComponent<Image>();
        img.color = background;
        AddOutline(img, BorderSoft, new Vector2(1f, -1f));

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = Hex("00000055");
        shadow.effectDistance = new Vector2(0f, -4f);
        shadow.useGraphicAlpha = true;

        return go;
    }

    private static void CreateSectionHeader(Transform parent, string eyebrow, string title, Color accent)
    {
        GameObject accentGo = CreateRect("Accent", parent,
            new Vector2(0.04f, 0.895f), new Vector2(0.06f, 0.97f));
        Image accentImg = accentGo.AddComponent<Image>();
        accentImg.color = accent;
        accentImg.raycastTarget = false;

        TMP_Text eyebrowTmp = CreateText(
            "Eyebrow",
            parent,
            eyebrow,
            9f,
            accent,
            TextAlignmentOptions.Left,
            new Vector2(0.085f, 0.935f),
            new Vector2(0.95f, 0.985f),
            true
        );
        eyebrowTmp.characterSpacing = 2f;

        CreateText(
            "Title",
            parent,
            title,
            19f,
            TextMain,
            TextAlignmentOptions.Left,
            new Vector2(0.085f, 0.855f),
            new Vector2(0.95f, 0.95f),
            true
        );
    }

    private static void CreateScrollArea(
        Transform parent,
        string name,
        out Transform contentTransform,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float spacing)
    {
        GameObject scroll = CreateRect(name, parent, anchorMin, anchorMax);
        Image viewportBg = scroll.AddComponent<Image>();
        viewportBg.color = Hex("0E1519CC");
        AddOutline(viewportBg, Hex("223039FF"), new Vector2(1f, -1f));

        ScrollRect sr = scroll.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 28f;

        // Dedicated Viewport so background outline and scroll rect aren't masked
        GameObject viewportGo = CreateRect("Viewport", scroll.transform, Vector2.zero, Vector2.one);
        RectTransform viewportRT = viewportGo.GetComponent<RectTransform>();
        RectMask2D mask = viewportGo.AddComponent<RectMask2D>();
        mask.padding = new Vector4(2f, 2f, 2f, 2f);
        sr.viewport = viewportRT;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewportGo.transform, false);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = spacing;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        sr.content = contentRT;
        contentTransform = content.transform;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        Vector2 anchorMin,
        Vector2 anchorMax,
        bool bold = false)
    {
        GameObject go = CreateRect(name, parent, anchorMin, anchorMax);
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(tmp);
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    private static GameObject CreateRect(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    private static void AddOutline(Graphic graphic, Color color, Vector2 distance)
    {
        Outline outline = graphic.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private static void ConfigureButtonColors(
        Button button,
        Color normal,
        Color highlighted,
        Color pressed,
        Color disabled)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.selectedColor = highlighted;
        colors.pressedColor = pressed;
        colors.disabledColor = disabled;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
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
                if (old != null)
                {
                    old.enabled = false;
                    Destroy(old);
                }

                es.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.Log("[ShopUI] EventSystem upgraded to InputSystem.");
            }
        }
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Color Hex(string hex)
    {
        if (!hex.StartsWith("#")) hex = "#" + hex;
        return ColorUtility.TryParseHtmlString(hex, out Color result) ? result : Color.magenta;
    }

    private static string ToHex(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }
}
