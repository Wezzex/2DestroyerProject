using UnityEngine;

public class RepeatUntilSuccess : Node
{
    public RepeatUntilSuccess(string name) : base(name) { }

    public override Status Process()
    {
        if (children[0].Process() == Status.Success)
        {
            Reset();
            return Status.Success;
        }

        return Status.Running;
    }
}
