using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public enum HomelessState { Idle, Alert, Dead }

public class HomelessNPC : MonoBehaviour, IInteractable
{
    [Header("Loot")]
    [Tooltip("Конкретные IngredientData, которые бомж несёт при себе. " +
             "Перетащите нужные ScriptableObject из проекта.")]
    public List<IngredientData> ingredientLoot = new List<IngredientData>();

    [Tooltip("Бонусные ингредиенты при убийстве (дополняют ingredientLoot).")]
    public List<IngredientData> bonusKillLoot  = new List<IngredientData>();

    public int junkLootAmount = 1;
    public int moneyLoot      = 0;

    [Header("Dialogue")]
    public string negotiateDialogueId  = "homeless_negotiate";
    public string aggressiveDialogueId = "homeless_hostile";

    [Range(0f, 1f)]
    public float stealDetectionChance = 0.4f;

    public HomelessState state = HomelessState.Idle;

    public UnityEvent              onLootStolen;
    public UnityEvent              onNegotiateSuccess;
    public UnityEvent<HomelessNPC> onMurdered;
    public UnityEvent              onAlarmed;

    private bool _lootTaken;

    // ── IInteractable ─────────────────────────────────────────────────────────

    public void Interact(PlayerController player)
    {
        if (state == HomelessState.Dead || _lootTaken) return;

        if (player.IsSneaking)
            AttemptSteal();
        else
            DialogueSystem.Instance?.StartDialogue(negotiateDialogueId, this, player);
    }

    // ── Действия ──────────────────────────────────────────────────────────────

    public void AttemptSteal()
    {
        if (_lootTaken) return;
        if (Random.value < stealDetectionChance)
        {
            state = HomelessState.Alert;
            onAlarmed?.Invoke();
            DialogueSystem.Instance?.StartDialogue(aggressiveDialogueId, this, null);
        }
        else
        {
            GiveLoot(bonus: false);
            onLootStolen?.Invoke();
        }
    }

    public void OnNegotiateSuccess()
    {
        GiveLoot(bonus: false);
        onNegotiateSuccess?.Invoke();
    }

    public void OnNegotiateFailed()
    {
        state = HomelessState.Alert;
        onAlarmed?.Invoke();
    }

    public void Kill()
    {
        if (state == HomelessState.Dead) return;
        state = HomelessState.Dead;
        GiveLoot(bonus: true);
        onMurdered?.Invoke(this);
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Die");
        yield return new WaitForSeconds(2f);
        gameObject.SetActive(false);
    }

    // ── Выдача лута ───────────────────────────────────────────────────────────

    void GiveLoot(bool bonus)
    {
        if (_lootTaken && !bonus) return;
        _lootTaken = true;

        // Основные ингредиенты
        foreach (var ing in ingredientLoot)
            if (ing != null) PlayerEconomy.Instance?.AddIngredient(ing);

        // Бонус при убийстве
        if (bonus)
        {
            foreach (var ing in bonusKillLoot)
                if (ing != null) PlayerEconomy.Instance?.AddIngredient(ing);
        }

        // Хлам и деньги
        if (junkLootAmount > 0)
            PlayerEconomy.Instance?.AddResource(ResourceType.JunkLoot,
                junkLootAmount + (bonus ? 2 : 0));

        int totalMoney = moneyLoot;
        if (totalMoney > 0) PlayerEconomy.Instance?.EarnMoney(totalMoney);

        Debug.Log($"[HomelessNPC] Выдан лут. Бонус убийства: {bonus}");
    }
}
