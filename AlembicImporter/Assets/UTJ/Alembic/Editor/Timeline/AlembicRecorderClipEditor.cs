using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace UTJ.Alembic
{
    [CustomEditor(typeof(AlembicRecorderClip))]
    public class AlembicRecorderClipEditor : AlembicRecorderSettingsEditor
    {
        public override void OnInspectorGUI()
        {
            var t = target as AlembicRecorderClip;
            var settings = t.settings;
            var so = serializedObject;

            var pathSettings = "m_settings";
            DrawAlembicSettings(settings, so, pathSettings);
            DrawCaptureSettings(settings, so, pathSettings);
            {
                EditorGUILayout.PropertyField(so.FindProperty("m_ignoreFirstFrame"));
                GUILayout.Space(5);
            }
            DrawMiscSettings(settings, so, pathSettings);

            so.ApplyModifiedProperties();
        }
    }
}
