using UnityEngine;

public enum FormationStance
{
    Hold,    // stay in formation where it is, facing the threat
    Advance, // the leader leads the whole formation towards the player
    Break    // advance, and some wingmen break formation to engage on their own
}

// How disciplined and aggressive the enemy is. Swap the asset per difficulty level or mission.
// aggression = player threat (0-1) + (1 - discipline) * indisciplineAggression
[CreateAssetMenu(fileName = "EnemyDoctrine", menuName = "Scriptable Objects/EnemyDoctrine")]
public class EnemyDoctrine : ScriptableObject
{
    [Tooltip("1 = always holds formation, 0 = undisciplined.")]
    [Range(0f, 1f)] public float discipline = 0.7f;
    [Tooltip("How much low discipline adds to aggression on top of the player's threat.")]
    [Range(0f, 1f)] public float indisciplineAggression = 0.5f;

    [Header("Stances")]
    [Tooltip("Aggression at which the formation starts advancing on the player.")]
    [Range(0f, 1f)] public float advanceAggression = 0.6f;
    [Tooltip("Aggression at which wingmen may break formation.")]
    [Range(0f, 1f)] public float breakAggression = 0.9f;
    [Tooltip("Only a force with discipline at or below this ever breaks formation.")]
    [Range(0f, 1f)] public float maxBreakDiscipline = 0.4f;
    [Tooltip("Share of the wingmen that break at full aggression.")]
    [Range(0f, 1f)] public float maxBreakFraction = 0.5f;

    private static EnemyDoctrine fallback;

    // Used when nothing is assigned, so every owner works out of the box.
    public static EnemyDoctrine Fallback
    {
        get
        {
            if (fallback == null)
            {
                fallback = CreateInstance<EnemyDoctrine>();
                fallback.name = "EnemyDoctrine (default)";
            }
            return fallback;
        }
    }

    public float GetAggression(float threat01)
    {
        return Mathf.Clamp01(threat01 + (1f - discipline) * indisciplineAggression);
    }

    public FormationStance GetStance(float threat01)
    {
        float aggression = GetAggression(threat01);

        if (aggression >= breakAggression && discipline <= maxBreakDiscipline) return FormationStance.Break;
        if (aggression >= advanceAggression) return FormationStance.Advance;
        return FormationStance.Hold;
    }

    // How many of the wingmen (never the leader) break formation.
    public int GetBreakCount(int wingmen, float threat01)
    {
        if (wingmen <= 0 || GetStance(threat01) != FormationStance.Break) return 0;

        float t = Mathf.InverseLerp(breakAggression, 1f, GetAggression(threat01));
        int count = Mathf.RoundToInt(wingmen * maxBreakFraction * Mathf.Lerp(0.5f, 1f, t));
        return Mathf.Clamp(count, 1, wingmen);
    }
}
