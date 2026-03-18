using UnityEngine;

public class RepeatUntilFail : Node
{
    public RepeatUntilFail(string name) : base(name) { }

    public override Status Process()
    {
        if (children[0].Process() == Status.Failure)
        { 
            Reset(); 
            return Status.Failure;
        }

        return Status.Running;
    }
}
