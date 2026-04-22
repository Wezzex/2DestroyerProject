using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AIContext : MonoBehaviour
{
    [Header("Refrences")]
    [SerializeField] private ShipController shipController;
    [SerializeField] private AIDetector detector;
    [SerializeField] private UnitManager unitManager;

    [Header("Behaviour components")]
    [SerializeField] private AIBehavior patrolBehaviour, shootBehaviour;

    private Dictionary<string, Func<bool>> conditions;
    private Dictionary<string, Action> actions;

    private Dictionary<string, AIBehavior> behaviours;
    public ShipController ShipController => shipController;
    public AIDetector Detector => detector;

    public UnitManager UnitManager => unitManager;

    private void Awake()
    {

    }

    private void Start()
    {


        if (shipController == null) shipController = GetComponent<ShipController>();
        if (detector == null) detector = GetComponentInChildren<AIDetector>();
        if (unitManager == null) unitManager = GetComponent<UnitManager>();


        if (patrolBehaviour == null) patrolBehaviour = GetComponent<AIPatrolBehaviour>();
        if (shootBehaviour == null) shootBehaviour = GetComponent<AIShootBehaviour>();

        BuildDictionaryMap();

    }

    private void BuildDictionaryMap()
    {

        //conditions = new Dictionary<string, Func<bool>>()
        //{
        //    { "TargetVisible", ()=> detector != null && detector.TargetVisible},
        //    { "HasTarget", ()=> detector != null && detector.Target != null},
        //    { "IsAlive", ()=> unitManager != null && !unitManager.IsDead}
        //};

        //actions = new Dictionary<string, Action>()
        //{
        //    //ShootAction
        //    { "Shoot", () =>
        //        {
        //            if(shootBehaviour != null && shipController != null && detector != null)
        //            {
        //                shootBehaviour.PerformAction(shipController, detector);
        //            }
        //        }
        //    },
        //    //PatrolAction
        //    {
        //        "Patrol", () =>
        //        {
        //            if(patrolBehaviour != null && shipController != null && detector != null)
        //            {
        //                patrolBehaviour.PerformAction(shipController, detector);
        //            }
        //        }
        //    },
        //    //StopShootingAction
        //    { "StopShooting", () =>
        //        {
        //            if(shipController != null) shipController.SetShootingState(false);
        //        }
        //    },
        //};

        behaviours = new Dictionary<string, AIBehavior>();

        var behavioursList = GetComponents<AIBehavior>();
        foreach (var behaviour in behavioursList)
        {
            behaviours.Add(behaviour.Name, behaviour);
        }
    }

    public AIBehavior FindBehaviour(string name)
    {
        behaviours.TryGetValue(name, out var behaviour);
        return behaviour;
    }

    public bool TargetVisible()
    {
        return detector != null && detector.TargetVisible;
    }

    public void ShootAction()
    {
        shootBehaviour.PerformAction(shipController, detector);
    }

    public void PatrolAction()
    {
        patrolBehaviour.PerformAction(shipController, detector);
    }

    public Func<bool> GetCondition(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return conditions.TryGetValue(id, out var findCondition) ? findCondition : null;
    }

    public Action GetAction(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return actions.TryGetValue(id, out var findAction) ? findAction : null;
    }

    public bool IsAlive()
    {
        if (unitManager.IsDead)
        {
            return false;
        }
        return true;

    }

}
