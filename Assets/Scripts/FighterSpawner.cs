using UnityEngine;

public class FighterSpawner : MonoBehaviour
{
    [SerializeField] private int fightersAmount = 5;
    [SerializeField] private float waveCooldownSeconds = 60f;

    [SerializeField] private Transform spawnPointTransform;
    [SerializeField] private Transform returnPointTransform;

    [SerializeField] private AIDetector parentSensor;

    [SerializeField] private GameManager[] fighterPrefabs;

}
