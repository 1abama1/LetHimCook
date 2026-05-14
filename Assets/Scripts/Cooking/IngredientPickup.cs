using UnityEngine;

public class IngredientPickup : MonoBehaviour, IInteractable
{
    public IngredientData ingredientData;

    public void Interact(PlayerController player)
    {
        if (player.HeldIngredient != null) return;
        player.GiveIngredient(ingredientData);
        gameObject.SetActive(false);
    }
}
