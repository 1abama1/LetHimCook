using UnityEngine;
using System.Collections;

/// <summary>
/// Одиночный эффект мерцания для любого Renderer или Light.
/// Используйте для ночного освещения улицы (фонарные столбы и т.п.).
/// </summary>
public class LightFlicker : MonoBehaviour
{
    [Header("Light")]
    public Light targetLight;                   // если null — ищет на этом объекте
    public float baseIntensity  = 1f;
    public float flickerAmount  = 0.3f;         // ± от baseIntensity
    public float flickerSpeed   = 8f;           // скорость шума Перлина

    [Header("Random Offs")]
    [Tooltip("Шанс полностью отключиться на кадр")]
    [Range(0f, 0.05f)]
    public float offChance = 0.005f;

    private float _noiseOffset;

    void Awake()
    {
        if (targetLight == null) targetLight = GetComponent<Light>();
        _noiseOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (targetLight == null) return;

        // Редкое полное гашение
        if (Random.value < offChance)
        {
            targetLight.intensity = 0f;
            return;
        }

        float noise = Mathf.PerlinNoise(_noiseOffset + Time.time * flickerSpeed, 0f);
        targetLight.intensity = baseIntensity + (noise - 0.5f) * 2f * flickerAmount;
    }
}
