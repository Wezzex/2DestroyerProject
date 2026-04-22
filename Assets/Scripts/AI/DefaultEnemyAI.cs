using System;
using UnityEngine;
using UnityEngine.AI;

public abstract class DefaultEnemyAI : MonoBehaviour
{
    [Header("Refrence")]
    [SerializeField] protected AIBehavior shootBehaviour, patrolBehaviour;

    
    [SerializeField] protected UnitManager unitManager;
    [SerializeField] protected ShipController shipController;
    [SerializeField] protected AIDetector aIDetector;

                     protected BehaviourTree behaviourTree;


    private void Awake()
    {

        
    }
    private void Start()
    {

        aIDetector = GetComponentInChildren<AIDetector>();
        shipController = GetComponentInChildren<ShipController>();
        unitManager = GetComponentInChildren<UnitManager>();

        BuildTree();
    }
    public virtual void BuildTree()
    {
        

    }

    

    private void Update()
    {
        if (unitManager.IsDead)
        {
            Debug.Log("Unit is Dead");
            return;
        }
        behaviourTree.Process();
    }
}
