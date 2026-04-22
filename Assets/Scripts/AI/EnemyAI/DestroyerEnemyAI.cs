using UnityEngine;

public class DestroyerEnemyAI : DefaultEnemyAI
{
    [Header("In Range Settigns")]
    private bool bInRange;

    [Header("Fire Range Settings")]
    [SerializeField] protected Transform destroyerTransform;
    [SerializeField] protected float destroyerFieringRadius = 50f;
    private bool bInFireRange = false;

    [Header("Hold Position Settings")]
    [SerializeField] private float destroyerHoldPositionRange = 50f;
    private bool bShouldHoldPosition = false;


    private void Awake()
    {
        behaviourTree = new BehaviourTree("DestroyerBehaviourTree");
    }

    private void Start()
    {
        
    }
    public override void BuildTree()
    {
        //Builds Tree Root witch is a Selector
        PrioritySelector root = new PrioritySelector("Root");
        behaviourTree.AddChild(root);

        Sequence attackSequence = new Sequence("AttackSequence", 100);
        attackSequence.AddChild(new Leaf("Target Visible?", new Condition(() => CanDetectTarget())));
        attackSequence.AddChild(new Leaf("Shoot Target?", new ActionStrategy(() => shootBehaviour.PerformAction(shipController, aIDetector))));
        root.AddChild(attackSequence);

        Sequence patrol = new Sequence("PatrolSequence", 50);
        patrol.AddChild(new Leaf("Patrol", new ActionStrategy(() => patrolBehaviour.PerformAction(shipController, aIDetector))));
        root.AddChild(patrol);

        Utility.LogInfo("Tree Built");
    }

    private bool CanDetectTarget()
    {
        if (aIDetector.TargetVisible)
        {
            bInRange = true;
            return true;

        }
        bInRange = false;
        return false;

    }

    private bool CanFireAtTarget()
    {
        if (Vector3.Distance(destroyerTransform.transform.position, aIDetector.Target.transform.position) < destroyerFieringRadius)
        {
            bInFireRange = true;
            return true;
        }

        bInFireRange = false;

        return false;
    }

    private bool ShouldHoldPosition()
    {
        if (Vector3.Distance(destroyerTransform.transform.position, aIDetector.Target.transform.position) < destroyerHoldPositionRange)
        {
            bShouldHoldPosition = true;
            return true;
        }

        bShouldHoldPosition = false;

        return false;
    }

}
