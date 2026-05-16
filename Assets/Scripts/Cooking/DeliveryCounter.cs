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

        Plate plate = player.HeldPlate;
        
        if (plate.ingredients.Count == 0)
        {
            Debug.Log("[Delivery] Тарелка пустая.");
            return;
        }

        if (RecipeManager.Instance == null)
        {
            Debug.LogError("[Delivery] RecipeManager.Instance не найден! Добавь объект RecipeManager на сцену.");
            return;
        }

        RecipeData matched = RecipeManager.Instance.CheckAndServeDish(
            plate.ingredients, out bool hasSpoiled);

        if (matched != null)
        {
            // Забираем тарелку у игрока
            player.TakePlate();

            AudioManager.Instance?.Play("order_success");

            // Уведомляем клиента
            bool clientFound = false;
            foreach (ClientNPC c in ClientNPC.ActiveClients)
            {
                // ВАЖНО: Ищем только того, кто УЖЕ подошел к окну и хочет именно этот рецепт
                if (c.IsAtWindow && c.WantedRecipe == matched) 
                { 
                    c.ReceiveDish(matched, hasSpoiled); 
                    clientFound = true;
                    break; 
                }
            }

            if (!clientFound)
            {
                Debug.LogWarning("[Delivery] Блюдо совпало с текущим заказом, но клиент у окна почему-то не принял его (или он еще не подошел).");
            }

            // Очистить и вернуть тарелку
            plate.Clear();
            if (returnCleanPlate && plateReturnAnchor != null)
            {
                plate.transform.SetParent(null);
                plate.transform.SetPositionAndRotation(plateReturnAnchor.position, plateReturnAnchor.rotation);
            }
            else
            {
                Debug.LogWarning("[Delivery] Тарелка очищена, но plateReturnAnchor не назначен. Тарелка осталась в мировых координатах.");
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
