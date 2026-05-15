using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Спавнит клиентов и управляет очередью к окошку.
///
/// Точка 1 (spawnPoint)  — место появления
/// Точка 2 (windowPoint) — окошко, куда подходит первый в очереди
/// Точка 3 (exitPoint)   — точка выхода после обслуживания / злого ухода
/// </summary>
public class ClientSpawner : MonoBehaviour
{
    [Header("Points")]
    public Transform spawnPoint;   // точка 1 — место спавна
    public Transform windowPoint;  // точка 2 — окошко выдачи
    public Transform exitPoint;    // точка 3 — выход

    [Header("Queue Settings")]
    [Tooltip("Расстояние между клиентами в очереди")]
    public float queueSpacing = 1.2f;

    [Header("Spawn Settings")]
    public List<GameObject> clientPrefabs = new List<GameObject>();
    public float spawnInterval = 15f;
    public int maxClients = 6;

    // Очередь: [0] — у окошка, [1],[2]... — стоят сзади
    private readonly List<ClientNPC> _queue = new List<ClientNPC>();

    private float _spawnTimer;
    private bool _isDayPhase;

    // ──────────────────────────────────────────────────────────────
    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onDayBegin.AddListener(OnDayBegin);
            GameManager.Instance.onNightBegin.AddListener(OnNightBegin);
            _isDayPhase = GameManager.Instance.currentPhase == GamePhase.Day;
        }
        _spawnTimer = spawnInterval;
    }

    void Update()
    {
        if (!_isDayPhase) return;

        // Чистим уничтоженных клиентов (ушли злые или Destroy после подачи)
        int before = _queue.Count;
        _queue.RemoveAll(c => c == null);
        if (_queue.Count != before)
            RefreshQueuePositions();

        if (_queue.Count >= maxClients) return;

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            SpawnClient();
            _spawnTimer = spawnInterval;
        }
    }

    // ──────────────────────────────────────────────────────────────
    void SpawnClient()
    {
        if (clientPrefabs == null || clientPrefabs.Count == 0 || spawnPoint == null) return;

        // Выбираем случайный префаб из списка
        GameObject prefab = clientPrefabs[Random.Range(0, clientPrefabs.Count)];
        
        // Защита от пустых слотов (None) в инспекторе
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        ClientNPC npc = go.GetComponent<ClientNPC>();
        if (npc == null) { Destroy(go); return; }

        npc.Init(this);
        _queue.Add(npc);
        RefreshQueuePositions();

        // Если первый — сразу идёт к окошку
        if (_queue.Count == 1)
            npc.GoToWindow();
    }

    // ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Вызывается ClientNPC когда клиент покидает очередь
    /// (получил еду или ушёл злым).
    /// </summary>
    public void OnClientLeft(ClientNPC client)
    {
        _queue.Remove(client);
        RefreshQueuePositions();

        // Следующий в очереди подходит к окошку
        if (_queue.Count > 0 && _queue[0] != null)
            _queue[0].GoToWindow();
    }

    // ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Пересчитывает целевые позиции всех стоящих клиентов.
    /// </summary>
    void RefreshQueuePositions()
    {
        if (windowPoint == null) return;

        // Строим очередь "назад" от окошка вдоль -forward
        Vector3 back = -windowPoint.forward;

        for (int i = 0; i < _queue.Count; i++)
        {
            if (_queue[i] == null) continue;

            Vector3 target = windowPoint.position + back * (i * queueSpacing);
            // isFirst == true только если i==0 И NPC уже получил команду GoToWindow,
            // иначе он просто стоит на позиции в очереди
            _queue[i].SetQueueTarget(target, i == 0);
        }
    }

    // ──────────────────────────────────────────────────────────────
    public Transform WindowPoint => windowPoint;
    public Transform ExitPoint   => exitPoint;

    void OnDayBegin()
    {
        _isDayPhase = true;
        _spawnTimer = spawnInterval;
    }

    void OnNightBegin()
    {
        _isDayPhase = false;
    }
}
