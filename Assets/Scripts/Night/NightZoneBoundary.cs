using UnityEngine;

/// <summary>
/// Граница ночной зоны. Если игрок покидает зону — получает штраф
/// или телепортируется обратно. Привяжите к Trigger-коллайдеру.
/// </summary>
[RequireComponent(typeof(Collider))]
public class NightZoneBoundary : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Если true — мгновенно возвращает игрока. Если false — штрафует деньги.")]
    public bool teleportBack     = true;
    public int  moneyPenalty     = 20;
    public Vector3 spawnBackPosition = Vector3.zero;

    [Tooltip("Сообщение в HUD при выходе из зоны")]
    public string warningMessage = "Нельзя уходить так далеко ночью!";

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance?.currentPhase != GamePhase.Night) return;

        Debug.Log($"[NightZone] Player left night zone!");

        if (teleportBack)
        {
            other.transform.position = spawnBackPosition;
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }
        else
        {
            PlayerEconomy.Instance?.SpendMoney(moneyPenalty);
        }

        AudioManager.Instance?.Play("warning");
        // Здесь можно дополнительно вызвать всплывающее сообщение через GameHUD
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.15f);
        Collider col = GetComponent<Collider>();
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
