using System;
using System.Collections.Generic;
using UnityEngine;
using TypeSelector;
using static Unity.VisualScripting.Metadata;


[Serializable]
public struct NodeEntry
{
    [SerializeReference, TypeSelector(DrawMode.NoFoldout)]
    public NodeData node;

}

public enum NodeType
{
    Leaf,
    Sequence,
    Selector
}

public enum StrategyType
{
    Condition,
    Action
}

[Serializable, TypeSelectorName("Node")]
public abstract class NodeData
{
    public string name;
    [HideInInspector]public NodeType type;

}

[Serializable]
public class SequenceData : NodeData
{
    public List<NodeEntry> children;
    public SequenceData() 
    { 
        children = new List<NodeEntry>(); 
        type = NodeType.Sequence;
    }
}

[Serializable]
public class SelectorData : NodeData
{
    public List<NodeEntry> children;
    public SelectorData()
    {
        children = new List<NodeEntry>();
        type = NodeType.Selector;
    }
}

public enum StrategyTarget
{
    Self,
    Detector,
    UnitManager
}

[Serializable]
public class LeafData : NodeData
{
    public StrategyType strategyType;
    public StrategyTarget strategyTarget;
    public string strategyName;

    public LeafData()
    {
        type = NodeType.Leaf;
    }
}

[CreateAssetMenu(fileName = "BehaviourTreeData", menuName = "Scriptable Objects/BehaviourTreeData")]
public class BehaviourTreeData : ScriptableObject
{
    public List<NodeEntry> rootChildren;
}
