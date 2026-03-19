using UnityEngine;

public class DefaultEnemyAI : MonoBehaviour
{

    [SerializeField] private AIBehavior shootBehaviour, patrolBehaviour;

    [SerializeField] private UnitManager unitManager;
    [SerializeField] private ShipController shipController;
    [SerializeField] private AIDetector aIDetector;

    BehaviourTree behaviourTree;

    bool bInRange;


    private void Awake()
    {
        aIDetector = GetComponentInChildren<AIDetector>();
        shipController = GetComponentInChildren<ShipController>();
        unitManager = GetComponentInChildren<UnitManager>();


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

        Sequence attackSequence = new Sequence("AttackSequence");
        attackSequence.AddChild(new Leaf("Target Visible?", new Condition(() => aIDetector.TargetVisible)));
        attackSequence.AddChild(new Leaf("Shoot Target?", new ActionStrategy(() => shootBehaviour.PerformAction(shipController, aIDetector))));
        root.AddChild(attackSequence);

        Sequence patrol = new Sequence("PatrolSequence");
        patrol.AddChild(new Leaf("Patrol", new ActionStrategy(() => patrolBehaviour.PerformAction(shipController, aIDetector))));
        root.AddChild(patrol);

        


        Utility.LogInfo("Tree Built");

    }

    private void Update()
    {
        if(unitManager.IsDead) return;
        behaviourTree.Process();
    }
}
