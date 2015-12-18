using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AlembicExporter))]
public class AlembicExporterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var t = target as AlembicExporter;

        if (t.isRecording)
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
