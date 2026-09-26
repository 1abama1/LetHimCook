using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Станция обработки (нарезка / жарка / варка).
/// Игрок кладёт ингредиент → идёт автоматический кук → забирает.
/// Если держит тарелку → готовый ингредиент ложится прямо на неё.
/// </summary>
public class CookingStation : MonoBehaviour, IInteractable
{
    [Header("Station")]
    public string stationName = "Cutting Board";
    public IngredientData.ProcessType stationType;

    [Header("UI")]
    public WorldSpaceBar progressBar;
    public Vector3       progressBarOffset = new Vector3(0, 1.2f, 0);

    [Header("Events")]
    public UnityEvent<IngredientData> onIngredientPlaced;
    public UnityEvent<IngredientData> onIngredientReady;
    public UnityEvent                 onIngredientTaken;

    // ── State ──────────────────────────────────────────────
    public IngredientData CurrentIngredient { get; private set; }
    public bool           IsCooking         { get; private set; }
    public bool           IsReady           { get; private set; }
    public float          CookProgress      => IsCooking ? 1f - (_timer / _duration) : (IsReady ? 1f : 0f);

    [Header("Visuals")]
    public Transform itemAnchor;
    public GameObject ingredientIconPrefab;

    private GameObject _currentVisual;

    private static MaterialPropertyBlock _propBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private float _timer;
    private float _duration;
    private bool  _ingredientHidden;
    private InspectorAI _inspector;

    // ── Init ───────────────────────────────────────────────
    void Start()
    {
        _inspector = FindFirstObjectByType<InspectorAI>();

        if (progressBar == null)
        {
            var go = new GameObject($"{stationName}_Bar");
            go.transform.position = transform.position + new Vector3(0, 1.5f, 0);
            go.transform.SetParent(transform, true);
            progressBar = go.AddComponent<WorldSpaceBar>();
        }
        else
        {
            progressBar.transform.SetParent(null, true);
            progressBar.transform.position = transform.position + new Vector3(0, 1.5f, 0);
            progressBar.transform.SetParent(transform, true);
        }

        RefreshVisual();
    }

    // ── IInteractable ──────────────────────────────────────
    public void Interact(PlayerController player)
    {
        // ── Игрок держит тарелку ──────────────────────────
        if (player.HeldPlate != null)
        {
            if (IsReady && CurrentIngredient != null)
            {
                if (player.HeldPlate.TryAddIngredient(CurrentIngredient))
                {
                    CurrentIngredient = null;
                    IsReady = false;
                    ResetBar();
                    RefreshVisual();
                    onIngredientTaken?.Invoke();
                }
            }
            return;
        }

        // ── Игрок держит ингредиент ───────────────────────
        if (player.HeldIngredient != null)
        {
            if (!IsCooking && CurrentIngredient == null)
                PlaceIngredient(player.TakeIngredient());
            return;
        }

        // ── Пустые руки ───────────────────────────────────
        if (IsReady && CurrentIngredient != null)
        {
            player.GiveIngredient(CurrentIngredient);
            CurrentIngredient = null;
            IsReady = false;
            ResetBar();
            RefreshVisual();
            onIngredientTaken?.Invoke();
            return;
        }

        // Можно подобрать ещё не готовый ингредиент (отмена готовки)
        if (!IsCooking && CurrentIngredient != null)
        {
            player.GiveIngredient(CurrentIngredient);
            CurrentIngredient = null;
            ResetBar();
            RefreshVisual();
        }
    }

    // ── Placement ─────────────────────────────────────────
    public bool PlaceIngredient(IngredientData ingredient)
    {
        if (IsCooking || ingredient == null) return false;

        if (ingredient.requiredProcess != stationType)
        {
            Debug.Log($"[{stationName}] {ingredient.ingredientName} нужна обработка: {ingredient.requiredProcess}");
            return false;
        }

        CurrentIngredient = ingredient;
        _ingredientHidden = false;
        IsReady           = false;
        _duration         = ingredient.cookedVersion != null ? 8f : 5f;

        RefreshVisual(); // Показываем положенный ингредиент

        Debug.Log($"[{stationName}] Начинаем готовить {ingredient.ingredientName}");
        onIngredientPlaced?.Invoke(ingredient);
        AudioManager.Instance?.Play("cook_start");
        StartCoroutine(CookRoutine());
        return true;
    }

    // ── Cook loop ─────────────────────────────────────────
    IEnumerator CookRoutine()
    {
        IsCooking = true;
        _timer    = _duration;

        while (_timer > 0f)
        {
            if (CurrentIngredient == null) { StopCooking(); yield break; }

            if (!_ingredientHidden && CurrentIngredient.IsSpoiled)
                if (_inspector != null && _inspector.IsWatchingStation(this))
                    _inspector.AddSuspicion(15f * Time.deltaTime);

            _timer -= Time.deltaTime;
            progressBar?.UpdateProgress(CookProgress);
            yield return null;
        }

        IsCooking = false;
        if (CurrentIngredient == null) { ResetBar(); yield break; }

        // Трансформация
        if (CurrentIngredient.cookedVersion != null)
        {
            CurrentIngredient = CurrentIngredient.cookedVersion;
            RefreshVisual(); // Обновляем визуал на приготовленный!
        }

        IsReady = true;
        progressBar?.UpdateProgress(1f);
        Debug.Log($"[{stationName}] {CurrentIngredient.ingredientName} готово!");
        onIngredientReady?.Invoke(CurrentIngredient);
        AudioManager.Instance?.Play("cook_done");
    }

    public void ResetStation()
    {
        CurrentIngredient = null;
        IsReady = false;
        IsCooking = false;
        _ingredientHidden = false;
        ResetBar();
        RefreshVisual();
        onIngredientTaken?.Invoke();
    }

    void StopCooking() { IsCooking = false; ResetBar(); }
    void ResetBar()    { progressBar?.UpdateProgress(0f); }

    // ── Visuals ────────────────────────────────────────────
    void RefreshVisual()
    {
        if (_currentVisual) Destroy(_currentVisual);

        if (CurrentIngredient != null)
        {
            Transform anchor = itemAnchor != null ? itemAnchor : transform;
            
            GameObject prefab = CurrentIngredient.modelPrefab != null
                ? CurrentIngredient.modelPrefab
                : ingredientIconPrefab;

            if (prefab != null)
            {
                _currentVisual = Instantiate(prefab, anchor);
                
                Vector3 baseOffset = itemAnchor == null ? new Vector3(0, 0.8f, 0) : Vector3.zero;
                
                _currentVisual.transform.localPosition = baseOffset + CurrentIngredient.countertopOffset;
                _currentVisual.transform.localRotation = Quaternion.Euler(CurrentIngredient.countertopRotation);
                _currentVisual.transform.localScale    = Vector3.one * (CurrentIngredient.visualScale * 0.01f);

                if (prefab == ingredientIconPrefab)
                {
                    var sr = _currentVisual.GetComponent<SpriteRenderer>();
                    if (sr != null && CurrentIngredient.sprite != null)
                        sr.sprite = CurrentIngredient.sprite;
                }

                if (CurrentIngredient.tintColor != Color.white)
                {
                    if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
                    foreach (var r in _currentVisual.GetComponentsInChildren<Renderer>())
                    {
                        r.GetPropertyBlock(_propBlock);
                        _propBlock.SetColor(BaseColorId, CurrentIngredient.tintColor);
                        _propBlock.SetColor(ColorId, CurrentIngredient.tintColor);
                        r.SetPropertyBlock(_propBlock);
                    }
                }
            }
        }
    }

    // ── Inspector helpers ──────────────────────────────────
    public void HideIngredient()
    {
        if (CurrentIngredient == null || !CurrentIngredient.IsSpoiled) return;
        _ingredientHidden = true;
        if (_currentVisual != null) _currentVisual.SetActive(false);
    }
    public void RevealIngredient() 
    {
        _ingredientHidden = false;
        if (_currentVisual != null) _currentVisual.SetActive(true);
    }
    public bool IsIngredientHidden => _ingredientHidden;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}
