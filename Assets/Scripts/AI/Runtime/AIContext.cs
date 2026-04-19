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
    [SerializeField] private AIPatrolPathBehaviour patrolPathBehaviour;
    [SerializeField] private AIShootBehaviour shootBehaviour;

    private Dictionary<string, Func<bool>> conditions;
    private Dictionary<string, Action> actions;

    private void Awake()
    {
        if (shipController == null)         shipController = GetComponent<ShipController>();
        if (detector == null)               detector = GetComponentInChildren<AIDetector>();
        if (unitManager == null)            unitManager = GetComponent<UnitManager>();


        if (patrolPathBehaviour == null)  patrolPathBehaviour = GetComponent<AIPatrolPathBehaviour>();
        if (shootBehaviour == null)       shootBehaviour = GetComponent<AIShootBehaviour>();

    }

    private void Start()
    {
        

        BuildDictionaryMap();
    }

    private void BuildDictionaryMap()
    {

        conditions = new Dictionary<string, Func<bool>>()
        {
            { "TargetVisible", ()=> detector != null && detector.TargetVisible},
            { "HasTarget", ()=> detector != null && detector.Target != null},
            { "IsAlive", ()=> unitManager != null && !unitManager.IsDead}
        };

        actions = new Dictionary<string, Action>()
        {
            //ShootAction
            { "Shoot", () =>
                {
                    if(shootBehaviour != null && shipController != null && detector != null)
                    {
                        shootBehaviour.PerformAction(shipController, detector);
                    }
                }
            },
            //PatrolAction
            {
                "Patrol", () =>
                {
                    if(patrolPathBehaviour != null && shipController != null && detector != null)
                    {
                        patrolPathBehaviour.PerformAction(shipController, detector);
                    }
                }
            },
            //StopShootingAction
            { "StopShooting", () =>
                {
                    if(shipController != null) shipController.SetShootingState(false);
                }
            },
        };
    }

    public bool TargetVisible()
    {
        if (detector.TargetVisible)
        {
            return true;
        }

        return false;
    }

    public void ShootAction()
    {
        shootBehaviour.PerformAction(shipController, detector);
    }

    public void PatrolAction()
    {
        patrolPathBehaviour.PerformAction(shipController, detector);
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
