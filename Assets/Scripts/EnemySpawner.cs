using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Reference")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject enemyStationPrefab;
    [SerializeField] private GameObject[] enemyShipsPrefabs;

    [Header("Station Settings")]
    [SerializeField] private int stationCount = 3;
    [SerializeField] private float stationRadiusMin = 400;
    [SerializeField] private float stationRadiusMax = 700;
    [SerializeField] private float stationSpawnDistance = 75;


    [Header("Reference")]
    [SerializeField] private float shipSpawnRadius = 8f;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxShipStart = 3;
    [SerializeField] private int maxShipIncreasAmount = 1;
    [SerializeField] private float maxShipIncreasOverTime = 30f;


    private readonly List<Transform> stations = new List<Transform>();
    private readonly List<GameObject> aliveShips = new List<GameObject>();
    private GameObject[] stationsAlive;

    public int aliveStationsCount = 0;
    private int maxShips;
    private float nextMaxIncreaseTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        maxShips = maxShipStart;
        nextMaxIncreaseTimer = Time.time + maxShipIncreasOverTime;

        SpawnInitialStationAndShips();
        StartCoroutine(SpawnLoop());
        
    }

    private void SpawnInitialStationAndShips()
    {
        if (player == null)
        { 
            return;
        }

        for (int i = 0; i < stationCount; i++)
        {
            Vector3 spawnPosition = GetRandomPointInRange(player.position, stationRadiusMin, stationRadiusMax);
           

            if (stations.Count > 0)
            {
                
                    Vector3 newSpawnPosition = TryValidStationSpawn();

                    GameObject station = Instantiate(enemyStationPrefab, newSpawnPosition, Quaternion.identity);
                    stations.Add(station.transform);
                
            }
            else
            {
                GameObject station = Instantiate(enemyStationPrefab, spawnPosition, Quaternion.identity);
                stations.Add(station.transform);
            }

            aliveStationsCount++;
        }

        for (int i = 0; i < stations.Count; i++)
        {
            TrySpawnShipNearStation(stations[i]);
        }
        
        
    }

    private Vector3 TryValidStationSpawn()
    {
        Vector3 candidat = GetRandomPointInRange(player.position, stationRadiusMin, stationRadiusMax);
        int spawnTry = 5;

        for (int i = 0; i < spawnTry; i++)
        {
            candidat = GetRandomPointInRange(player.position, stationRadiusMin, stationRadiusMax);
            foreach (var station in stations)
            {
                if (Vector3.Distance(station.transform.position, candidat) < stationSpawnDistance)
                {
                    continue;
                }
            }
            return candidat;
        }

        return candidat;
    }

    private IEnumerator SpawnLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);

        while (true)
        {
            yield return wait;

            CleanupDeadShips();
            CleanupDeadStations();
            IncreaseSpawnWaveSize();

            if (stations.Count == 0) continue;
            if (aliveShips.Count >= maxShips) continue;

            Transform station = stations[UnityEngine.Random.Range(0, stations.Count)];
            TrySpawnShipNearStation(station);

        }
    }

    private void IncreaseSpawnWaveSize()
    {
        if (Time.time >= nextMaxIncreaseTimer)
        {
            maxShips += maxShipIncreasAmount;
            nextMaxIncreaseTimer = Time.time + maxShipIncreasOverTime;
        }
    }

    private void CleanupDeadShips()
    {
        aliveShips.RemoveAll(x => x == null);
    }

    private void CleanupDeadStations()
    {
        stations.RemoveAll(s => s == null);
    }

    public void OnStationDestroyed(GameObject station)
    {

        Debug.Log("Station Destroyed");
        aliveStationsCount--;

        if (aliveStationsCount <= 0)
        {
            GameManager.Instance.RequestGameOver(GameManager.GameOverReason.AllStationsDestroyed);
        }
    }


    private void TrySpawnShipNearStation(Transform station)
    {
        if (station == null) return;
        if (aliveShips.Count >= maxShips) return;

        GameObject enemyShipPrefab = enemyShipsPrefabs[UnityEngine.Random.Range(0, enemyShipsPrefabs.Length)];

        Vector2 offset = UnityEngine.Random.insideUnitCircle * shipSpawnRadius;
        Vector3 spawnPosition = station.position + new Vector3(offset.x, 0f, offset.y);
        spawnPosition.y = station.position.y;

        GameObject ship = Instantiate(enemyShipPrefab, spawnPosition, Quaternion.identity);

        PatrolArea patrolArea = ship.GetComponentInChildren<PatrolArea>();
        if (patrolArea != null)
        {


            patrolArea.InitilizeSpawnPoints();
            patrolArea.SetPatrolAncor(station.transform.position);
        }

        aliveShips.Add(ship);
    }

    private static Vector3 GetRandomPointInRange(Vector3 center, float minRadius, float maxRadius)
    {
        float radius = UnityEngine.Random.Range(minRadius, maxRadius);
        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;

        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        return new Vector3(center.x + x, center.y, center.z + z);

    }
}
