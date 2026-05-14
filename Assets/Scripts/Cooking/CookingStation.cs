using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class CookingStation : MonoBehaviour, IInteractable
{
    public string stationName = "Cutting Board";
    public float interactRange = 1.5f;

    public IngredientData currentIngredient;
    private bool _isCooking;
    private float _cookTimer;
    private float _cookDuration;

    public UnityEvent<IngredientData> onIngredientPlaced;
    public UnityEvent<IngredientData> onIngredientReady;
    public UnityEvent onIngredientHidden;

    private bool _ingredientHidden;
    private InspectorAI _inspector;

    void Start()
    {
        _inspector = FindFirstObjectByType<InspectorAI>();
    }

    public void Interact(PlayerController player)
    {
        if (player.HeldIngredient != null)
        {
            PlaceIngredient(player.TakeIngredient());
        }
        else if (currentIngredient != null && !_isCooking)
        {
            player.GiveIngredient(currentIngredient);
            Debug.Log($"[Cooking] Player picked up {currentIngredient.ingredientName} from {stationName}");
            currentIngredient = null;
        }
    }

    public bool PlaceIngredient(IngredientData ingredient)
    {
        if (_isCooking || ingredient == null) return false;

        currentIngredient  = ingredient;
        _ingredientHidden  = false;
        _cookDuration      = RecipeManager.Instance?.currentRecipe?.cookTimeSeconds ?? 8f;

        Debug.Log($"[Cooking] {ingredient.ingredientName} placed on {stationName}. Cooking started!");
        onIngredientPlaced?.Invoke(ingredient);
        AudioManager.Instance?.Play("cook_start");
        StartCoroutine(CookRoutine());
        return true;
    }

    IEnumerator CookRoutine()
    {
        _isCooking  = true;
        _cookTimer  = _cookDuration;

        while (_cookTimer > 0)
        {
            if (!_ingredientHidden && currentIngredient != null && currentIngredient.IsSpoiled)
            {
                if (_inspector != null && _inspector.IsWatchingStation(this))
                    _inspector.AddSuspicion(15f * Time.deltaTime);
            }
            _cookTimer -= Time.deltaTime;
            yield return null;
        }

        _isCooking = false;
        Debug.Log($"[Cooking] {currentIngredient.ingredientName} is READY on {stationName}!");
        onIngredientReady?.Invoke(currentIngredient);
        AudioManager.Instance?.Play("cook_done");
    }

    public void HideIngredient()
    {
        if (currentIngredient == null || !currentIngredient.IsSpoiled) return;
        _ingredientHidden = true;
        Debug.Log($"[Cooking] Hid spoiled {currentIngredient.ingredientName} from Inspector!");
        onIngredientHidden?.Invoke();
    }

    public void RevealIngredient() => _ingredientHidden = false;

    public bool IsIngredientHidden => _ingredientHidden;
    public bool IsCooking          => _isCooking;
    public float CookProgress      => _isCooking ? 1f - (_cookTimer / _cookDuration) : 0f;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
