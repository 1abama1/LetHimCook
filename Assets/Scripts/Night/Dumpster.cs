using UnityEngine;
using System.Collections.Generic;

public class Dumpster : MonoBehaviour, IInteractable
{
    [Header("Loot Settings")]
    public int  minSpoiled     = 1;
    public int  maxSpoiled     = 4;
    public int  junkLootChance = 50;
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
            PlayerEconomy.Instance?.AddResource(ResourceType.JunkLoot, Random.Range(1, 3));

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
