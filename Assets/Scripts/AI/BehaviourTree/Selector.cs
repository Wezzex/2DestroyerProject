using UnityEngine;

public class Selector : Node
{
    public Selector(string name) : base(name) { }

    public override Status Process()
    {
        if (currentChild < children.Count)
        {
            switch (children[currentChild].Process())
            {
                case Status.Running:
                    return Status.Running;

                case Status.Success:
                    Reset();
                    return Status.Success;

                default:
                    currentChild++;
                    return Status.Running;
            }
        }
        Reset();
        return Status.Failure;
    }
}
