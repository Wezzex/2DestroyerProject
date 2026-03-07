using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Events;

public class EnemyManager : UnitManager
{

    [Header("References")]
    [SerializeField] private ShipController shipController;
    [SerializeField] private EnemySpawner spawner;

    [Header("Health Settings")]
    [SerializeField] private int health;

    [SerializeField] private GameObject explotionPrefab;
    public bool IsDead => bIsDead;

    private void Awake()
    {
        shipController = GetComponent<ShipController>();
        spawner = GetComponent<EnemySpawner>();
    }

    public override void CreateDeathExplotion()
    {
        Vector3 explotionSpawnPosition = this.transform.position;
        GameObject explotion = Instantiate(explotionPrefab, explotionSpawnPosition, Quaternion.identity);

    }


}
