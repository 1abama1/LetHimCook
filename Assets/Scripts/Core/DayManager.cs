using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Менеджер дневной фазы.
/// При старте дня: назначает рецепт, продаёт хлам, сбрасывает нужные состояния.
/// При конце дня: автоматически продаёт накопленный JunkLoot.
/// </summary>
public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Day Start")]
    [Tooltip("Автоматически назначить случайный рецепт при старте дня")]
    public bool autoAssignRecipe = true;

    [Tooltip("Стоимость закупки свежих ингредиентов в начале дня")]
    public int freshIngredientsBudget = 3;

    [Header("End of Day")]
    [Tooltip("Автоматически продавать JunkLoot в конце дня")]
    public bool autoSellJunk = true;

    [Header("Tutorial")]
    public GameObject tutorialPointerPrefab;
    private System.Collections.Generic.List<TutorialPointer> _activePointers = new System.Collections.Generic.List<TutorialPointer>();

    [Header("Events")]
    public UnityEvent onDayStart;
    public UnityEvent onDayEnd;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onDayBegin.AddListener(OnDayBegin);
            GameManager.Instance.onNightBegin.AddListener(OnNightBegin);

            if (GameManager.Instance.currentPhase == GamePhase.Day)
                OnDayBegin();
        }
    }

    // ─── Начало дня ───────────────────────────────────────────────────────────
    public void OnDayBegin()
    {
        // Назначаем рецепт
        if (autoAssignRecipe && RecipeManager.Instance != null)
        {
            RecipeManager.Instance.AssignRandomRecipe();
            Debug.Log("[DayManager] New day started — recipe assigned.");
        }

        if (StoryManager.Instance != null && StoryManager.Instance.CurrentDay == 1 && tutorialPointerPrefab != null)
        {
            var stations = FindObjectsByType<CookingStation>(FindObjectsSortMode.None);
            foreach (var st in stations) _activePointers.Add(TutorialPointer.Create(st.transform, tutorialPointerPrefab));

            var deliveries = FindObjectsByType<DeliveryCounter>(FindObjectsSortMode.None);
            foreach (var del in deliveries) _activePointers.Add(TutorialPointer.Create(del.transform, tutorialPointerPrefab));
        }

        onDayStart?.Invoke();
    }

    // ─── Конец дня / начало ночи ──────────────────────────────────────────────
    public void OnNightBegin()
    {
        foreach (var p in _activePointers) if (p != null) Destroy(p.gameObject);
        _activePointers.Clear();

        // 1. Очистка рук игрока
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.ClearHandsForNight();

        // 2. Сброс кухонных станций
        var stations = FindObjectsByType<CookingStation>(FindObjectsSortMode.None);
        foreach(var st in stations) st.ResetStation();

        // 3. Сброс столов (Countertops)
        var countertops = FindObjectsByType<Countertop>(FindObjectsSortMode.None);
        foreach(var ct in countertops) ct.ResetCountertop();

        // 4. Удаление оставшихся тарелок
        var plates = FindObjectsByType<Plate>(FindObjectsSortMode.None);
        foreach(var p in plates) Destroy(p.gameObject);

        // 5. Удаление клиентов и очереди
        var clients = FindObjectsByType<ClientNPC>(FindObjectsSortMode.None);
        foreach(var c in clients) Destroy(c.gameObject);
        
        var spawner = FindFirstObjectByType<ClientSpawner>();
        if (spawner != null) spawner.ClearQueue();

        onDayEnd?.Invoke();
    }

    /// <summary>Ручная закупка свежих ингредиентов (кнопка в меню магазина).</summary>
    public bool BuyFreshIngredients(int units)
    {
        bool ok = PlayerEconomy.Instance?.BuyFreshIngredients(units) ?? false;
        if (ok) AudioManager.Instance?.Play("buy_ingredients");
        else    AudioManager.Instance?.Play("error");
        return ok;
    }
}
