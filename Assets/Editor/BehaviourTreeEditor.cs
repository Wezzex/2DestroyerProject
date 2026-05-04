using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public sealed class BehaviourTreeEditor : EditorWindow
{
    public static BehaviourTreeData dataToEdit;

    [MenuItem("Testing/Editor Window Example")]
    private static void Open()
    {
        GetWindow<BehaviourTreeEditor>().Show();
    }

    private BehaviourTreeData behaviourTreeData;

    private SerializedObject _serializedObject;

    private void OnEnable()
    {
        if(dataToEdit == null) return;
        behaviourTreeData = dataToEdit;   
        _serializedObject = new SerializedObject(behaviourTreeData);
    }

    private void OnDestroy()
    {
        _serializedObject.Dispose();
    }

    private void CreateGUI()
    {
        if(behaviourTreeData == null || _serializedObject == null) return;
        var root = this.rootVisualElement;
        var property = _serializedObject.FindProperty(nameof(behaviourTreeData.rootChildren));
        var propertyField = new PropertyField(property, "Example");
        propertyField.Bind(_serializedObject);
        root.StretchToParentSize();
        root.Add(propertyField);
    }
}