using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class CookingStation : MonoBehaviour, IInteractable
{
    public string stationName = "Cutting Board";
    public IngredientData.ProcessType stationType;
    public float interactRange = 1.5f;

    [Header("UI")]
    public WorldSpaceBar progressBar;
    public Vector3 progressBarOffset = new Vector3(0, 1.2f, 0);
    
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
        
        if (progressBar == null)
        {
            GameObject barGo = new GameObject($"{stationName}_ProgressBar");
            barGo.transform.SetParent(transform);
            barGo.transform.localPosition = progressBarOffset;
            progressBar = barGo.AddComponent<WorldSpaceBar>();
        }
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
        
        if (ingredient.requiredProcess != stationType)
        {
            Debug.Log($"[Cooking] {ingredient.ingredientName} cannot be processed at {stationName} (Needs {ingredient.requiredProcess})");
            return false;
        }

        currentIngredient  = ingredient;
        _ingredientHidden  = false;
        // Use ingredient's own timing, NOT the current recipe (which could change mid-cook).
        _cookDuration      = ingredient.cookedVersion != null ? 8f : 5f;

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
            // Guard: player may have picked up the ingredient during cooking
            if (currentIngredient == null)
            {
                _isCooking = false;
                if (progressBar != null) progressBar.UpdateProgress(0f);
                yield break;
            }

            if (!_ingredientHidden && currentIngredient.IsSpoiled)
            {
                if (_inspector != null && _inspector.IsWatchingStation(this))
                    _inspector.AddSuspicion(15f * Time.deltaTime);
            }
            _cookTimer -= Time.deltaTime;

            if (progressBar != null)
            {
                progressBar.UpdateProgress(CookProgress);
            }

            yield return null;
        }

        _isCooking = false;

        // Guard: ingredient could have been removed on the very last frame
        if (currentIngredient == null)
        {
            if (progressBar != null) progressBar.UpdateProgress(0f);
            yield break;
        }

        if (currentIngredient.cookedVersion != null)
            currentIngredient = currentIngredient.cookedVersion;

        if (progressBar != null) progressBar.UpdateProgress(0f);

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
