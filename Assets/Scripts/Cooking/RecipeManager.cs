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
        Debug.Log($"[RecipeManager] Assigned new order: {recipe.dishName}");
        onRecipeAssigned?.Invoke(recipe);
    }

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
        
        Debug.Log($"[RecipeManager] Added {ingredient.ingredientName} to dish! (Spoiled: {ingredient.IsSpoiled})");

        ResourceType resType = ingredient.IsSpoiled
            ? ResourceType.SpoiledIngredient
            : ResourceType.FreshIngredient;
        PlayerEconomy.Instance?.UseResource(resType, 1);

        onIngredientAdded?.Invoke(ingredient.ingredientName);
        CheckCompletion();
    }

    void CheckCompletion()
    {
        foreach (string req in currentRecipe.requiredIngredientNames)
            if (!_addedIngredients.Contains(req)) return;
        ServeDish();
    }

    void ServeDish()
    {
        Debug.Log($"[RecipeManager] Dish '{currentRecipe.dishName}' completed and served! Spoiled used: {_usedSpoiled}");
        onDishCompleted?.Invoke(currentRecipe, _usedSpoiled);

        int pay = currentRecipe.basePayment + (_usedSpoiled ? currentRecipe.spoiledBonusPayment : 0);
        PlayerEconomy.Instance?.EarnMoney(pay);

        currentRecipe = null;
        _addedIngredients.Clear();
        _usedSpoiled = false;
    }

    public bool CheckAndServeDish(List<IngredientData> submittedIngredients)
    {
        if (currentRecipe == null) return false;

        // Собираем названия того, что принесли
        List<string> submittedNames = new List<string>();
        bool hasSpoiled = false;

        foreach (var ing in submittedIngredients)
        {
            submittedNames.Add(ing.ingredientName);
            if (ing.IsSpoiled) hasSpoiled = true;
        }

        // Проверяем, совпадают ли списки (без учета порядка)
        bool isMatch = currentRecipe.requiredIngredientNames.Length == submittedNames.Count &&
                       currentRecipe.requiredIngredientNames.All(submittedNames.Contains);

        if (isMatch)
        {
            Debug.Log($"Блюдо {currentRecipe.dishName} успешно собрано и отдано!");
            
            // Вызываем событие завершения
            onDishCompleted?.Invoke(currentRecipe, hasSpoiled);
            
            // Начисляем деньги
            int pay = currentRecipe.basePayment + (hasSpoiled ? currentRecipe.spoiledBonusPayment : 0);
            PlayerEconomy.Instance?.EarnMoney(pay);

            // Очищаем текущий рецепт
            currentRecipe = null;
            return true; // Успех
        }

        Debug.Log("Блюдо собрано неверно!");
        return false; // Ошибка в рецепте
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
