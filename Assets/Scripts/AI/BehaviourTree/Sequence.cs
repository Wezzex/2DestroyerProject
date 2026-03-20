using UnityEngine;

public class Sequence : Node
{
    public Sequence(string name, int priority = 0) : base(name, priority) { }

    public override Status Process()
    {

        while (currentChild < children.Count)
        {
            var status = children[currentChild].Process();


            switch (children[currentChild].Process())
            {
                case Status.Running:
                    return Status.Running;

                case Status.Failure:
                    Reset();
                    return Status.Failure;

                case Status.Success:
                    currentChild++;
                    return currentChild == children.Count ? Status.Success : Status.Running;
                    
            }

        }
            
        Reset();
        return Status.Success;
    }
}
