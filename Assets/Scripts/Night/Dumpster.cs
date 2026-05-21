using UnityEngine;
using System.Collections.Generic;

public class Dumpster : MonoBehaviour, IInteractable
{
    [Header("Loot Settings")]
    public int  minSpoiled     = 1;
    public int  maxSpoiled     = 4;
    public int  junkLootChance = 50;
    public Sprite lootPortrait;
    public bool alreadySearched;

    [Tooltip("Пул просроченных IngredientData, которые могут выпасть из бака. " +
             "Если пуст — используется defaultSpoiledIngredient из PlayerEconomy.")]
    public List<IngredientData> spoiledIngredientPool = new List<IngredientData>();

    [Tooltip("Пул свежих IngredientData, которые могут выпасть из бака с шансом freshFoodChance.")]
    public List<IngredientData> freshIngredientPool = new List<IngredientData>();

    [Range(0, 100)]
    public int freshFoodChance = 15;

    public GameObject searchParticlePrefab;

    public void ResetForNewNight()
    {
        alreadySearched = false;
    }

    public void Interact(PlayerController player)
    {
        if (alreadySearched) return;

        alreadySearched = true;
        AudioManager.Instance?.PlayAt("dumpster_open", transform.position);

        int count = Random.Range(minSpoiled, maxSpoiled + 1);
        Dictionary<string, int> foundItems = new Dictionary<string, int>();

        // Берём ингредиенты из пула, или фоллбэк на дефолтный
        for (int i = 0; i < count; i++)
        {
            IngredientData data = null;
            if (freshIngredientPool != null && freshIngredientPool.Count > 0 && Random.Range(0, 100) < freshFoodChance)
            {
                data = freshIngredientPool[Random.Range(0, freshIngredientPool.Count)];
            }
            else
            {
                data = PickSpoiled();
            }

            if (data != null)
            {
                PlayerEconomy.Instance?.AddIngredient(data);
                string name = data.ingredientName;
                if (foundItems.ContainsKey(name))
                    foundItems[name]++;
                else
                    foundItems[name] = 1;
            }
            else
            {
                PlayerEconomy.Instance?.AddResource(ResourceType.SpoiledIngredient, 1); // старый путь
                string name = "Spoiled Ingredient";
                if (foundItems.ContainsKey(name))
                    foundItems[name]++;
                else
                    foundItems[name] = 1;
            }
        }

        if (Random.Range(0, 100) < junkLootChance)
        {
            int amount = Random.Range(1, 3);
            PlayerEconomy.Instance?.AddResource(ResourceType.JunkLoot, amount);
            foundItems["Junk"] = amount;
        }

        string lootText = "You searched the dumpster and found:\n";
        if (foundItems.Count > 0)
        {
            foreach (var pair in foundItems)
            {
                lootText += $"- {pair.Key} x{pair.Value}\n";
            }
        }
        else
        {
            lootText = "You searched the dumpster, but found nothing.";
        }

        DialogueSystem.Instance?.ShowDynamicDialogue("Dumpster", lootText, lootPortrait);
        
        if (searchParticlePrefab != null)
            Instantiate(searchParticlePrefab, transform.position, Quaternion.identity);

        Debug.Log($"[Dumpster] Нашёл {count} ингредиентов.");
    }

    IngredientData PickSpoiled()
    {
        if (spoiledIngredientPool.Count > 0)
            return spoiledIngredientPool[Random.Range(0, spoiledIngredientPool.Count)];

        return PlayerEconomy.Instance?.defaultSpoiledIngredient;
    }
}
