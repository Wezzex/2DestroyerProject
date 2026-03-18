using UnityEngine;

public class DefaultEnemyAI : MonoBehaviour
{

    [SerializeField] private AIBehavior shootBehaviour, patrolBehaviour;

    [SerializeField] private UnitManager unitManager;
    [SerializeField] private ShipController shipController;
    [SerializeField] private AIDetector aIDetector;

    BehaviourTree behaviourTree;

    private void Awake()
    {
        aIDetector = GetComponentInChildren<AIDetector>();
        shipController = GetComponentInChildren<ShipController>();
        unitManager = GetComponentInChildren<UnitManager>();

        behaviourTree = new BehaviourTree("DestroyerBehaviourTree");


        BuildTree();
    }

    private void BuildTree()
    {
        //Builds Tree Root witch is a Selector
        Selector root = new Selector("Root");

        Sequence attackSequence = new Sequence("AttackSequence");
        attackSequence.AddChild(new Leaf("Target Visible?",new Condition(() => aIDetector.TargetVisible)));
        attackSequence.AddChild(new Leaf("Shoot Target?",new ActionStrategy(() => shootBehaviour.PerformAction(shipController, aIDetector))));

        Sequence patrol = new Sequence("PatrolSequence");

        patrol.AddChild(new Leaf("Patrol", new ActionStrategy(() => patrolBehaviour.PerformAction(shipController, aIDetector))));

        root.AddChild(attackSequence);
        root.AddChild(patrol);

        behaviourTree.AddChild(root);

    }

    private void Update()
    {
        if(unitManager.IsDead) return;

        behaviourTree.Process();
    }
}
