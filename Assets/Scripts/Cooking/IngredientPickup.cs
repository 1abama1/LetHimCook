using UnityEngine;

public class IngredientPickup : MonoBehaviour, IInteractable
{
    public IngredientData ingredientData;

    public void Interact(PlayerController player)
    {
        if (player.HeldIngredient != null) return;
        player.GiveIngredient(ingredientData);
        Debug.Log($"[Pickup] Player picked up {ingredientData.ingredientName} from the world.");
        gameObject.SetActive(false);
    }
}
