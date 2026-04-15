using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public struct NodeEntry
{
    [SerializeReference]
    public NodeData node;

}

public enum NodeType
{
    Leaf,
    Sequence,
    Selector
}

[Serializable]
public class NodeData
{
    public string name;


}

[Serializable]
public class SequenceData : NodeData
{

    public List<NodeEntry> children;
}

[Serializable]
public class SelectorData : NodeData
{

    public List<NodeEntry> children;
}

[CreateAssetMenu(fileName = "BehaviourTreeData", menuName = "Scriptable Objects/BehaviourTreeData")]
public class BehaviourTreeData : ScriptableObject
{
    public List<NodeEntry> rootChildren;


}
