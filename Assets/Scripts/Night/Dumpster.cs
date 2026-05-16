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

    public GameObject searchParticlePrefab;

    public void Interact(PlayerController player)
    {
        if (alreadySearched) return;

        alreadySearched = true;
        AudioManager.Instance?.PlayAt("dumpster_open", transform.position);

        int count = Random.Range(minSpoiled, maxSpoiled + 1);

        // Берём ингредиенты из пула, или фоллбэк на дефолтный
        for (int i = 0; i < count; i++)
        {
            IngredientData data = PickSpoiled();
            if (data != null)
                PlayerEconomy.Instance?.AddIngredient(data);
            else
                PlayerEconomy.Instance?.AddResource(ResourceType.SpoiledIngredient, 1); // старый путь
        }

        if (Random.Range(0, 100) < junkLootChance)
        {
            int amount = Random.Range(1, 3);
            PlayerEconomy.Instance?.AddResource(ResourceType.JunkLoot, amount);
            DialogueSystem.Instance?.ShowDynamicDialogue("Dumpster", $"You searched the dumpster and found:\n- Junk x{amount}", lootPortrait);
        }
        else
        {
            DialogueSystem.Instance?.ShowDynamicDialogue("Dumpster", "You searched the dumpster, but found nothing.", lootPortrait);
        }
        
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
