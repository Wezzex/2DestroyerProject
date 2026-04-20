using System;
using UnityEngine;

public abstract class AIBehavior : MonoBehaviour
{
    public abstract string Name { get; }
    public abstract void PerformAction(ShipController shipController, AIDetector aIDetector);
}