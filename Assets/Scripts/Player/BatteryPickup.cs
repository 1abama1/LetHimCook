using UnityEngine;

/// <summary>
/// Батарейка — подбирается через IInteractable.
/// Разместите на объекте с коллайдером в ночной сцене.
/// </summary>
public class BatteryPickup : MonoBehaviour, IInteractable
{
    [Tooltip("Сколько % заряда восстанавливает батарейка (0–100)")]
    public float chargeAmount = 40f;

    [Tooltip("Анимация / частицы при подборе")]
    public GameObject pickupFX;

    public void Interact(PlayerController player)
    {
        FlashlightController flashlight = player.GetComponent<FlashlightController>();
        if (flashlight == null) return;

        flashlight.AddBattery(chargeAmount);

        if (pickupFX != null)
            Instantiate(pickupFX, transform.position, Quaternion.identity);

        gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
    }
}
