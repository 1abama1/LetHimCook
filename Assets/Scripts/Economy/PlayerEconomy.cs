using UnityEngine;
using UnityEngine.Events;

public enum ResourceType
{
    FreshIngredient,
    SpoiledIngredient,
    Money,
    JunkLoot
}

public class PlayerEconomy : MonoBehaviour
{
    public static PlayerEconomy Instance { get; private set; }

    public int startingMoney = 50;
    public int freshIngredientCostPerUnit = 10;

    [SerializeField] private int _money;
    [SerializeField] private int _fresh;
    [SerializeField] private int _spoiled;
    [SerializeField] private int _junkLoot;

    public int Money => _money;
    public int FreshIngredients => _fresh;
    public int SpoiledIngredients => _spoiled;
    public int JunkLoot => _junkLoot;

    public UnityEvent<int> onMoneyChanged;
    public UnityEvent<ResourceType, int> onResourceChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _money = startingMoney;
        onMoneyChanged?.Invoke(_money);
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
            case ResourceType.FreshIngredient:   _fresh    += amount; break;
            case ResourceType.SpoiledIngredient: _spoiled  += amount; break;
            case ResourceType.JunkLoot:          _junkLoot += amount; break;
        }
        onResourceChanged?.Invoke(type, GetResource(type));
    }

    public bool UseResource(ResourceType type, int amount)
    {
        if (GetResource(type) < amount) return false;
        switch (type)
        {
            case ResourceType.FreshIngredient:   _fresh    -= amount; break;
            case ResourceType.SpoiledIngredient: _spoiled  -= amount; break;
            case ResourceType.JunkLoot:          _junkLoot -= amount; break;
        }
        onResourceChanged?.Invoke(type, GetResource(type));
        return true;
    }

    public int GetResource(ResourceType type) => type switch
    {
        ResourceType.FreshIngredient   => _fresh,
        ResourceType.SpoiledIngredient => _spoiled,
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
        _spoiled  = 0;
        _junkLoot = 0;
        onResourceChanged?.Invoke(ResourceType.SpoiledIngredient, 0);
        onResourceChanged?.Invoke(ResourceType.JunkLoot, 0);
    }
}
