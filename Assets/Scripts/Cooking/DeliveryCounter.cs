using UnityEngine;

public class DeliveryCounter : MonoBehaviour, IInteractable
{
    public AssemblyStation plateToServe;

    public void Interact(PlayerController player)
    {
        if (plateToServe == null || plateToServe.ingredientsOnPlate.Count == 0) return;

        RecipeData matched = RecipeManager.Instance.CheckAndServeDish(
            plateToServe.ingredientsOnPlate, out bool hasSpoiled);

        if (matched != null)
        {
            plateToServe.ingredientsOnPlate.Clear();
            AudioManager.Instance?.Play("order_success");

            // Notify the ClientNPC that wanted this exact recipe
            ClientNPC[] clients = FindObjectsByType<ClientNPC>(FindObjectsSortMode.None);
            foreach (ClientNPC c in clients)
            {
                if (c.WantedRecipe == matched)
                {
                    c.ReceiveDish(matched, hasSpoiled);
                    break;
                }
            }
        }
        else
        {
            AudioManager.Instance?.Play("order_fail");
        }
    }
}
