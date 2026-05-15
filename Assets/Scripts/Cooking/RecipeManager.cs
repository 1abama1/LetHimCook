using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

public class RecipeManager : MonoBehaviour
{
    public static RecipeManager Instance { get; private set; }

    public List<RecipeData> allRecipes = new List<RecipeData>();

    public RecipeData currentRecipe;
    private List<string> _addedIngredients = new List<string>();
    private bool _usedSpoiled;

    public UnityEvent<RecipeData> onRecipeAssigned;
    public UnityEvent<string>     onIngredientAdded;
    public UnityEvent<RecipeData, bool> onDishCompleted;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void AssignRandomRecipe()
    {
        if (allRecipes.Count == 0) return;
        AssignRecipe(allRecipes[Random.Range(0, allRecipes.Count)]);
    }

    public void AssignRecipe(RecipeData recipe)
    {
        currentRecipe = recipe;
        _addedIngredients.Clear();
        _usedSpoiled = false;
        Debug.Log($"[RecipeManager] Assigned new order: {recipe.recipeName}");
        onRecipeAssigned?.Invoke(recipe);
    }

    // AddIngredient tracks ingredients for UI/HUD purposes only — it no longer
    // triggers ServeDish to avoid double-payment. Money is only paid via CheckAndServeDish.
    public void AddIngredient(IngredientData ingredient)
    {
        if (currentRecipe == null) return;

        bool needed = false;
        foreach (string req in currentRecipe.requiredIngredientNames)
        {
            if (req == ingredient.ingredientName && !_addedIngredients.Contains(req))
            {
                needed = true;
                break;
            }
        }
        if (!needed) return;

        _addedIngredients.Add(ingredient.ingredientName);
        if (ingredient.IsSpoiled) _usedSpoiled = true;

        Debug.Log($"[RecipeManager] Tracked {ingredient.ingredientName} for HUD. (Spoiled: {ingredient.IsSpoiled})");

        ResourceType resType = ingredient.IsSpoiled
            ? ResourceType.SpoiledIngredient
            : ResourceType.FreshIngredient;
        PlayerEconomy.Instance?.UseResource(resType, 1);

        onIngredientAdded?.Invoke(ingredient.ingredientName);
        // NOTE: No longer calls CheckCompletion/ServeDish here.
        // Serving happens exclusively through CheckAndServeDish (called by DeliveryCounter).
    }

    // Returns the matched recipe (before clearing it) on success, or null on failure.
    // Caller is responsible for notifying the correct ClientNPC.
    public RecipeData CheckAndServeDish(List<IngredientData> submittedIngredients, out bool hasSpoiled)
    {
        hasSpoiled = false;
        if (currentRecipe == null) return null;

        List<string> submittedNames = new List<string>();
        foreach (var ing in submittedIngredients)
        {
            submittedNames.Add(ing.ingredientName);
            if (ing.IsSpoiled) hasSpoiled = true;
        }

        bool isMatch = currentRecipe.requiredIngredientNames.Length == submittedNames.Count &&
                       currentRecipe.requiredIngredientNames.All(submittedNames.Contains);

        if (isMatch)
        {
            // Capture recipe BEFORE clearing so caller can notify the right ClientNPC
            RecipeData matched = currentRecipe;

            Debug.Log($"[RecipeManager] УСПЕХ! Блюдо '{matched.recipeName}' совпало. Награда начислена.");
            onDishCompleted?.Invoke(matched, hasSpoiled);

            int pay = matched.basePayment + (hasSpoiled ? matched.spoiledBonusPayment : 0);
            PlayerEconomy.Instance?.EarnMoney(pay);

            currentRecipe = null;
            _addedIngredients.Clear();
            _usedSpoiled = false;
            return matched; // Success — caller notifies ClientNPC
        }

        // Подробная диагностика ошибки для консоли
        string required = string.Join(", ", currentRecipe.requiredIngredientNames);
        string submitted = submittedNames.Count > 0 ? string.Join(", ", submittedNames) : "пусто";

        if (currentRecipe.requiredIngredientNames.Length != submittedNames.Count)
        {
            Debug.LogWarning($"[RecipeManager] ОТКАЗ: Неверное количество предметов! Рецепт '{currentRecipe.recipeName}' требует {currentRecipe.requiredIngredientNames.Length} инг. ({required}), а на тарелке {submittedNames.Count} ({submitted}).");
        }
        else
        {
            var missing = currentRecipe.requiredIngredientNames.Where(name => !submittedNames.Contains(name)).ToList();
            Debug.LogWarning($"[RecipeManager] ОТКАЗ: Ошибка в составе! Рецепт: [{required}]. На тарелке: [{submitted}]. Не хватает: {string.Join(", ", missing)}.");
        }

        return null; // Wrong dish
    }

    public List<string> GetMissingIngredients()
    {
        var missing = new List<string>();
        if (currentRecipe == null) return missing;
        foreach (string req in currentRecipe.requiredIngredientNames)
            if (!_addedIngredients.Contains(req)) missing.Add(req);
        return missing;
    }
}
