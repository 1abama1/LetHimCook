using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Станция сборки (тарелка на столе).
/// Игрок кладёт тарелку → добавляет ингредиенты → забирает тарелку на выдачу.
/// Если тарелки нет — принять тарелку от игрока.
/// </summary>
public class AssemblyStation : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public string stationName = "Assembly Table";

    [Header("References")]
    public Transform plateAnchor;           // куда встаёт тарелка визуально
    public GameObject defaultPlatePrefab;   // если игрок подходит без тарелки — заспавнить

    public Plate PlateOnStation { get; private set; }

    // ── IInteractable ──────────────────────────────────────
    public void Interact(PlayerController player)
    {
        // ── Игрок держит тарелку ──────────────────────────
        if (player.HeldPlate != null)
        {
            if (PlateOnStation == null)
            {
                // Поставить тарелку на станцию
                SetPlate(player.TakePlate());
            }
            // Уже стоит — ничего не делаем (не перекладываем)
            return;
        }

        // ── Игрок держит ингредиент ───────────────────────
        if (player.HeldIngredient != null)
        {
            if (PlateOnStation != null)
            {
                PlateOnStation.TryAddIngredient(player.TakeIngredient());
            }
            else
            {
                Debug.Log("[Assembly] Сначала поставь тарелку!");
            }
            return;
        }

        // ── Пустые руки ───────────────────────────────────
        if (PlateOnStation != null)
        {
            // Забрать тарелку (нести на DeliveryCounter)
            Plate plate = PlateOnStation;
            PlateOnStation = null;
            player.GivePlate(plate);
            Debug.Log("[Assembly] Игрок забрал тарелку для подачи.");
            return;
        }

        // Дать чистую тарелку (если заспавнена)
        SpawnAndGivePlate(player);
    }

    void SetPlate(Plate plate)
    {
        PlateOnStation = plate;
        if (plate != null && plateAnchor != null)
        {
            plate.transform.SetParent(plateAnchor);
            plate.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        Debug.Log($"[Assembly] Тарелка установлена на {stationName}.");
    }

    void SpawnAndGivePlate(PlayerController player)
    {
        if (defaultPlatePrefab == null) return;
        GameObject go = Instantiate(defaultPlatePrefab,
                            plateAnchor != null ? plateAnchor.position : transform.position,
                            Quaternion.identity);
        Plate plate = go.GetComponent<Plate>();
        if (plate != null) player.GivePlate(plate);
    }
}
