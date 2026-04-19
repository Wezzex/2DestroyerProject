using System;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyAIRunner : MonoBehaviour
{
    BehaviourTree behaviourTree;
    [SerializeField] private BehaviourTreeData behaviourTreeData;
    [SerializeField] private AIContext context;

    private void Awake()
    {
        if (context == null)
        {
            context = GetComponent<AIContext>();
        }

        behaviourTree = new BehaviourTree(gameObject.name + ("_BT"));
    }

    private void Start()
    {
        if (behaviourTreeData == null)
        {
            Debug.LogError($"[EnemyRunner.Start]: BehaviourTree for {behaviourTree.name} is missing");
            return;
        }

        BuildTree();
    }

    private void Update()
    {
        if(!context.IsAlive()) return;

        behaviourTree.Process();
    }
    private IStrategy CreateStrategy(LeafData leafData)
    {


        switch (leafData.strategyType)
        {
            case StrategyType.Condition:

                var foundCondition = FindCondition(leafData.strategyName);
                if (foundCondition == null)
                {
                    Debug.LogError("[EnemyAIRunner.CreateStrategy()]: Condition not Found");
                    return null;
                }

                return new Condition(foundCondition);

            case StrategyType.Action:
                {
                    var foundAction = FindAction(leafData.strategyName);
                    if (foundAction == null)
                    {
                        Debug.LogError("[EnemyAIRunner.CreateStrategy()]: Action not Found");
                        return new ActionStrategy(() => { });
                    }

                    return new ActionStrategy(foundAction);
                }
            default:
                return null;
                
        }
    }

    private Action FindAction(string strategyName)
    {
        return context != null ? context.GetAction(strategyName) : null;
    }

    private Func<bool> FindCondition(string strategyName)
    {
        return context != null ? context.GetCondition(strategyName) : null;
    }

    private Node CreateNode(NodeData nodeData)
    {
        if (nodeData == null)
        {
            Debug.LogError("[EnemyAIRunner.CreateNode()] NodeData is null ");
            return null;

        }

        switch (nodeData.type)
        {
            case NodeType.Sequence:

                {
                    var sequenceData = nodeData as SequenceData;
                    var sequence = new Sequence(nodeData.name);
                    foreach (var childEntry in sequenceData.children)
                    {
                        var childNode = CreateNode(childEntry.node);
                        if(childNode != null) sequence.AddChild(childNode);
                    }
                return sequence;
                }

            case NodeType.Selector:

                {
                    var selectorData = nodeData as SelectorData;
                    var selector = new Selector(nodeData.name);
                    foreach (var childEntry in selectorData.children)
                    {
                        var childNode = CreateNode(childEntry.node);
                        if (childNode != null) selector.AddChild(childNode);
                    }
                    return selector;
                }

            case NodeType.Leaf:

                {
                    var leafData = nodeData as LeafData;
                    var strategy = CreateStrategy(leafData);
                    return new Leaf(leafData.name, strategy);
                }


        }

        return null;
    }

    private void BuildTree()
    {
        if (behaviourTreeData == null)
        {
            Debug.LogError($"[EnemyAIRunner.BuildTree()]: No BehaviourTreeData assigned on {name} ");
            return;
        }

        behaviourTree.children.Clear();

        foreach (var nodeEntry in behaviourTreeData.rootChildren)
        {
            var node = CreateNode(nodeEntry.node);
            if (node != null)
            {
                behaviourTree.AddChild(node);
            }
        }

        Debug.Log("<color =#00ff00ff>"+ $"[EnemyAIRunner.BuildTree]: Built Tree from {behaviourTreeData.name} for {name}</c>" + "</color>");
    }
}
