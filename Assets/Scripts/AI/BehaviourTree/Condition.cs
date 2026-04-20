using System;
using UnityEngine;

public class Condition : IStrategy
{
    readonly Func<bool> predicate;
    private Func<bool> value;

    public Condition(Func<bool> predicate)
    {
        this.predicate = predicate;
    }

    public Node.Status Process() => predicate() ? Node.Status.Success : Node.Status.Failure;

}
