using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class PrioritySelector : Node
{
    List<Node> sortedChildren;
    List<Node> SortedChildren => sortedChildren ??= SortChildren();

    protected virtual List<Node> SortChildren() => children.OrderByDescending(child => child.priority).ToList();

    public PrioritySelector(string name) : base(name) { }

    public override void Reset()
    {
        base.Reset();
        sortedChildren = null;
    }

    public override Status Process()
    {
        foreach (var child in SortedChildren)
        {
            switch (child.Process())
            {
                case Status.Running:
                    return Status.Running;
                case Status.Success:
                    return Status.Success;
                default:
                    continue;
            
            }
        }
        return Status.Failure;
    }
}
