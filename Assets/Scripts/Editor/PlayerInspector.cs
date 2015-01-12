using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (Player))]
[CanEditMultipleObjects]
public class PlayerInspector : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (Application.isPlaying)
        {
            var t = (Player) target;

            GUILayout.Space(15);
            EditorGUILayout.TextField("Game position", t.currentX + ":" + t.currentY);

            foreach (var area in Field.instance.posPerArea.Keys)
            {
                if (GUILayout.Button("Move to " + area))
                {
                    t.ProcessInput("GO " + area);
                }
            }
            if (GUILayout.Button("Greetings"))
            {
                t.ProcessInput("Greetings, traveller! " + Time.frameCount);
            }
            if (GUILayout.Button("Pickup"))
            {
                t.ProcessInput("PICKUP");
            }
            if (GUILayout.Button("Inventory?"))
            {
                t.ProcessInput("INVENTORY");
            }
            if (GUILayout.Button("Gain XP"))
            {
                t.GainExperience(15);
            }
        }
    }

}
