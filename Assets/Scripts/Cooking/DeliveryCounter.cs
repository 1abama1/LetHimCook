using UnityEngine;

/// <summary>
/// Стойка выдачи. Игрок приносит тарелку с блюдом → деньги + очистка тарелки.
/// </summary>
public class DeliveryCounter : MonoBehaviour, IInteractable
{
    [Header("Plate return")]
    public Transform plateReturnAnchor;  // куда вернуть чистую тарелку
    public bool      returnCleanPlate = true;

    public void Interact(PlayerController player)
    {
        if (player.HeldPlate == null)
        {
            Debug.Log("[Delivery] Нет тарелки в руках.");
            return;
        }

        Plate plate = player.HeldPlate; // Не забираем сразу, сначала проверим
        
        if (plate.ingredients.Count == 0)
        {
            Debug.Log("[Delivery] Тарелка пустая.");
            return;
        }

        RecipeData matched = RecipeManager.Instance.CheckAndServeDish(
            plate.ingredients, out bool hasSpoiled);

        if (matched != null)
        {
            // Забираем тарелку у игрока теперь, когда блюдо принято
            player.TakePlate();

            AudioManager.Instance?.Play("order_success");

            // Уведомляем клиента
            ClientNPC[] clients = FindObjectsByType<ClientNPC>(FindObjectsSortMode.None);
            foreach (ClientNPC c in clients)
            {
                if (c.WantedRecipe == matched) { c.ReceiveDish(matched, hasSpoiled); break; }
            }

            // Очистить и вернуть тарелку
            plate.Clear();
            if (returnCleanPlate && plateReturnAnchor != null)
            {
                plate.transform.SetParent(null);
                plate.transform.SetPositionAndRotation(plateReturnAnchor.position, Quaternion.identity);
            }
            else
            {
                // Если не возвращаем на якорь, может быть просто уничтожить или дать игроку пустую?
                // В Overcooked тарелки обычно возвращаются на грязную кучу или в мойку.
                // Здесь просто очистим и отдадим игроку? Нет, лучше на якорь.
            }

            Debug.Log($"[Delivery] Блюдо «{matched.recipeName}» сдано! Порченые: {hasSpoiled}");
        }
        else
        {
            AudioManager.Instance?.Play("order_fail");
            Debug.Log("[Delivery] Блюдо не совпадает с заказом.");
        }
    }
}
