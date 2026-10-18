using System;
using System.Collections.Generic;
using UnityEngine;

public enum WaveSelection
{
    WeightedRandom,
    Sequential
}

// Scene singleton. Receives distress calls from stations, runs a timer per station, then spawns a
// destroyer wave in formation. Higher threat = shorter timer, and the wave appears closer to and
// more accurately around the player (never on top of them).
public class WaveDirector : MonoBehaviour
{
    public static WaveDirector Instance { get; private set; }

    private class PendingWave
    {
        public DistressBeacon beacon;
        public Transform target;
        public float fireAt;
    }

    [Header("Waves")]
    [SerializeField] private List<WaveDefinition> waves = new List<WaveDefinition>();
    [SerializeField] private WaveSelection selection = WaveSelection.WeightedRandom;

    [Header("Timing")]
    [SerializeField] private float waveDelayAtLowThreat = 30f;
    [SerializeField] private float waveDelayAtHighThreat = 10f;
    [Tooltip("After a station's wave arrives, it can't call for aid again for this long.")]
    [SerializeField] private float stationCooldown = 60f;
    [Tooltip("Cancel a pending wave if the player escapes before it arrives.")]
    [SerializeField] private bool cancelIfTargetLost = false;

    [Header("Limits")]
    [SerializeField] private int maxAliveWaveShips = 12;

    [Header("Placement")]
    [Tooltip("Waves never spawn closer to the player than this.")]
    [SerializeField] private float minDistanceFromPlayer = 40f;
    [Tooltip("Random spread around the spawn centre at threat 0 (inaccurate).")]
    [SerializeField] private float spreadAtLowThreat = 80f;
    [Tooltip("Random spread around the spawn centre at max threat (accurate).")]
    [SerializeField] private float spreadAtHighThreat = 10f;
    [Tooltip("How far the spawn centre moves from the station towards the player at max threat (0-1).")]
    [SerializeField, Range(0f, 1f)] private float maxApproach = 1f;
    [Tooltip("Waves always arrive within this distance of the station that called, even if the player has flown off towards another station.")]
    [SerializeField] private float maxDistanceFromStation = 150f;
    [Tooltip("Keeps the wave centre clear of the calling station's hull.")]
    [SerializeField] private float minDistanceFromStation = 60f;
    [SerializeField] private float navMeshSearchRadius = 20f;

    [Header("Fleet behaviour")]
    [Tooltip("Leave empty to use the default doctrine.")]
    [SerializeField] private EnemyDoctrine doctrine;
    [SerializeField] private FleetSettings fleetSettings = new FleetSettings();

    [Header("References")]
    [SerializeField] private ThreatMeter threatMeter;

    private readonly List<PendingWave> pending = new List<PendingWave>();
    private readonly Dictionary<DistressBeacon, float> cooldownUntil = new Dictionary<DistressBeacon, float>();
    private readonly List<UnitManager> aliveWaveShips = new List<UnitManager>();
    private Transform waveContainer;
    private int sequentialIndex;

    // UI / audio hooks: "Enemy reinforcements arriving in X seconds".
    public event Action<DistressBeacon, float> WaveIncoming;
    public event Action<DistressBeacon, List<GameObject>> WaveSpawned;

    private float Threat01 => threatMeter != null ? threatMeter.Normalized : 0f;

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
        if (threatMeter == null) threatMeter = ThreatMeter.Instance;
        waveContainer = UnitSetup.GetOrCreateContainer("WaveUnits");
    }

    public bool RequestAid(DistressBeacon beacon, Transform target)
    {
        if (beacon == null || target == null) return false;

        // Waves already out get the position, even if this station can't call for a new wave right now.
        WaveFleet.ReportSightingToAll(target);

        if (pending.Exists(p => p.beacon == beacon)) return false;
        if (cooldownUntil.TryGetValue(beacon, out float until) && Time.time < until) return false;

        if (threatMeter != null) threatMeter.OnStationAlerted();

        float delay = Mathf.Lerp(waveDelayAtLowThreat, waveDelayAtHighThreat, Threat01);
        pending.Add(new PendingWave { beacon = beacon, target = target, fireAt = Time.time + delay });

        WaveIncoming?.Invoke(beacon, delay);
        return true;
    }

    public void CancelAid(DistressBeacon beacon)
    {
        pending.RemoveAll(p => p.beacon == beacon);
    }

    private void Update()
    {
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            PendingWave wave = pending[i];

            if (wave.beacon == null || !wave.beacon.IsAlive)
            {
                pending.RemoveAt(i);
                continue;
            }

            bool targetGone = wave.target == null || !wave.target.gameObject.activeInHierarchy;
            if (targetGone && cancelIfTargetLost)
            {
                pending.RemoveAt(i);
                continue;
            }

            if (Time.time < wave.fireAt) continue;

            pending.RemoveAt(i);
            cooldownUntil[wave.beacon] = Time.time + stationCooldown;

            if (!targetGone) SpawnWave(wave.beacon, wave.target);
        }
    }

    private void SpawnWave(DistressBeacon beacon, Transform target)
    {
        aliveWaveShips.RemoveAll(u => u == null || u.IsDead);
        int room = maxAliveWaveShips - aliveWaveShips.Count;
        if (room <= 0) return;

        WaveDefinition definition = PickWave(Threat01);
        if (definition == null) return;

        List<GameObject> prefabs = definition.BuildShipList();
        prefabs.RemoveAll(IsFighterPrefab);
        if (prefabs.Count > room) prefabs.RemoveRange(room, prefabs.Count - room);
        if (prefabs.Count == 0) return;

        Vector3 stationPosition = beacon.Station.position;
        Vector3 anchor = GetWaveAnchor(stationPosition, target.position, Threat01);
        Vector3 facing = target.position - anchor;
        facing.y = 0f;
        Quaternion rotation = facing.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(facing.normalized, Vector3.up) : Quaternion.identity;

        List<Vector3> positions = FormationUtility.GetWorldPositions(definition.formation, prefabs.Count, definition.spacing, anchor, facing);
        var spawned = new List<GameObject>(prefabs.Count);

        for (int i = 0; i < prefabs.Count; i++)
        {
            // A formation slot inside the station's hull is further than navMeshSearchRadius from walkable
            // NavMesh; search wider before giving up on the ship.
            if (!UnitSetup.TryGetNavMeshPoint(positions[i], navMeshSearchRadius, out Vector3 position) &&
                !UnitSetup.TryGetNavMeshPoint(positions[i], Mathf.Max(navMeshSearchRadius, NavMeshUtility.DefaultSearchRadius), out position))
            {
                continue;
            }

            GameObject ship = Instantiate(prefabs[i], position, rotation, waveContainer);
            UnitSetup.AssignHome(ship, beacon.Station);
            UnitSetup.AssignTarget(ship, target);

            UnitManager unit = ship.GetComponentInChildren<UnitManager>(true);
            if (unit != null) aliveWaveShips.Add(unit);

            spawned.Add(ship);
        }

        if (spawned.Count > 0)
        {
            var fleetObject = new GameObject($"Fleet_{beacon.name}_{definition.name}");
            fleetObject.transform.SetParent(waveContainer, false);
            fleetObject.AddComponent<WaveFleet>().Initialize(spawned, definition.formation, definition.spacing, target, doctrine, fleetSettings);
        }

        WaveSpawned?.Invoke(beacon, spawned);
    }

    // Threat 0: spawn around the station with a wide, sloppy spread.
    // Threat 1: spawn centre slides to the player with a tight spread, pushed out to minDistanceFromPlayer.
    // The anchor always stays in a ring around the calling station (minDistanceFromStation..maxDistanceFromStation).
    private Vector3 GetWaveAnchor(Vector3 stationPosition, Vector3 playerPosition, float threat01)
    {
        // The player is read when the timer fires, not when the station called, and may be far away by then.
        Vector3 toPlayer = playerPosition - stationPosition;
        toPlayer.y = 0f;
        Vector3 approachPoint = stationPosition + Vector3.ClampMagnitude(toPlayer, maxDistanceFromStation);

        Vector3 centre = Vector3.Lerp(stationPosition, approachPoint, threat01 * maxApproach);
        float spread = Mathf.Lerp(spreadAtLowThreat, spreadAtHighThreat, threat01);

        Vector2 offset = UnityEngine.Random.insideUnitCircle * spread;
        Vector3 anchor = centre + new Vector3(offset.x, 0f, offset.y);

        Vector3 fromStation = anchor - stationPosition;
        fromStation.y = 0f;
        if (fromStation.sqrMagnitude < 0.01f) fromStation = toPlayer.sqrMagnitude > 0.01f ? toPlayer : Vector3.forward;
        float stationDistance = Mathf.Clamp(fromStation.magnitude, minDistanceFromStation, maxDistanceFromStation);
        anchor = stationPosition + fromStation.normalized * stationDistance;

        Vector3 fromPlayer = anchor - playerPosition;
        fromPlayer.y = 0f;

        if (fromPlayer.magnitude < minDistanceFromPlayer)
        {
            Vector3 direction = fromPlayer.sqrMagnitude > 0.01f ? fromPlayer.normalized : (stationPosition - playerPosition).normalized;
            if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
            direction.y = 0f;
            anchor = playerPosition + direction.normalized * minDistanceFromPlayer;
        }

        anchor.y = stationPosition.y;
        return anchor;
    }

    private WaveDefinition PickWave(float threat01)
    {
        if (waves.Count == 0) return null;

        if (selection == WaveSelection.Sequential)
        {
            WaveDefinition next = waves[sequentialIndex % waves.Count];
            sequentialIndex++;
            return next;
        }

        float totalWeight = 0f;
        foreach (WaveDefinition wave in waves)
        {
            if (wave != null && threat01 >= wave.minThreat) totalWeight += wave.weight;
        }
        if (totalWeight <= 0f) return null;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        foreach (WaveDefinition wave in waves)
        {
            if (wave == null || threat01 < wave.minThreat) continue;

            roll -= wave.weight;
            if (roll <= 0f) return wave;
        }
        return null;
    }

    // Fighters can't fly on their own; they only exist under a DeploySpawner owner.
    private static bool IsFighterPrefab(GameObject prefab)
    {
        if (prefab.GetComponentInChildren<FighterManager>(true) == null) return false;

        Debug.LogWarning($"[WaveDirector] {prefab.name} is a fighter and can't be part of a wave. Give it to a DeploySpawner instead.");
        return true;
    }
}
