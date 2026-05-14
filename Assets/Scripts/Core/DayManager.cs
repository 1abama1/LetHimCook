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
        // Покупаем свежие ингредиенты автоматически (если есть деньги)
        if (freshIngredientsBudget > 0)
            PlayerEconomy.Instance?.BuyFreshIngredients(freshIngredientsBudget);

        // Назначаем рецепт
        if (autoAssignRecipe && RecipeManager.Instance != null)
        {
            RecipeManager.Instance.AssignRandomRecipe();
            Debug.Log("[DayManager] New day started — recipe assigned.");
        }

        onDayStart?.Invoke();
    }

    // ─── Конец дня / начало ночи ──────────────────────────────────────────────
    public void OnNightBegin()
    {
        if (autoSellJunk)
        {
            int junk = PlayerEconomy.Instance?.JunkLoot ?? 0;
            if (junk > 0)
            {
                PlayerEconomy.Instance.SellJunkLoot();
                Debug.Log($"[DayManager] Night started — sold {junk} junk loot.");
            }
        }

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
