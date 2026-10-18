using UnityEngine;

// Runs its children in order within one tick until one fails or is still running.
// Re-evaluated from the first child every tick, so its conditions are checked every frame.
// (It used to advance only one child per tick, so an action ran just one frame in every few.)
public class Sequence : Node
{
    public Sequence(string name, int priority = 0) : base(name, priority) { }

    public override Status Process()
    {
        for (int i = 0; i < children.Count; i++)
        {
            Status status = children[i].Process();
            if (status != Status.Success) return status;
        }

        return Status.Success;
    }
}
