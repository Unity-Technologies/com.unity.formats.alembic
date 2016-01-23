using System;
using UnityEditor;
using UnityEngine;

namespace UTJ
{
    [CustomEditor(typeof(AlembicColorBalls))]
    public class AlembicColorBallsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
    
            var t = target as AlembicColorBalls;

            if (GUILayout.Button("Export"))
            {
                t.DoExport();
            }
        }
    }
}
