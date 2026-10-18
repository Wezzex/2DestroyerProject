using System;
using System.Collections.Generic;
using UnityEngine;

// A predetermined wave. WaveDirector picks one (weighted random or in order) when a distress timer runs out.
// Waves are destroyer-class only: fighters always belong to a DeploySpawner owner.
[CreateAssetMenu(fileName = "WaveDefinition", menuName = "Scriptable Objects/WaveDefinition")]
public class WaveDefinition : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public GameObject prefab;
        [Min(1)] public int count;
    }

    public List<Entry> ships = new List<Entry>();

    [Header("Formation")]
    public FormationShape formation = FormationShape.Wedge;
    public float spacing = 15f;

    [Header("Selection")]
    [Tooltip("Only used when the current threat (0-1) is at least this high.")]
    [Range(0f, 1f)] public float minThreat = 0f;
    [Min(0f)] public float weight = 1f;

    public List<GameObject> BuildShipList()
    {
        var list = new List<GameObject>();
        foreach (Entry entry in ships)
        {
            if (entry.prefab == null) continue;
            for (int i = 0; i < entry.count; i++) list.Add(entry.prefab);
        }
        return list;
    }
}
