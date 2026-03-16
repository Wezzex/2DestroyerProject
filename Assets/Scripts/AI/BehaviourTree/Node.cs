using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

public class Node : MonoBehaviour
{
    public enum Status { Success, Failure, Running}

    public readonly string name;

    public readonly List<Node> children = new();
    protected int currentChild;


    public Node(string name = "Node")
    {
        this.name = name;
    }

    public void AddChild(Node child)
    {
        children.Add(child);
    }

    public virtual Status Process()
    {
        return children[currentChild].Process();
    }

    public virtual void Reset()
    {
        currentChild = 0;

        foreach (var child in children)
        {
            child.Reset();
        }
    }
}
