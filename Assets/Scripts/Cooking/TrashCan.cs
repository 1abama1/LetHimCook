using UnityEngine;
using System.Collections;

/// <summary>
/// Мусорка в стиле Overcooked.
/// • Игрок держит ингредиент → выбрасывает его.
/// • Игрок держит тарелку   → очищает тарелку (ингредиенты пропадают, тарелка остаётся).
/// Опционально: анимация крышки и частицы выброса.
/// </summary>
public class TrashCan : MonoBehaviour, IInteractable
{
    [Header("Visual / FX")]
    [Tooltip("Transform крышки мусорки. Если задан — крышка откроется при взаимодействии.")]
    public Transform lid;

    [Tooltip("Угол открытия крышки по оси X.")]
    public float lidOpenAngle = -70f;

    [Tooltip("Скорость анимации крышки.")]
    public float lidSpeed = 8f;

    [Tooltip("Префаб частиц выброса (опционально).")]
    public GameObject trashFxPrefab;

    [Tooltip("Точка появления частиц (опционально, иначе позиция мусорки).")]
    public Transform fxPoint;

    [Header("Audio")]
    [Tooltip("Ключ звука в AudioManager при выбросе.")]
    public string throwSfx = "trash_throw";

    // ── Состояние крышки ──────────────────────────────────────────
    private Quaternion _lidClosedRot;
    private Quaternion _lidOpenRot;
    private Coroutine  _lidRoutine;

    // ── Unity ─────────────────────────────────────────────────────
    void Start()
    {
        if (lid != null)
        {
            _lidClosedRot = lid.localRotation;
            _lidOpenRot   = Quaternion.Euler(lidOpenAngle, 0f, 0f) * _lidClosedRot;
        }
    }

    // ── IInteractable ──────────────────────────────────────────────
    public void Interact(PlayerController player)
    {
        bool didTrash = false;

        // 1. Выбросить ингредиент из рук
        if (player.HeldIngredient != null)
        {
            IngredientData ing = player.TakeIngredient();
            Debug.Log($"[TrashCan] Выброшен ингредиент: {ing.ingredientName}");
            didTrash = true;
        }
        // 2. Очистить тарелку (ингредиенты — в мусор, тарелка остаётся в руках)
        else if (player.HeldPlate != null)
        {
            Plate plate = player.HeldPlate;
            if (plate.ingredients.Count > 0)
            {
                string contents = string.Join(", ", plate.ingredients.ConvertAll(i => i.ingredientName));
                plate.Clear();
                Debug.Log($"[TrashCan] Тарелка очищена: [{contents}]");
                didTrash = true;
            }
            else
            {
                Debug.Log("[TrashCan] Тарелка уже пуста — нечего выбрасывать.");
            }
        }
        else
        {
            Debug.Log("[TrashCan] Руки пусты — нечего выбрасывать.");
        }

        if (didTrash)
        {
            PlayFX();
            OpenLid();
        }
    }

    // ── Эффекты ────────────────────────────────────────────────────
    private void PlayFX()
    {
        AudioManager.Instance?.PlayAt(throwSfx, transform.position);

        if (trashFxPrefab != null)
        {
            Vector3 pos = fxPoint != null ? fxPoint.position : transform.position + Vector3.up * 0.5f;
            GameObject fx = Instantiate(trashFxPrefab, pos, Quaternion.identity);
            Destroy(fx, 2f);
        }
    }

    private void OpenLid()
    {
        if (lid == null) return;
        if (_lidRoutine != null) StopCoroutine(_lidRoutine);
        _lidRoutine = StartCoroutine(LidRoutine());
    }

    private IEnumerator LidRoutine()
    {
        // Открыть
        while (Quaternion.Angle(lid.localRotation, _lidOpenRot) > 0.5f)
        {
            lid.localRotation = Quaternion.Slerp(lid.localRotation, _lidOpenRot, lidSpeed * Time.deltaTime);
            yield return null;
        }
        lid.localRotation = _lidOpenRot;

        yield return new WaitForSeconds(0.6f);

        // Закрыть
        while (Quaternion.Angle(lid.localRotation, _lidClosedRot) > 0.5f)
        {
            lid.localRotation = Quaternion.Slerp(lid.localRotation, _lidClosedRot, lidSpeed * Time.deltaTime);
            yield return null;
        }
        lid.localRotation = _lidClosedRot;
    }

    // ── Gizmo ──────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.3f, 0.1f, 0.6f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(0.6f, 1f, 0.6f));
    }
}
