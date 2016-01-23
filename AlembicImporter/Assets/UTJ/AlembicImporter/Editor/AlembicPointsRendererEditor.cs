using System;
using UnityEditor;
using UnityEngine;

namespace UTJ
{
    [CustomEditor(typeof(AlembicPointsRenderer))]
    public class AlembicPointsRendererEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var t = target as AlembicPointsRenderer;
            if (GUILayout.Button("Apply Material"))
            {
                t.RefleshMaterials();
            }
        }
    }
}
