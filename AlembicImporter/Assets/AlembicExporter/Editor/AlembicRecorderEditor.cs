using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AlembicRecorder))]
public class AlembicRecorderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var t = target as AlembicRecorder;
        if(t.isRecording)
        {
            if (GUILayout.Button("End Recording"))
            {
                t.EndRecording();
            }
        }
        else
        {
            if (GUILayout.Button("Begin Recording"))
            {
                t.BeginRecording();
            }

            if (GUILayout.Button("One Shot"))
            {
                t.OneShot();
            }
        }
    }
}
