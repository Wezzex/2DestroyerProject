using System.Collections.Generic;
using UnityEngine;

public class StationSpawner : MonoBehaviour
{
    public static StationSpawner Instance { get; private set; }

    [Header("Reference")]
    [SerializeField] private GameObject enemyStationPrefab;

    [Header("Station Settings")]
    [SerializeField] private int stationCount = 3;
    [SerializeField] private float stationRadiusMin = 400;
    [SerializeField] private float stationRadiusMax = 700;
    [SerializeField] private float stationSpawnDistance = 75;
    Vector3 worldOrigin = new Vector3(0, 0, 0);

    private readonly List<Transform> stations = new List<Transform>();

    public int AliveStationsCount { get; private set; }

    public IReadOnlyList<Transform> Stations
    {
        get
        {
            CleanupDeadStations();
            return stations;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Spawned in Awake so stations exist before other spawners run their Start.
        SpawnStations();
    }

    private void SpawnStations()
    {
        for (int i = 0; i < stationCount; i++)
        {
            Vector3 spawnPosition = stations.Count > 0
                ? TryGetValidStationSpawnFurthest()
                : GetStationCandidateSpawn();

            GameObject station = Instantiate(enemyStationPrefab, spawnPosition, Quaternion.identity);
            stations.Add(station.transform);

            AliveStationsCount++;
        }
    }

    public Transform GetRandomStation()
    {
        CleanupDeadStations();
        if (stations.Count == 0) return null;

        return stations[Random.Range(0, stations.Count)];
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
        return SpawnUtils.GetRandomPointInRange(worldOrigin, stationRadiusMin, stationRadiusMax);
    }

    private void CleanupDeadStations()
    {
        stations.RemoveAll(s => s == null);
    }

    public void OnStationDestroyed(GameObject station)
    {

        Debug.Log("Station Destroyed");
        AliveStationsCount--;

        if (AliveStationsCount <= 0)
        {
            GameManager.Instance.RequestGameOver(GameManager.GameOverReason.AllStationsDestroyed);
        }
    }
}
