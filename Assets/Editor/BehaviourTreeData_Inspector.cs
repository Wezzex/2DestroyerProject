using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(BehaviourTreeData))]
public class BehaviourTreeData_Inspector : Editor
{

    public override void OnInspectorGUI()
    {

        if (!Application.IsPlaying(target))
        {
            if (GUILayout.Button("Edit"))
            {
                BehaviourTreeData treeData = (BehaviourTreeData)target;

                BehaviourTreeEditor.dataToEdit = treeData;

                EditorWindow.GetWindow<BehaviourTreeEditor>().Show();
            }
        }
    }
}