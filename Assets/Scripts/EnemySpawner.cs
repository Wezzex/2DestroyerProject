using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Reference")]
    [SerializeField] private Transform player;
    [SerializeField] private StationSpawner stationSpawner;
    [SerializeField] private GameObject[] enemyShipsPrefabs;

    [Header("Migration")]
    [Tooltip("Turn off once stations use DeploySpawner/WaveDirector for destroyers.")]
    [SerializeField] private bool spawnDestroyers = true;
    [Tooltip("Turn off once stations have a DeploySpawner for fighters.")]
    [SerializeField] private bool spawnFighters = true;

    [Header("Destroyer Settings")]
    [SerializeField] private float shipMaxSpawnRadius = 75f;
    [SerializeField] private float shipMinSpawnRadius = 50f;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxShipStart = 3;
    [SerializeField] private int maxShipIncreasAmount = 1;
    [SerializeField] private float maxShipIncreasOverTime = 30f;

    [Header("Fighter Settings")]
    [SerializeField] private GameObject[] fighterPrefabs;
    [SerializeField] private int fighterCount = 5;
    [SerializeField] private float fighterMinSpawnRadius = 50f;
    [SerializeField] private float figherMaxSpawnRadius = 100f;


    private readonly List<GameObject> aliveDestroyers = new List<GameObject>();
    private readonly List<GameObject> aliveFighters = new List<GameObject>();

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

        if (stationSpawner == null)
        {
            stationSpawner = StationSpawner.Instance;
        }
    }

    private void Start()
    {
        maxShips = maxShipStart;
        nextMaxIncreaseTimer = Time.time + maxShipIncreasOverTime;

        SpawnInitialShips();
        StartCoroutine(SpawnLoop());

    }

    private void SpawnInitialShips()
    {
        if (player == null || stationSpawner == null)
        {
            return;
        }

        foreach (Transform station in stationSpawner.Stations)
        {
            if (spawnDestroyers) TrySpawnShipNearStation(station);
            if (spawnFighters) TrySpawnFightersNearStation(station, fighterCount);
        }
    }

    private IEnumerator SpawnLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);

        while (true)
        {
            yield return wait;

            CleanupDeadShips();
            IncreaseSpawnWaveSize();

            if (!spawnDestroyers || stationSpawner == null) continue;
            if (aliveDestroyers.Count >= maxShips) continue;

            Transform station = stationSpawner.GetRandomStation();
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

    private void TrySpawnFightersNearStation(Transform station, int fighterCount)
    {

        for (int i = 0; i < fighterCount; i++)
        {
            GameObject fighterPrefab = fighterPrefabs[UnityEngine.Random.Range(0, fighterPrefabs.Length)];

            Vector3 spawnPosition = SpawnUtils.GetRandomPointInRange(station.position, fighterMinSpawnRadius, figherMaxSpawnRadius);
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

        Vector3 candidate = SpawnUtils.GetRandomPointInRange(station.transform.position, shipMinSpawnRadius, shipMaxSpawnRadius);

        return candidate;

    }
}
