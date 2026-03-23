using UnityEngine;
using UnityEngine.AI;

public class DefaultEnemyAI : MonoBehaviour
{

    [SerializeField] private AIBehavior shootBehaviour, patrolBehaviour;

    [SerializeField] private UnitManager unitManager;
    [SerializeField] private ShipController shipController;
    [SerializeField] private AIDetector aIDetector;

    BehaviourTree behaviourTree;
    NavMeshAgent agent;
    public  bool bInRange;


    private void Awake()
    {
        aIDetector = GetComponentInChildren<AIDetector>();
        shipController = GetComponentInChildren<ShipController>();
        unitManager = GetComponentInChildren<UnitManager>();
        agent = GetComponent<NavMeshAgent>();


        behaviourTree = new BehaviourTree("DestroyerBehaviourTree");
    }
    private void Start()
    {
        BuildTree();
    }
    private void BuildTree()
    {
        //Builds Tree Root witch is a Selector
        PrioritySelector root = new PrioritySelector("Root");
        behaviourTree.AddChild(root);

        Sequence attackSequence = new Sequence("AttackSequence", 100);
        attackSequence.AddChild(new Leaf("Target Visible?", new Condition(() => TargetInRange())));
        attackSequence.AddChild(new Leaf("Shoot Target?", new ActionStrategy(() => shootBehaviour.PerformAction(shipController, aIDetector))));
        root.AddChild(attackSequence);

        Sequence patrol = new Sequence("PatrolSequence", 50);
        patrol.AddChild(new Leaf("Patrol", new ActionStrategy(() => patrolBehaviour.PerformAction(shipController, aIDetector))));
        root.AddChild(patrol);

        Utility.LogInfo("Tree Built");


    }

    bool TargetInRange()
    {
        if (aIDetector.TargetVisible)
        {
            bInRange = true;
            return true;

        }
        bInRange = false;
        return false;

    }

    private void Update()
    {
        if(unitManager.IsDead) return;

        TargetInRange();
        behaviourTree.Process();
    }
}
