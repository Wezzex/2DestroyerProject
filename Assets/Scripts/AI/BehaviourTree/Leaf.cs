using UnityEngine;
using System.Collections.Generic;

public class Leaf : Node
{
    readonly IStrategy strategy;

    public Leaf(string name, IStrategy strategy, int priority = 0) : base(name, priority)
    {
        this.strategy = strategy;
    }

    public override Status Process()
    {
        return strategy.Process();
    }

    public override void Reset()
    {
        strategy.Reset();   
    }
}
