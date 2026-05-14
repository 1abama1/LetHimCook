using UnityEngine;

public class DeliveryCounter : MonoBehaviour, IInteractable
{
    public AssemblyStation plateToServe;

    public void Interact(PlayerController player)
    {
        if (plateToServe != null && plateToServe.ingredientsOnPlate.Count > 0)
        {
            bool success = RecipeManager.Instance.CheckAndServeDish(plateToServe.ingredientsOnPlate);

            if (success)
            {
                plateToServe.ingredientsOnPlate.Clear();
                AudioManager.Instance?.Play("order_success");
            }
            else
            {
                AudioManager.Instance?.Play("order_fail");
            }
        }
    }
}
