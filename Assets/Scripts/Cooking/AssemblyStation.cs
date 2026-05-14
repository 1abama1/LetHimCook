using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AssemblyStation : MonoBehaviour, IInteractable
{
    public string stationName = "Plate / Assembly";
    
    public List<IngredientData> ingredientsOnPlate = new List<IngredientData>();

    public void Interact(PlayerController player)
    {
        if (player.HeldIngredient != null)
        {
            ingredientsOnPlate.Add(player.TakeIngredient());
            Debug.Log($"Положили {ingredientsOnPlate.Last().ingredientName} на {stationName}");
        }
        else if (ingredientsOnPlate.Count > 0)
        {
            Debug.Log("Чтобы сдать блюдо, отнесите тарелку на стол выдачи!");
        }
    }
}
