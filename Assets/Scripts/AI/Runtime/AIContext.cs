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

    [Header("Presuit Settings")]
    public bool PresuitTarget { get; private set; }
    [SerializeField] private float persuitRange = 100f;
    [SerializeField] private float stationOutOfReach = 75f;


    [Header("Fire At Target Settings")]
    public bool FireAtTarget { get; private set; }
    [SerializeField] private float fireRange = 50f;


    [Header("Hold Position Settings")]
    public bool HoldPosition { get; private set; }
    [SerializeField] private float toCloseRange = 25f;

    private void Awake()
    {

    }

    private void Start()
    {


        if (shipController == null) shipController = GetComponent<ShipController>();
        if (detector == null) detector = GetComponentInChildren<AIDetector>();
        if (unitManager == null) unitManager = GetComponentInChildren<UnitManager>();


        if (patrolBehaviour == null) patrolBehaviour = GetComponent<AIPatrolBehaviour>();
        if (shootBehaviour == null) shootBehaviour = GetComponent<AIShootBehaviour>();

        BuildDictionaryMap();

    }

    private void BuildDictionaryMap()
    {

        //conditions = new Dictionary<string, Func<bool>>()
        //{
        //    { "CanDetectTarget", ()=> CanDetectTarget()},
        //    { "CanFireAtTarget", ()=> CanFireAtTarget()},
        //    { "ShouldHoldPosition()", ()=> ShouldHoldPosition()},

        //    { "IsAlive", ()=> IsAlive()}
        //};

        behaviours = new Dictionary<string, AIBehavior>();

        var behavioursList = GetComponents<AIBehavior>();
        foreach (var behaviour in behavioursList)
        {
            behaviours.Add(behaviour.Name, behaviour);
        }
    }

    private void ShouldHoldPosition()
    {
        if (detector.TargetVisible == false) return;

        Vector3 targetPosition = detector.Target.position;
        Vector3 unitPosition = detector.transform.position;

        if (Vector3.Distance(unitPosition, targetPosition) < toCloseRange)
        {
            HoldPosition = true;
        }

        HoldPosition = false;
    }

    private void CanFireAtTarget()
    {
        if (detector.TargetVisible == false) return;

        Vector3 targetPosition = detector.Target.position;
        Vector3 unitPosition = detector.transform.position;

        if (Vector3.Distance(unitPosition, targetPosition) < fireRange)
        {
            HoldPosition = true;
        }

        HoldPosition = false;
    }

    private void CanPursuitTarget()
    {
        if (detector.TargetVisible == false) return;

        Vector3 targetPosition = detector.Target.position;
        Vector3 unitPosition = detector.transform.position;

        if (Vector3.Distance(unitPosition, targetPosition) < persuitRange)
        {
            HoldPosition = true;
        }

        HoldPosition = false;
    }

    public bool CanDetectTarget()
    {
        return detector != null && detector.TargetVisible;
    }

    public AIBehavior FindBehaviour(string name)
    {
        behaviours.TryGetValue(name, out var behaviour);
        return behaviour;
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
