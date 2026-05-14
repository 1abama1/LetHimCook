using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

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
        onDishCompleted?.Invoke(currentRecipe, _usedSpoiled);

        int pay = currentRecipe.basePayment + (_usedSpoiled ? currentRecipe.spoiledBonusPayment : 0);
        PlayerEconomy.Instance?.EarnMoney(pay);

        currentRecipe = null;
        _addedIngredients.Clear();
        _usedSpoiled = false;
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
