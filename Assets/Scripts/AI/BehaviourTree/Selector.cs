using UnityEngine;

// Tries its children in priority order within one tick and stops at the first that doesn't fail.
// Re-evaluated from the top every tick, so a higher-priority branch (Return, Break, Formation) takes
// over the moment its condition becomes true. (It used to stick with whichever child was running and
// move on only one child per tick.)
public class Selector : Node
{
    public Selector(string name, int priority = 0) : base(name, priority) { }

    public override Status Process()
    {
        for (int i = 0; i < children.Count; i++)
        {
            Status status = children[i].Process();
            if (status != Status.Failure) return status;
        }

        return Status.Failure;
    }
}
