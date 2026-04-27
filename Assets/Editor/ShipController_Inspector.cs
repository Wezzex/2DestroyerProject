using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(ShipController))]
public class ShipController_Inspector : Editor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (Application.IsPlaying(target))
        {
            if (GUILayout.Button("Set as DebugTarget"))
            {
                ShipController controller = (ShipController)target;
                Utility.AIDebugTarget = controller;
            }
        }
    }
}
