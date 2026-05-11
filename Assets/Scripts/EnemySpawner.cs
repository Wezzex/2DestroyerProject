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
    Vector3 worldOrigin = new Vector3(0, 0, 0);


    [Header("Destroyer Settings")]
    [SerializeField] private float shipMaxSpawnRadius = 75f;
    [SerializeField] private float shipMinSpawnRadius = 50f;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxShipStart = 3;
    [SerializeField] private int maxShipIncreasAmount = 1;
    [SerializeField] private float maxShipIncreasOverTime = 30f;

    [Header("Destroyer Settings")]
    [SerializeField] private GameObject[] fighterPrefabs;
    [SerializeField] private int fighterCount = 5;
    [SerializeField] private float fighterMinSpawnRadius = 50f;
    [SerializeField] private float figherMaxSpawnRadius = 100f;


    private readonly List<Transform> stations = new List<Transform>();
    private readonly List<GameObject> aliveDestroyers = new List<GameObject>();
    private readonly List<GameObject> aliveFighters = new List<GameObject>();
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
            Vector3 spawnPosition = GetRandomPointInRange(worldOrigin, stationRadiusMin, stationRadiusMax);
           

            if (stations.Count > 0)
            {
                
                    Vector3 newSpawnPosition = TryGetValidStationSpawnFurthest();

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
            TrySpawnFightersNearStation(stations[i], fighterCount);
        }
        
        
    }

    

    private Vector3 TryValidStationSpawn()
    {
        Vector3 candidat = GetStationCandidateSpawn();
        int spawnTries = 50;
        bool validSpawn = true;

        for (int i = 0; i < spawnTries; i++)
        {
            candidat = GetStationCandidateSpawn();
            validSpawn = true;
            foreach (var station in stations)
            {
                if (Vector3.Distance(station.transform.position, candidat) < stationSpawnDistance)
                {
                    validSpawn = false;
                    break;
                }
            }
            if(validSpawn)
                return candidat;
            
        }

        Debug.LogWarning($"Could not find Valid spawn for station: {stations[0]} after: {spawnTries} Tries.");
        return candidat;
    }

    private Vector3 TryGetValidStationSpawnFurthest()
    {
        Vector3 bestCandidate = GetStationCandidateSpawn();
        float bestMinDistance = float.NegativeInfinity;

        int spawnTries = 50;

        for (int i = 0; i < spawnTries; i++)
        {
            Vector3 candidate = GetStationCandidateSpawn();
            float minDistanceToAnyStation = float.PositiveInfinity;

            foreach (var station in stations)
            {
                float distance = Vector3.Distance(candidate, station.position);

                if (distance < minDistanceToAnyStation)
                {
                    minDistanceToAnyStation = distance;
                }
            }
            if (minDistanceToAnyStation >= stationSpawnDistance)
            {
                return candidate;
            }

            if (minDistanceToAnyStation > bestMinDistance)
            {
                bestMinDistance = minDistanceToAnyStation;
                bestCandidate = candidate;
            }

        }
        Debug.LogWarning($"No candidate met stationSpawnDistance={stationSpawnDistance}. Using best fallback minDist={bestMinDistance}");
        return bestCandidate;

    }

    private Vector3 GetStationCandidateSpawn()
    {
        return GetRandomPointInRange(worldOrigin, stationRadiusMin, stationRadiusMax);
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
            if (aliveDestroyers.Count >= maxShips) continue;

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
        aliveDestroyers.RemoveAll(x => x == null);
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

    private void TrySpawnFightersNearStation(Transform station, int fighterCount)
    {

        for (int i = 0; i < fighterCount; i++)
        {
            GameObject fighterPrefab = fighterPrefabs[UnityEngine.Random.Range(0, fighterPrefabs.Length)];

            Vector3 spawnPosition = GetRandomPointInRange(station.position, fighterMinSpawnRadius, figherMaxSpawnRadius);
            spawnPosition.y = station.position.y;

            GameObject fighter = Instantiate(fighterPrefab, spawnPosition, Quaternion.identity);
            AIContext context = fighter.GetComponent<AIContext>();
            AIDetector detector = fighter.GetComponent<AIDetector>();

            if (context != null)
            {
                context.SetParentStation(station);
            }

            PatrolArea patrolArea = fighter.GetComponentInChildren<PatrolArea>();
            FighterStrafePoints fighterStrafe = fighter.GetComponentInChildren<FighterStrafePoints>();
            if (patrolArea != null)
            {
                patrolArea.SetPatrolAncor(station);
                patrolArea.InitilizeSpawnPoints();

            }
            Transform fighters = station.Find("Fighters");
            if (fighters == null)
            {
                var go = new GameObject("Fighters");
                fighters = go.transform;
                fighters.SetParent(station, true);
            }
            fighter.transform.SetParent(fighters, true);

            aliveFighters.Add(fighter);
        }

    }


    private void TrySpawnShipNearStation(Transform station)
    {
        if (station == null) return;
        if (aliveDestroyers.Count >= maxShips) return;

        GameObject enemyShipPrefab = enemyShipsPrefabs[UnityEngine.Random.Range(0, enemyShipsPrefabs.Length)];

        Vector3 spawnPosition = TryShipSpawnCandidate(station);
        spawnPosition.y = station.position.y;

        GameObject ship = Instantiate(enemyShipPrefab, spawnPosition, Quaternion.identity);

        var context = ship.GetComponent<AIContext>();
        Transform defenders = station.Find("Defenders");
        if (defenders == null)
        {
            var go = new GameObject("Defenders");
            defenders = go.transform;
            defenders.SetParent(station, true);
        }

        ship.transform.SetParent(defenders, true);


        if (context != null)
        {
            context.SetParentStation(station);
        }

        PatrolArea patrolArea = ship.GetComponentInChildren<PatrolArea>();
        if (patrolArea != null)
        {
            patrolArea.SetPatrolAncor(station);
            patrolArea.InitilizeSpawnPoints();
        }

        aliveDestroyers.Add(ship);
    }

    private Vector3 TryShipSpawnCandidate(Transform station)
    {
        
        Vector3 candidate = GetRandomPointInRange(station.transform.position, shipMinSpawnRadius, shipMaxSpawnRadius);

        return candidate;

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
