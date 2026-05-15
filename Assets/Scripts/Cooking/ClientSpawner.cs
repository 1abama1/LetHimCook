using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ClientSpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject clientPrefab;
    public List<Transform> tableSpawnPoints;
    public float spawnInterval = 15f;
    public int maxClients = 3;

    private float _spawnTimer;
    private List<GameObject> _activeClients = new List<GameObject>();
    private bool _isDayPhase;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            // Подписываемся на смену фаз
            GameManager.Instance.onDayBegin.AddListener(OnDayBegin);
            GameManager.Instance.onNightBegin.AddListener(OnNightBegin);
            _isDayPhase = GameManager.Instance.currentPhase == GamePhase.Day;
        }
        _spawnTimer = spawnInterval;
    }

    void Update()
    {
        if (!_isDayPhase) return;

        // Очищаем список от ушедших/уничтоженных клиентов
        _activeClients.RemoveAll(item => item == null);

        if (_activeClients.Count >= maxClients) return;

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0)
        {
            SpawnClient();
            _spawnTimer = spawnInterval;
        }
    }

    void SpawnClient()
    {
        if (tableSpawnPoints.Count == 0 || clientPrefab == null) return;

        // Pick a free spawn point (not already occupied by an active client)
        Transform spawnPoint = tableSpawnPoints
            .Where(t => !_activeClients.Any(c => c != null &&
                   Vector3.Distance(c.transform.position, t.position) < 0.5f))
            .OrderBy(_ => Random.value)
            .FirstOrDefault();

        if (spawnPoint == null) return; // All tables occupied

        GameObject newClient = Instantiate(clientPrefab, spawnPoint.position, spawnPoint.rotation);
        _activeClients.Add(newClient);
    }

    void OnDayBegin()
    {
        _isDayPhase = true;
        _spawnTimer = spawnInterval; // Сбрасываем таймер в начале дня
    }

    void OnNightBegin()
    {
        _isDayPhase = false;
        // По желанию можно заставить всех оставшихся клиентов уйти
    }
}
