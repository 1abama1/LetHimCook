using UnityEngine;

/// <summary>
/// Обычный стол. Игрок может оставить/забрать ингредиент ИЛИ тарелку.
/// Только одна «вещь» лежит одновременно.
/// </summary>
public class Countertop : MonoBehaviour, IInteractable
{
    [Header("Slots")]
    public IngredientData storedIngredient;
    public Plate          storedPlate;

    [Header("Visual")]
    public Transform itemAnchor;            // позиция где показываем предмет
    public GameObject ingredientIconPrefab;

    private GameObject _currentVisual;

    public void Interact(PlayerController player)
    {
        // ── ИГРОК ДЕРЖИТ ТАРЕЛКУ ───────────────────────────
        if (player.HeldPlate != null)
        {
            if (storedIngredient != null)
            {
                // Добавить лежащий ингредиент на тарелку
                if (player.HeldPlate.TryAddIngredient(storedIngredient))
                {
                    storedIngredient = null;
                    RefreshVisual();
                }
            }
            else if (storedPlate == null)
            {
                // Поставить тарелку на стол
                Plate plate = player.TakePlate();
                PlacePlate(plate);
            }
            return;
        }

        // ── ИГРОК ДЕРЖИТ ИНГРЕДИЕНТ ────────────────────────
        if (player.HeldIngredient != null)
        {
            if (storedIngredient == null && storedPlate == null)
            {
                storedIngredient = player.TakeIngredient();
                RefreshVisual();
            }
            return;
        }

        // ── ИГРОК С ПУСТЫМИ РУКАМИ ────────────────────────
        if (storedPlate != null)
        {
            Plate plate = storedPlate;
            storedPlate = null;
            RefreshVisual();
            player.GivePlate(plate);
            return;
        }

        if (storedIngredient != null)
        {
            player.GiveIngredient(storedIngredient);
            storedIngredient = null;
            RefreshVisual();
        }
    }

    void PlacePlate(Plate plate)
    {
        storedPlate = plate;
        if (plate != null && itemAnchor != null)
        {
            plate.transform.SetParent(itemAnchor);
            plate.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        RefreshVisual();
    }

    void RefreshVisual()
    {
        if (_currentVisual) Destroy(_currentVisual);

        if (storedIngredient != null && ingredientIconPrefab != null && itemAnchor != null)
        {
            _currentVisual = Instantiate(ingredientIconPrefab, itemAnchor.position,
                                         Quaternion.identity, itemAnchor);
            var sr = _currentVisual.GetComponent<SpriteRenderer>();
            if (sr != null && storedIngredient.sprite != null)
                sr.sprite = storedIngredient.sprite;
        }
        // тарелка — GameObject самой тарелки и так виден
    }
}
