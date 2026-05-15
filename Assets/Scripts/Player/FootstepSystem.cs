using UnityEngine;
using System.Collections;

/// <summary>
/// Воспроизводит звуки шагов в зависимости от состояния игрока.
/// Добавьте на игрока. Нужны SoundEntry-ключи в AudioManager:
///   "step_walk", "step_sprint", "step_sneak"
/// </summary>
public class FootstepSystem : MonoBehaviour
{
    [Header("Intervals")]
    public float walkInterval   = 0.45f;
    public float sprintInterval = 0.28f;
    public float sneakInterval  = 0.65f;

    [Header("Sound Keys")]
    public string walkSoundKey   = "step_walk";
    public string sprintSoundKey = "step_sprint";
    public string sneakSoundKey  = "step_sneak";

    private PlayerController _player;
    private float            _stepTimer;

    void Awake() => _player = GetComponent<PlayerController>();

    void Update()
    {
        if (_player == null || _player.IsHiding || !_player.IsMoving) { _stepTimer = 0f; return; }

        _stepTimer -= Time.deltaTime;
        if (_stepTimer > 0f) return;

        PlayStep();
    }

    void PlayStep()
    {
        string key;
        float  interval;

        if (_player.IsSneaking)
        {
            key      = sneakSoundKey;
            interval = sneakInterval;
        }
        else if (_player.IsSprinting)
        {
            key      = sprintSoundKey;
            interval = sprintInterval;
        }
        else
        {
            key      = walkSoundKey;
            interval = walkInterval;
        }

        AudioManager.Instance?.PlayAt(key, transform.position);
        _stepTimer = interval;
    }
}
