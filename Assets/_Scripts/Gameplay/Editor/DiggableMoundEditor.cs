using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DiggableMound))]
[CanEditMultipleObjects]
public class DiggableMoundEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Reset Mound"))
            {
                foreach (Object selected in targets)
                    ((DiggableMound)selected).ResetMound();
            }
        }
    }
}
