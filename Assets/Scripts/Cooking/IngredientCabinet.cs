using UnityEngine;

/// <summary>
/// Шкафчик/холодильник: игрок жмёт E → открывается меню выбора ингредиента из инвентаря.
/// Повторное нажатие E (или Esc / клик вне меню) — закрывает меню.
/// </summary>
public class IngredientCabinet : MonoBehaviour, IInteractable
{
    [Header("Visual")]
    [Tooltip("Иконка на дверце шкафа (опционально).")]
    public SpriteRenderer previewRenderer;

    void Start()
    {
        // Авто-создаём синглтон UI, если ещё не существует на сцене
        if (IngredientSelectUI.Instance == null)
        {
            var go = new GameObject("IngredientSelectUI");
            go.AddComponent<IngredientSelectUI>();
        }
    }

    public void Interact(PlayerController player)
    {
        // Нельзя открыть меню с занятыми руками
        if (player.HeldIngredient != null || player.HeldPlate != null)
        {
            Debug.Log("[Cabinet] Руки заняты — сначала освободи.");
            return;
        }

        // Повторное нажатие E — закрывает меню
        IngredientSelectUI.Instance?.Toggle(player);
        AudioManager.Instance?.PlayAt("cabinet_open", transform.position);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 0.4f, 0.4f);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.8f);
    }
}
