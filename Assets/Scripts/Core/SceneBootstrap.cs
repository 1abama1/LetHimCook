using UnityEngine;

/// <summary>
/// Точка входа сцены. Проверяет наличие всех синглтонов и выводит
/// диагностику в консоль. Добавьте на пустой GameObject "Bootstrap" в сцене.
/// Порядок исполнения: -100 (поставьте в Edit → Project Settings → Script Execution Order).
/// </summary>
public class SceneBootstrap : MonoBehaviour
{
    [Header("Validation (Editor Only)")]
    public bool validateOnStart = true;

    void Awake()
    {
        // Гарантируем что Time.timeScale нормальный после перезагрузки
        Time.timeScale = 1f;
    }

    void Start()
    {
        if (!validateOnStart) return;

#if UNITY_EDITOR
        ValidateSystems();
#endif
    }

    void ValidateSystems()
    {
        bool ok = true;

        ok &= CheckSingleton("GameManager",         GameManager.Instance != null);
        ok &= CheckSingleton("PlayerEconomy",        PlayerEconomy.Instance != null);
        ok &= CheckSingleton("RestaurantReputation", RestaurantReputation.Instance != null);
        ok &= CheckSingleton("AudioManager",         AudioManager.Instance != null);
        ok &= CheckSingleton("RecipeManager",        RecipeManager.Instance != null);
        ok &= CheckSingleton("DialogueSystem",       DialogueSystem.Instance != null);

        // Проверяем игрока
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[Bootstrap] ❌ No GameObject with tag 'Player' found!");
            ok = false;
        }
        else
        {
            CheckComponent<PlayerController>("PlayerController", player);
            CheckComponent<NoiseEmitter>("NoiseEmitter",         player);
            CheckComponent<FootstepSystem>("FootstepSystem",     player);
            CheckComponent<FlashlightController>("FlashlightController", player);
            CheckComponent<MurderSystem>("MurderSystem",         player);
        }

        // Ночная/дневная фаза
        CheckSingleton("NightManager", NightManager.Instance != null);
        CheckSingleton("DayManager",   DayManager.Instance != null);

        if (ok)
            Debug.Log("[Bootstrap] ✅ All systems validated OK.");
        else
            Debug.LogWarning("[Bootstrap] ⚠️ Some systems are missing — check above errors.");
    }

    static bool CheckSingleton(string name, bool exists)
    {
        if (!exists)
        {
            Debug.LogError($"[Bootstrap] ❌ {name} singleton not found! Add it to the scene.");
            return false;
        }
        return true;
    }

    static void CheckComponent<T>(string name, GameObject go) where T : Component
    {
        if (go.GetComponent<T>() == null)
            Debug.LogWarning($"[Bootstrap] ⚠️ {name} not found on {go.name}");
    }
}
