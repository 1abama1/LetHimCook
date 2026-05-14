using UnityEngine;

public class Dumpster : MonoBehaviour, IInteractable
{
    public int  minSpoiled      = 1;
    public int  maxSpoiled      = 4;
    public int  junkLootChance  = 50;
    public bool alreadySearched;

    public GameObject searchParticlePrefab;

    public void Interact(PlayerController player)
    {
        if (alreadySearched) return;

        alreadySearched = true;
        AudioManager.Instance?.PlayAt("dumpster_open", transform.position);
        int spoiled = Random.Range(minSpoiled, maxSpoiled + 1);
        PlayerEconomy.Instance?.AddResource(ResourceType.SpoiledIngredient, spoiled);

        if (Random.Range(0, 100) < junkLootChance)
            PlayerEconomy.Instance?.AddResource(ResourceType.JunkLoot, Random.Range(1, 3));

        if (searchParticlePrefab != null)
            Instantiate(searchParticlePrefab, transform.position, Quaternion.identity);
    }
}
