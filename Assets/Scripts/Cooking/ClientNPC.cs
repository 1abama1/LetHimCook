using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class ClientNPC : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────
    [Header("Order")]
    public RecipeData wantedRecipe;

    [Header("Patience (only ticks at window)")]
    public float patienceTime = 30f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    [Tooltip("Расстояние, при котором считается что NPC достиг точки")]
    public float arrivalThreshold = 0.15f;

    [Header("UI Settings")]
    [Tooltip("Высота слайдера над головой NPC")]
    public float sliderHeightOffset = 2.2f;
    [Tooltip("Высота окошка заказа над головой NPC")]
    public float orderBubbleHeightOffset = 3.0f;

    public UnityEvent onClientAngry;

    // ── Private state ──────────────────────────────────────────────
    private enum State
    {
        WalkingToQueue,
        WaitingInQueue,
        WalkingToWindow,
        AtWindow,
        WalkingToExit,
        Done
    }

    private State         _state = State.WalkingToQueue;
    private Vector3       _queueTarget;
    private float         _currentPatience;
    private bool          _isServed;
    private ClientSpawner _spawner;
    private Animator      _animator;

    // ── Auto-created UI ────────────────────────────────────────────
    private Slider     _patienceSlider;
    private GameObject _patienceSliderGO;
    private GameObject _orderBubbleGO;
    private Canvas     _worldCanvas;

    // ── Init ───────────────────────────────────────────────────────
    public void Init(ClientSpawner spawner)
    {
        _spawner  = spawner;
        _animator = GetComponentInChildren<Animator>();

        if (RecipeManager.Instance != null && RecipeManager.Instance.allRecipes.Count > 0)
        {
            int idx = Random.Range(0, RecipeManager.Instance.allRecipes.Count);
            wantedRecipe = RecipeManager.Instance.allRecipes[idx];
        }

        _currentPatience = patienceTime;

        CreateWorldCanvas();
        CreatePatienceSlider();
        CreateOrderBubble();
    }

    // ── UI Creation ────────────────────────────────────────────────

    void CreateWorldCanvas()
    {
        var go = new GameObject("ClientCanvas");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        _worldCanvas = go.AddComponent<Canvas>();
        _worldCanvas.renderMode = RenderMode.WorldSpace;
        _worldCanvas.sortingOrder = 10;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;

        go.AddComponent<GraphicRaycaster>();

        // Масштаб чтобы UI не был огромным в мировых координатах
        go.transform.localScale = Vector3.one * 0.01f;
    }

    void CreatePatienceSlider()
    {
        _patienceSliderGO = new GameObject("PatienceSlider");
        _patienceSliderGO.transform.SetParent(_worldCanvas.transform, false);

        // Позиция над головой (в локальных единицах canvas)
        var rect = _patienceSliderGO.AddComponent<RectTransform>();
        rect.sizeDelta        = new Vector2(120f, 14f);
        rect.anchoredPosition = new Vector2(0f, sliderHeightOffset * 100f);

        // Фон слайдера
        var bg = new GameObject("Background");
        bg.transform.SetParent(_patienceSliderGO.transform, false);
        var bgRect  = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        var bgImg        = bg.AddComponent<Image>();
        bgImg.color      = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        // Fill area
        var fillArea     = new GameObject("Fill Area");
        fillArea.transform.SetParent(_patienceSliderGO.transform, false);
        var faRect       = fillArea.AddComponent<RectTransform>();
        faRect.anchorMin = new Vector2(0f, 0f);
        faRect.anchorMax = new Vector2(1f, 1f);
        faRect.offsetMin = new Vector2(2f, 2f);
        faRect.offsetMax = new Vector2(-2f, -2f);

        var fill     = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        var fillImg  = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.85f, 0.3f);

        // Slider component
        _patienceSlider                     = _patienceSliderGO.AddComponent<Slider>();
        _patienceSlider.fillRect            = fillRect;
        _patienceSlider.targetGraphic       = fillImg;
        _patienceSlider.direction           = Slider.Direction.LeftToRight;
        _patienceSlider.minValue            = 0f;
        _patienceSlider.maxValue            = 1f;
        _patienceSlider.value               = 1f;
        _patienceSlider.interactable        = false;

        _patienceSliderGO.SetActive(false);
    }

    void CreateOrderBubble()
    {
        _orderBubbleGO = new GameObject("OrderBubble");
        _orderBubbleGO.transform.SetParent(_worldCanvas.transform, false);

        var rect          = _orderBubbleGO.AddComponent<RectTransform>();
        rect.sizeDelta    = new Vector2(90f, 90f);
        rect.anchoredPosition = new Vector2(0f, orderBubbleHeightOffset * 100f);

        // Фоновый пузырь
        var bubble        = new GameObject("Bubble");
        bubble.transform.SetParent(_orderBubbleGO.transform, false);
        var bubbleRect    = bubble.AddComponent<RectTransform>();
        bubbleRect.anchorMin = Vector2.zero;
        bubbleRect.anchorMax = Vector2.one;
        bubbleRect.sizeDelta = Vector2.zero;
        var bubbleImg     = bubble.AddComponent<Image>();
        bubbleImg.color   = new Color(1f, 0.97f, 0.85f, 0.95f);

        // Иконка блюда
        if (wantedRecipe != null && wantedRecipe.icon != null)
        {
            var iconGO       = new GameObject("DishIcon");
            iconGO.transform.SetParent(_orderBubbleGO.transform, false);
            var iconRect     = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin       = new Vector2(0.1f, 0.35f);
            iconRect.anchorMax       = new Vector2(0.9f, 0.95f);
            iconRect.sizeDelta       = Vector2.zero;
            var iconImg      = iconGO.AddComponent<Image>();
            iconImg.sprite   = wantedRecipe.icon;
            iconImg.preserveAspect = true;
        }

        // Название блюда
        var labelGO          = new GameObject("DishLabel");
        labelGO.transform.SetParent(_orderBubbleGO.transform, false);
        var labelRect        = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin  = new Vector2(0f, 0f);
        labelRect.anchorMax  = new Vector2(1f, 0.38f);
        labelRect.sizeDelta  = Vector2.zero;
        labelRect.offsetMin  = new Vector2(4f, 2f);
        labelRect.offsetMax  = new Vector2(-4f, -2f);

        // Пробуем TextMeshPro, иначе обычный Text
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text      = wantedRecipe != null ? wantedRecipe.recipeName : "?";
            tmp.fontSize  = 10f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = new Color(0.2f, 0.1f, 0f);
        }

        _orderBubbleGO.SetActive(false);
    }

    // ── Called by ClientSpawner ────────────────────────────────────
    public void SetQueueTarget(Vector3 position, bool isFirst)
    {
        _queueTarget = position;

        if (_state == State.AtWindow ||
            _state == State.WalkingToWindow ||
            _state == State.WalkingToExit ||
            _state == State.Done)
            return;

        _state = State.WalkingToQueue;
    }

    public void GoToWindow()
    {
        if (_state == State.Done) return;
        _state = State.WalkingToWindow;
    }

    // ── Unity loop ─────────────────────────────────────────────────
    void Update()
    {
        // Canvas всегда смотрит на камеру
        if (_worldCanvas != null && Camera.main != null)
            _worldCanvas.transform.rotation = Camera.main.transform.rotation;

        // Цвет слайдера по уровню терпения
        UpdateSliderColor();

        if (_animator != null)
        {
            bool isWalking = _state == State.WalkingToQueue ||
                             _state == State.WalkingToWindow ||
                             _state == State.WalkingToExit;
            _animator.SetBool("IsWalking", isWalking);
        }

        switch (_state)
        {
            case State.WalkingToQueue:
                MoveTowards(_queueTarget);
                if (Arrived(_queueTarget))
                    _state = State.WaitingInQueue;
                break;

            case State.WaitingInQueue:
                break;

            case State.WalkingToWindow:
                if (_spawner == null) break;
                Vector3 windowPos = _spawner.WindowPoint.position;
                MoveTowards(windowPos);
                if (Arrived(windowPos))
                    ArriveAtWindow();
                break;

            case State.AtWindow:
                TickPatience();
                break;

            case State.WalkingToExit:
                if (_spawner == null) break;
                Vector3 exitPos = _spawner.ExitPoint.position;
                MoveTowards(exitPos);
                if (Arrived(exitPos))
                {
                    _state = State.Done;
                    Destroy(gameObject);
                }
                break;

            case State.Done:
                break;
        }
    }

    void UpdateSliderColor()
    {
        if (_patienceSlider == null) return;

        float t = _currentPatience / patienceTime;
        // Зелёный → жёлтый → красный
        Color c = t > 0.5f
            ? Color.Lerp(new Color(1f, 0.8f, 0f), new Color(0.2f, 0.85f, 0.3f), (t - 0.5f) * 2f)
            : Color.Lerp(new Color(0.9f, 0.1f, 0.1f), new Color(1f, 0.8f, 0f), t * 2f);

        if (_patienceSlider.fillRect != null)
        {
            var img = _patienceSlider.fillRect.GetComponent<Image>();
            if (img != null) img.color = c;
        }
    }

    // ── Internal helpers ───────────────────────────────────────────
    void MoveTowards(Vector3 target)
    {
        Vector3 flat = new Vector3(target.x, transform.position.y, target.z);
        transform.position = Vector3.MoveTowards(
            transform.position, flat, moveSpeed * Time.deltaTime);

        Vector3 dir = (flat - transform.position);
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                10f * Time.deltaTime);
    }

    bool Arrived(Vector3 target)
    {
        Vector3 flat = new Vector3(target.x, transform.position.y, target.z);
        return Vector3.Distance(transform.position, flat) <= arrivalThreshold;
    }

    void ArriveAtWindow()
    {
        _state = State.AtWindow;

        if (_patienceSliderGO != null) _patienceSliderGO.SetActive(true);
        if (_orderBubbleGO    != null) _orderBubbleGO.SetActive(true);

        // Когда клиент подходит к окошку, он назначает свой рецепт глобальным
        if (RecipeManager.Instance != null)
        {
            RecipeManager.Instance.AssignRecipe(wantedRecipe);
        }
    }

    void TickPatience()
    {
        if (_isServed) return;

        _currentPatience -= Time.deltaTime;

        if (_patienceSlider != null)
            _patienceSlider.value = _currentPatience / patienceTime;

        if (_currentPatience <= 0f)
            LeaveAngry();
    }

    // ── Public API ─────────────────────────────────────────────────
    public float      PatiencePercent => Mathf.Clamp01(_currentPatience / patienceTime);
    public RecipeData WantedRecipe    => wantedRecipe;
    public bool       IsAtWindow      => _state == State.AtWindow;

    public void ReceiveDish(RecipeData cookedRecipe, bool usedSpoiled)
    {
        if (_state != State.AtWindow || _isServed) return;
        if (cookedRecipe != wantedRecipe) return;

        _isServed = true;
        HideUI();

        if (usedSpoiled)
        {
            DialogueSystem.Instance?.StartDialogue("client_complaint", this, null);
            RestaurantReputation.Instance?.ChangeReputation(-15f);
        }
        else
        {
            RestaurantReputation.Instance?.ChangeReputation(5f);
        }

        LeaveHappy();
    }

    // ── Leave methods ──────────────────────────────────────────────
    void LeaveHappy()
    {
        _state = State.WalkingToExit;
        HideUI();
        _spawner?.OnClientLeft(this);
    }

    void LeaveAngry()
    {
        if (_isServed) return;
        _isServed = true;

        onClientAngry?.Invoke();
        PlayerEconomy.Instance?.SpendMoney(10);
        RestaurantReputation.Instance?.ChangeReputation(-10f);

        _state = State.WalkingToExit;
        HideUI();
        _spawner?.OnClientLeft(this);
    }

    void HideUI()
    {
        if (_patienceSliderGO != null) _patienceSliderGO.SetActive(false);
        if (_orderBubbleGO    != null) _orderBubbleGO.SetActive(false);
    }
}
