using UnityEngine;
using UnityEngine.Events;

public class RestaurantReputation : MonoBehaviour
{
    public static RestaurantReputation Instance { get; private set; }

    [Range(0, 100)]
    public float reputation = 50f;

    [Header("Events")]
    public UnityEvent<float> onReputationChanged;
    public UnityEvent onGameOver;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onArrest.AddListener(() => ChangeReputation(-20f));
            GameManager.Instance.onInspectorFine.AddListener(() => ChangeReputation(-10f));
        }
    }

    public void ChangeReputation(float amount)
    {
        reputation = Mathf.Clamp(reputation + amount, 0, 100);
        onReputationChanged?.Invoke(reputation);

        if (reputation <= 0)
        {
            TriggerGameOver();
        }
    }

    void TriggerGameOver()
    {
        Debug.Log("Game Over! Репутация упала до 0.");
        onGameOver?.Invoke();
        // Здесь можно добавить вызов UI-экрана Game Over
    }
}
