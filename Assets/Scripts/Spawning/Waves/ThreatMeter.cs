using System;
using UnityEngine;

// Global "how dangerous is the player right now" value. Rises when stations raise the alarm and when
// enemies die, decays when the player keeps quiet. WaveDirector reads it to decide how fast and
// how close to the player a wave arrives.
public class ThreatMeter : MonoBehaviour
{
    public static ThreatMeter Instance { get; private set; }

    [SerializeField] private float maxThreat = 100f;
    [SerializeField] private float decayPerSecond = 0.5f;
    [Tooltip("Seconds after the last threat event before decay starts.")]
    [SerializeField] private float decayDelay = 10f;

    [Header("Threat sources")]
    [SerializeField] private float stationAlertThreat = 10f;
    [SerializeField] private float fighterKilledThreat = 1f;
    [SerializeField] private float shipKilledThreat = 4f;
    [SerializeField] private float stationDestroyedThreat = 25f;

    private float lastThreatTime;

    public float Threat { get; private set; }
    public float Normalized => maxThreat > 0f ? Threat / maxThreat : 0f;

    public event Action<float> ThreatChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        UnitManager.AnyUnitDied += OnAnyUnitDied;
    }

    private void OnDisable()
    {
        UnitManager.AnyUnitDied -= OnAnyUnitDied;
    }

    private void Update()
    {
        if (Threat <= 0f || Time.time < lastThreatTime + decayDelay) return;

        SetThreat(Threat - decayPerSecond * Time.deltaTime);
    }

    public void AddThreat(float amount)
    {
        lastThreatTime = Time.time;
        SetThreat(Threat + amount);
    }

    public void OnStationAlerted()
    {
        AddThreat(stationAlertThreat);
    }

    private void OnAnyUnitDied(UnitManager unit)
    {
        if (unit is PlayerManager) return;

        // Orphans burning out on their own are not the player's doing.
        SquadMember member = unit.GetComponentInParent<SquadMember>();
        if (member != null && member.IsOrphaned) return;

        if (unit is StationManager) AddThreat(stationDestroyedThreat);
        else if (unit is FighterManager) AddThreat(fighterKilledThreat);
        else AddThreat(shipKilledThreat);
    }

    private void SetThreat(float value)
    {
        float clamped = Mathf.Clamp(value, 0f, maxThreat);
        if (Mathf.Approximately(clamped, Threat)) return;

        Threat = clamped;
        ThreatChanged?.Invoke(Normalized);
    }
}
