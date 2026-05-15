using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class Plate : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public int maxIngredients = 4;

    [Header("Visual")]
    public Transform ingredientSlotRoot;   // пустой Transform над тарелкой
    public GameObject ingredientIconPrefab; // мировой спрайт-иконка ингредиента

    public List<IngredientData> ingredients = new List<IngredientData>();

    private List<GameObject> _icons = new List<GameObject>();

    public UnityEvent<IngredientData> onIngredientAdded;
    public UnityEvent onPlateCleared;
    public UnityEvent<Plate> onPickedUp;
    public UnityEvent<Plate> onPutDown;

    // ── IInteractable ──────────────────────────────────────
    public void Interact(PlayerController player)
    {
        // Если игрок держит ингредиент — положить на тарелку
        if (player.HeldIngredient != null)
        {
            TryAddIngredient(player.TakeIngredient());
            return;
        }

        // Если игрок держит тарелку — положить текущую обратно (swap не нужен)
        if (player.HeldPlate != null) return;

        // Поднять тарелку
        player.GivePlate(this);
        onPickedUp?.Invoke(this);
    }

    // ── API ────────────────────────────────────────────────
    public bool TryAddIngredient(IngredientData ing)
    {
        if (ing == null || ingredients.Count >= maxIngredients) return false;
        ingredients.Add(ing);
        SpawnIcon(ing);
        onIngredientAdded?.Invoke(ing);
        Debug.Log($"[Plate] + {ing.ingredientName}  ({ingredients.Count}/{maxIngredients})");
        return true;
    }

    public void Clear()
    {
        ingredients.Clear();
        foreach (var go in _icons) if (go) Destroy(go);
        _icons.Clear();
        onPlateCleared?.Invoke();
    }

    // Вызывается, когда игрок кладёт тарелку на стол / стойку
    public void PlaceOnSurface(PlayerController player)
    {
        onPutDown?.Invoke(this);
    }

    // ── Иконки ────────────────────────────────────────────
    void SpawnIcon(IngredientData ing)
    {
        // Фолбек: если ingredientSlotRoot не назначен — кладём прямо на тарелку
        Transform root = ingredientSlotRoot != null ? ingredientSlotRoot : transform;

        // Приоритет: собственная модель ингредиента → общий иконка-префаб
        GameObject prefab = ing.modelPrefab != null ? ing.modelPrefab : ingredientIconPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"[Plate] SpawnIcon: нет modelPrefab и нет ingredientIconPrefab для {ing.ingredientName}. Назначь одно из двух!");
            return;
        }

        Vector3 offset = Vector3.up * 0.08f * _icons.Count;
        GameObject go = Instantiate(prefab, root.position + offset, Quaternion.identity);
        go.transform.SetParent(root, true);
        go.transform.localPosition += ing.visualOffset;
        go.transform.localRotation *= Quaternion.Euler(ing.visualRotation);

        // Спрайт применяем только для иконка-префаба (не для 3D модели)
        if (prefab == ingredientIconPrefab)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && ing.sprite != null) sr.sprite = ing.sprite;
        }

        // Применяем tintColor если есть Renderer (работает и для 3D и для спрайтов)
        if (ing.tintColor != Color.white)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                r.material.color = ing.tintColor;
        }

        // ОТКЛЮЧАЕМ ФИЗИКУ ДЛЯ МОДЕЛИ НА ТАРЕЛКЕ
        foreach (var col in go.GetComponentsInChildren<Collider>())
            col.enabled = false;
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Debug.Log($"[Plate] SpawnIcon: {ing.ingredientName}, prefab={prefab.name}, root={root.name}");
        _icons.Add(go);
    }
}
