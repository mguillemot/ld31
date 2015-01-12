using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (Monster))]
[CanEditMultipleObjects]
public class MonsterInspector : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (Application.isPlaying)
        {
            var t = (Monster)target;

            GUILayout.Space(15);
            EditorGUILayout.TextField("Game position", t.currentX + ":" + t.currentY);
            EditorGUILayout.TextField("State", t.state.ToString());
            EditorGUILayout.ObjectField("Aggro", t.aggro, typeof(Player), true);
            EditorGUILayout.TextField("Cannot aggro before", (t.cantAggroBefore - Time.time).ToString("0.0"));
            if (t.hasPath)
            {
                EditorGUILayout.TextField("Path", t.path.PathRepr());
            }
            else
            {
                EditorGUILayout.TextField("Path", "-");
            }
        }
    }

}
