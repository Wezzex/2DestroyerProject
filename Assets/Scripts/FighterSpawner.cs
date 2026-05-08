using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class FighterSpawner : MonoBehaviour
{

    //[SerializeField] private Transform stationTransform;
    //[SerializeField] private Transform fighterContainerTransform;
    //[SerializeField] private Transform fighterDockTransform;

    //[SerializeField] private AIDetector parentSensor;

    //[SerializeField] private GameObject[] fighterPrefabs;


    //[SerializeField] private int fightersAmount = 5;
    //[SerializeField] private float waveCooldownSeconds = 60f;
    //[SerializeField] private float minSpawnRadius = 0f;
    //[SerializeField] private float maxSpawnRadius = 10f;

    //[SerializeField] private float nextReplenishTime = 0f;

    //List<FighterSlot> fighterSlots;
    //private bool bTargetInRange = false;
 


    //struct FighterSlot
    //{
    //    GameObject fighter;
    //    bool bIsDead;
    //    bool bIsDocked;
    //}

    //private void Awake()
    //{
    //    if (parentSensor == null)
    //    {
    //        parentSensor = GetComponentInParent<AIDetector>();
    //    }
    //}

    //private void Start()
    //{

    //    stationTransform = transform.parent;
    //    EnsureDockingContainerExists();

        

        
    //}

    //private void EnsureDockingContainerExists()
    //{
    //    Transform defenders = stationTransform.Find("Defender");
    //    if (defenders == null)
    //    {
    //        defenders = new GameObject("Defender").transform;
    //        defenders.SetParent(stationTransform);
    //    }

    //    fighterContainerTransform = defenders.Find("Fighters");
    //    if (fighterContainerTransform == null)
    //    {
    //        fighterContainerTransform = new GameObject("Fighters").transform;
    //        fighterContainerTransform.SetParent(defenders);
    //    }

    //    fighterDockTransform = defenders.Find("Fighters");
    //    if (fighterDockTransform == null)
    //    {
    //        fighterDockTransform = new GameObject("Fighters").transform;
    //        fighterDockTransform.SetParent(defenders);
    //    }



    //}

    //private void Update()
    //{
    //    if (TargetInRange())
    //    {
    //        bTargetInRange = true;
    //        DeployActiveFighters();
    //    }

    //    if (!TargetInRange())
    //    {
    //        bTargetInRange = false;
    //        RecalFighters();
    //    }
    //}

    //public bool TargetInRange()
    //{
    //    if (parentSensor == null || parentSensor.Target == null) return false;

    //    return false;
    //}

    //private void SpawnFighter()
    //{
    //    for (int i = 0; i < fightersAmount; i++)
    //    {
    //        GameObject fighterPrefab = fighterPrefabs[UnityEngine.Random.Range(0, fighterPrefabs.Length)];

    //        GameObject fighter = Instantiate(fighterPrefab, stationTransform.position, Quaternion.identity);

    //        fighter.SetActive(false);
    //        fighter.transform.position = dockingPointTransform.position;

    //        fighter.transform.SetParent(fighterDock, true);

    //        fighterSlots.Add(fighter);
    //    }

    //    foreach (var fighter in fighterSlots)
    //    {

    //    }
    //}

    //private void DeployActiveFighters()
    //{
    //    foreach (var fighter in fighterSlots)
    //    {
    //        if(bIsDead) continue;

    //        Vector3 DeployPosition = GetRandomPointInRange(stationTransform.position, minSpawnRadius, maxSpawnRadius);

    //        fighter.SetActive(true);
    //        fighter.transform.position = DeployPosition;

    //        //Assign Station Transform
    //        //Assign Target Transform

    //        //Set Fighter Behaviour to "Pursuit"
    //    }
    //}

    //private void RecalFighters()
    //{
    //    foreach (var fighter in fighterSlots)
    //    {
    //        if(bIsDead) continue;
    //        if(bIsDocked) continue;

    //        //Set Fighter Behaviour to "Return"
    //    }
    //}

    //private void OnFighterDocked(GameObject fighter)
    //{
        
    //}

    //private void ReplenishDeadFighters()
    //{
    //    foreach (var fighter in fighterSlots)
    //    {
    //        if(!bIsDead) continue;

    //        bIsDead = false;
    //    }
    //}

    //private void BuildFighterPool()
    //{
    //    fighterSlots = new List<FighterSlot>();

    //    for (int i = 0; i < fightersAmount; i++)
    //    {
    //        GameObject fighterPrefab = fighterPrefabs[UnityEngine.Random.Range(0, fighterPrefabs.Length)];
    //        GameObject fighter = Instantiate(fighterPrefab, stationTransform.position, Quaternion.identity);

    //        fighter.transform.SetParent(fighterContainerTransform, true);

    //        fighter.transform.position = fighterContainerTransform.position;
    //        fighter.SetActive(false);

    //        FighterManager fighterManager = fighter.GetComponent<FighterManager>();
    //        fighterManager.
    //    }
    //}



    //private static Vector3 GetRandomPointInRange(Vector3 center, float minRadius, float maxRadius)
    //{
    //    float radius = UnityEngine.Random.Range(minRadius, maxRadius);
    //    float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;

    //    float x = Mathf.Cos(angle) * radius;
    //    float z = Mathf.Sin(angle) * radius;

    //    return new Vector3(center.x + x, center.y, center.z + z);

    //}


}
