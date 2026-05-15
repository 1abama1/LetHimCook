using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

public enum ResourceType
{
    FreshIngredient,
    SpoiledIngredient,
    Money,
    JunkLoot
}

[System.Serializable]
public class IngredientStack
{
    public IngredientData ingredient;
    public int amount;
}

public class PlayerEconomy : MonoBehaviour
{
    public static PlayerEconomy Instance { get; private set; }

    public int startingMoney = 50;
    public int freshIngredientCostPerUnit = 10;

    [Header("Fallbacks")]
    public IngredientData defaultFreshIngredient;
    public IngredientData defaultSpoiledIngredient;

    [SerializeField] private int _money;
    [SerializeField] private int _junkLoot;

    public List<IngredientStack> inventory = new List<IngredientStack>();

    public int Money => _money;
    public int FreshIngredients => GetTotalByType(true);
    public int SpoiledIngredients => GetTotalByType(false);
    public int JunkLoot => _junkLoot;

    private int GetTotalByType(bool safe)
    {
        int count = 0;
        foreach (var stack in inventory)
        {
            if (stack.ingredient != null && (stack.ingredient.IsSafe == safe))
                count += stack.amount;
        }
        return count;
    }

    /// <summary>Возвращает кол-во конкретного ингредиента (сравнение по ingredientName).</summary>
    public int GetIngredientCount(IngredientData data)
    {
        if (data == null) return 0;
        var stack = inventory.Find(s => s.ingredient != null && s.ingredient.ingredientName == data.ingredientName);
        return stack != null ? stack.amount : 0;
    }

    /// <summary>Снимает один экземпляр конкретного ингредиента из инвентаря.
    /// Возвращает true при успехе.</summary>
    public bool RemoveIngredientOfType(IngredientData data)
    {
        if (data == null) return false;
        var stack = inventory.Find(s => s.ingredient != null && s.ingredient.ingredientName == data.ingredientName);
        
        if (stack != null && stack.amount > 0)
        {
            stack.amount--;
            // Удаляем пустые стаки, чтобы не захламлять инспектор (опционально)
            // if (stack.amount <= 0) inventory.Remove(stack); 

            if (data.IsSafe)
                onResourceChanged?.Invoke(ResourceType.FreshIngredient, FreshIngredients);
            else
                onResourceChanged?.Invoke(ResourceType.SpoiledIngredient, SpoiledIngredients);
            onInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    public UnityEvent<int> onMoneyChanged;
    public UnityEvent<ResourceType, int> onResourceChanged;
    
    // Новое событие для обновления инвентаря
    public UnityEvent onInventoryChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _money = startingMoney;
        onMoneyChanged?.Invoke(_money);
        onInventoryChanged?.Invoke();
    }

    public void AddIngredient(IngredientData ingredient)
    {
        if (ingredient == null) return;
        
        var stack = inventory.Find(s => s.ingredient == ingredient);
        if (stack != null)
        {
            stack.amount++;
        }
        else
        {
            inventory.Add(new IngredientStack { ingredient = ingredient, amount = 1 });
        }
        
        // Обновляем UI счетчики
        if (ingredient.IsSafe)
            onResourceChanged?.Invoke(ResourceType.FreshIngredient, FreshIngredients);
        else
            onResourceChanged?.Invoke(ResourceType.SpoiledIngredient, SpoiledIngredients);
            
        onInventoryChanged?.Invoke();
    }
    
    public bool RemoveIngredient(IngredientData ingredient)
    {
        var stack = inventory.Find(s => s.ingredient == ingredient);
        if (stack != null && stack.amount > 0)
        {
            stack.amount--;
            
            if (ingredient.IsSafe)
                onResourceChanged?.Invoke(ResourceType.FreshIngredient, FreshIngredients);
            else
                onResourceChanged?.Invoke(ResourceType.SpoiledIngredient, SpoiledIngredients);
                
            onInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool SpendMoney(int amount)
    {
        if (_money < amount) return false;
        _money -= amount;
        onMoneyChanged?.Invoke(_money);
        return true;
    }

    public void EarnMoney(int amount)
    {
        _money += amount;
        onMoneyChanged?.Invoke(_money);
    }

    public void AddResource(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.FreshIngredient:   
                if (defaultFreshIngredient != null) AddIngredientAmount(defaultFreshIngredient, amount);
                break;
            case ResourceType.SpoiledIngredient: 
                if (defaultSpoiledIngredient != null) AddIngredientAmount(defaultSpoiledIngredient, amount);
                break;
            case ResourceType.JunkLoot:          
                _junkLoot += amount; 
                onResourceChanged?.Invoke(type, GetResource(type));
                break;
        }
    }

    private void AddIngredientAmount(IngredientData data, int amount)
    {
        var stack = inventory.Find(s => s.ingredient == data);
        if (stack != null) stack.amount += amount;
        else inventory.Add(new IngredientStack { ingredient = data, amount = amount });

        if (data.IsSafe) onResourceChanged?.Invoke(ResourceType.FreshIngredient, FreshIngredients);
        else onResourceChanged?.Invoke(ResourceType.SpoiledIngredient, SpoiledIngredients);
        onInventoryChanged?.Invoke();
    }

    public bool UseResource(ResourceType type, int amount)
    {
        if (GetResource(type) < amount) return false;
        switch (type)
        {
            case ResourceType.FreshIngredient:   
                RemoveByType(true, amount);
                onResourceChanged?.Invoke(type, FreshIngredients);
                onInventoryChanged?.Invoke();
                break;
            case ResourceType.SpoiledIngredient: 
                RemoveByType(false, amount);
                onResourceChanged?.Invoke(type, SpoiledIngredients);
                onInventoryChanged?.Invoke();
                break;
            case ResourceType.JunkLoot:          
                _junkLoot -= amount; 
                onResourceChanged?.Invoke(type, GetResource(type));
                break;
        }
        return true;
    }

    private void RemoveByType(bool safe, int amount)
    {
        int toRemove = amount;
        for (int i = inventory.Count - 1; i >= 0 && toRemove > 0; i--)
        {
            if (inventory[i].ingredient != null && inventory[i].ingredient.IsSafe == safe)
            {
                int take = Mathf.Min(inventory[i].amount, toRemove);
                inventory[i].amount -= take;
                toRemove -= take;
            }
        }
    }

    public int GetResource(ResourceType type) => type switch
    {
        ResourceType.FreshIngredient   => FreshIngredients,
        ResourceType.SpoiledIngredient => SpoiledIngredients,
        ResourceType.JunkLoot          => _junkLoot,
        ResourceType.Money             => _money,
        _                              => 0
    };

    public bool BuyFreshIngredients(int units)
    {
        if (!SpendMoney(units * freshIngredientCostPerUnit)) return false;
        AddResource(ResourceType.FreshIngredient, units);
        return true;
    }

    public void SellJunkLoot()
    {
        int revenue = _junkLoot * 3;
        _junkLoot = 0;
        EarnMoney(revenue);
        onResourceChanged?.Invoke(ResourceType.JunkLoot, 0);
    }

    public void LoseAllNightResources()
    {
        foreach(var stack in inventory)
        {
            if (stack.ingredient != null && stack.ingredient.IsSpoiled)
                stack.amount = 0;
        }
        _junkLoot = 0;
        onResourceChanged?.Invoke(ResourceType.SpoiledIngredient, 0);
        onResourceChanged?.Invoke(ResourceType.JunkLoot, 0);
        onInventoryChanged?.Invoke();
    }
}
