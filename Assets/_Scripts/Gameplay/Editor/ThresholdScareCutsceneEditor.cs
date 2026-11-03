using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ThresholdScareCutscene))]
public class ThresholdScareCutsceneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        Transform point = serializedObject.FindProperty("cloverSpawnPoint").objectReferenceValue as Transform;
        if (point != null && GUILayout.Button("Select Clover Spawn Point"))
        {
            Selection.activeTransform = point;
            EditorGUIUtility.PingObject(point);
            Tools.current = Tool.Move;
        }
    }

    private void OnSceneGUI()
    {
        Transform point = serializedObject.FindProperty("cloverSpawnPoint").objectReferenceValue as Transform;
        if (point == null) return;

        Vector3 spawnPosition = point.position + serializedObject.FindProperty("cloverOffset").vector3Value;
        SceneView view = SceneView.currentDrawingSceneView;
        Handles.color = Color.magenta;
        Handles.Label(spawnPosition + Vector3.up * 2f, "Clover spawn");
        Handles.DrawWireDisc(spawnPosition, Vector3.up, 0.3f);
        Handles.DrawLine(spawnPosition, spawnPosition + Vector3.up * 1.6f);
        Handles.DrawWireDisc(spawnPosition + Vector3.up * 1.8f, view != null ? view.camera.transform.forward : Vector3.forward, 0.2f);
        Handles.ArrowHandleCap(0, spawnPosition + Vector3.up * 0.1f, point.rotation, 0.7f, EventType.Repaint);

        EditorGUI.BeginChangeCheck();
        if (Tools.current == Tool.Rotate)
        {
            Quaternion rotation = Handles.RotationHandle(point.rotation, point.position);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(point, "Rotate Clover Spawn Point");
                point.rotation = rotation;
                PrefabUtility.RecordPrefabInstancePropertyModifications(point);
            }
        }
        else
        {
            Quaternion orientation = Tools.pivotRotation == PivotRotation.Local ? point.rotation : Quaternion.identity;
            Vector3 position = Handles.PositionHandle(point.position, orientation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(point, "Move Clover Spawn Point");
                point.position = position;
                PrefabUtility.RecordPrefabInstancePropertyModifications(point);
            }
        }
    }
}
