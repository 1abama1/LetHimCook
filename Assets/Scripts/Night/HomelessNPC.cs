using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public enum HomelessState { Idle, Alert, Dead }

public class HomelessNPC : MonoBehaviour, IInteractable
{
    public int spoiledIngredientLoot = 2;
    public int junkLootAmount        = 1;
    public int moneyLoot             = 0;

    public string negotiateDialogueId = "homeless_negotiate";
    public string aggressiveDialogueId = "homeless_hostile";

    [Range(0f, 1f)]
    public float stealDetectionChance = 0.4f;

    public HomelessState state = HomelessState.Idle;

    public UnityEvent                  onLootStolen;
    public UnityEvent                  onNegotiateSuccess;
    public UnityEvent<HomelessNPC>     onMurdered;
    public UnityEvent                  onAlarmed;

    private bool _lootTaken;

    public void Interact(PlayerController player)
    {
        if (state == HomelessState.Dead || _lootTaken) return;

        if (player.IsSneaking)
            AttemptSteal();
        else
            DialogueSystem.Instance?.StartDialogue(negotiateDialogueId, this, player);
    }

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
            GiveLoot();
            onLootStolen?.Invoke();
        }
    }

    public void OnNegotiateSuccess()
    {
        GiveLoot();
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
        GetComponent<Animator>()?.SetTrigger("Die");
        yield return new WaitForSeconds(2f);
        gameObject.SetActive(false);
    }

    void GiveLoot(bool bonus = false)
    {
        if (_lootTaken && !bonus) return;
        _lootTaken = true;

        PlayerEconomy.Instance?.AddResource(ResourceType.SpoiledIngredient,
            spoiledIngredientLoot + (bonus ? 1 : 0));
        PlayerEconomy.Instance?.AddResource(ResourceType.JunkLoot,
            junkLootAmount + (bonus ? 2 : 0));
        if (moneyLoot > 0) PlayerEconomy.Instance?.EarnMoney(moneyLoot);
    }
}
