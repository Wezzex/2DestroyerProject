using Unity.VisualScripting;
using UnityEngine;

public abstract class AIContext
{
    ShipController shipController;
    AIDetector detector;
    UnitManager unitManager;
    GlobalPathPlaner globalPathPlaner;
    FollowPlannedPath FollowPlannedPath;
    Transform selfTransform;
}
