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
        if (ingredientIconPrefab == null || ingredientSlotRoot == null) return;

        Vector3 offset = Vector3.up * 0.08f * _icons.Count;
        GameObject go = Instantiate(ingredientIconPrefab,
            ingredientSlotRoot.position + offset, Quaternion.identity, ingredientSlotRoot);

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && ing.sprite != null) sr.sprite = ing.sprite;

        _icons.Add(go);
    }
}
