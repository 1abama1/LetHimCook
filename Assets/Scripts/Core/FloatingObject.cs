using UnityEngine;
using System.Collections;

/// <summary>
/// Простой скрипт для объектов, которые должны бобиться/пульсировать
/// (иконки подбора, маркеры важных объектов и т.п.).
/// </summary>
public class FloatingObject : MonoBehaviour
{
    [Header("Bob")]
    public float bobAmplitude = 0.15f;
    public float bobFrequency = 1.5f;

    [Header("Rotate")]
    public float rotateSpeed = 45f;   // градусов/сек; 0 = без вращения
    public Vector3 rotateAxis = Vector3.up;

    [Header("Pulse Scale")]
    public bool  pulseScale     = false;
    public float pulseAmount    = 0.05f;
    public float pulseFrequency = 2f;

    private Vector3 _startPos;
    private Vector3 _startScale;

    void Awake()
    {
        _startPos   = transform.localPosition;
        _startScale = transform.localScale;
    }

    void Update()
    {
        // Боббинг
        float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.localPosition = _startPos + Vector3.up * bob;

        // Вращение
        if (rotateSpeed != 0f)
            transform.Rotate(rotateAxis, rotateSpeed * Time.deltaTime, Space.World);

        // Пульсация масштаба
        if (pulseScale)
        {
            float s = 1f + Mathf.Sin(Time.time * pulseFrequency) * pulseAmount;
            transform.localScale = _startScale * s;
        }
    }
}
