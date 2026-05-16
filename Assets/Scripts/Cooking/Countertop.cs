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

    void Start()
    {
        // Если тарелка назначена в инспекторе как префаб — создаем инстанс
        if (storedPlate != null && !storedPlate.gameObject.scene.IsValid())
        {
            Plate instance = Instantiate(storedPlate);
            PlacePlate(instance);
        }
        else if (storedPlate != null)
        {
            // Если это уже инстанс на сцене — просто привязываем
            PlacePlate(storedPlate);
        }

        RefreshVisual();
    }

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
            // ПРИОРИТЕТ: Попробовать положить в тарелку, которая стоит на столе
            if (storedPlate != null)
            {
                if (storedPlate.TryAddIngredient(player.HeldIngredient))
                {
                    player.TakeIngredient();
                    return;
                }
            }

            // Иначе положить просто на стол (если он пуст)
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

        if (storedIngredient != null && itemAnchor != null)
        {
            // Приоритет: собственная модель ингредиента → общий иконка-префаб
            GameObject prefab = storedIngredient.modelPrefab != null
                ? storedIngredient.modelPrefab
                : ingredientIconPrefab;

            if (prefab != null)
            {
                _currentVisual = Instantiate(prefab, itemAnchor);
                _currentVisual.transform.localPosition = storedIngredient.countertopOffset;
                _currentVisual.transform.localRotation = Quaternion.Euler(storedIngredient.countertopRotation);
                // Делим на 100 для коррекции масштаба моделей
                _currentVisual.transform.localScale    = Vector3.one * (storedIngredient.visualScale * 0.01f);

                // Спрайт применяем только для иконка-префаба
                if (prefab == ingredientIconPrefab)
                {
                    var sr = _currentVisual.GetComponent<SpriteRenderer>();
                    if (sr != null && storedIngredient.sprite != null)
                        sr.sprite = storedIngredient.sprite;
                }

                // Применяем tintColor
                if (storedIngredient.tintColor != Color.white)
                {
                    foreach (var r in _currentVisual.GetComponentsInChildren<Renderer>())
                        r.material.color = storedIngredient.tintColor;
                }
            }
        }
        // тарелка — GameObject самой тарелки и так виден
    }
}
