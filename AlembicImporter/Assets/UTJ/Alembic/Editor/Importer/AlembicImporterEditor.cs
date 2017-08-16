#if UNITY_2017_1_OR_NEWER

using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;

namespace UTJ.Alembic
{
    [CustomEditor(typeof(AlembicImporter))]
    public class AlembicImporterEditor : ScriptedImporterEditor
    {

        public override void OnInspectorGUI()
        {
            var importer = serializedObject.targetObject as AlembicImporter;
            var settings = importer.m_ImportSettings;
            var serSettings = serializedObject.FindProperty(() => importer.m_ImportSettings);

            AddEnumProperty(serializedObject.FindProperty(() => importer.m_importMode), "Import Mode", "", typeof(AlembicImportMode));

            AddBoolProperty(serSettings.FindPropertyRelative(() => settings.m_swapHandedness), "Swap handedness", "");
            AddBoolProperty(serSettings.FindPropertyRelative(() => settings.m_swapFaceWinding), "Swap face winding", "");
            AddBoolProperty(serSettings.FindPropertyRelative(() => settings.m_submeshPerUVTile), "Submesh per UV tile", "");
            AddBoolProperty(serSettings.FindPropertyRelative(() => settings.m_shareVertices), "Merge Vertices (experimental)", "Allow vertex sharing between faces when possible.");
            AddBoolProperty(serSettings.FindPropertyRelative(() => settings.m_treatVertexExtraDataAsStatics), "Vertex extra data is static (exp.)", "When set, UV's/normals/tangents are fetched from file only on topology change event.");
            AddFloatProperty(serSettings.FindPropertyRelative(() => settings.m_scaleFactor), "Scale factor", "Yo!");

            AddEnumProperty(serSettings.FindPropertyRelative(() => settings.m_normalsMode), "Normals mode", "", settings.m_normalsMode.GetType());
            AddEnumProperty(serSettings.FindPropertyRelative(() => settings.m_tangentsMode), "Tangent mode", "", settings.m_tangentsMode.GetType());
            AddEnumProperty(serSettings.FindPropertyRelative(() => settings.m_aspectRatioMode), "Aspect ratio mode", "", settings.m_aspectRatioMode.GetType());
            AddBoolProperty(serSettings.FindPropertyRelative(() => settings.m_useThreads), "Use threads", "");
            AddIntProperty(serSettings.FindPropertyRelative(() => settings.m_sampleCacheSize), "Sample cache size", "");
            
            base.ApplyRevertGUI();
        }

        private void AddBoolProperty(SerializedProperty porperty, string text, string tooltip)
        {
            var orgValue = porperty.boolValue;
            var newValue = EditorGUILayout.Toggle(new GUIContent(text, tooltip), orgValue);
            porperty.boolValue = newValue;
        }


        void AddEnumProperty(SerializedProperty porperty, string text, string tooltip, Type typeOfEnum)
        {
            Rect ourRect = EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginProperty(ourRect, GUIContent.none, porperty);

            int selectionFromInspector = porperty.intValue;
            string[] enumNamesList = System.Enum.GetNames(typeOfEnum);
            var actualSelected = EditorGUILayout.Popup(text, selectionFromInspector, enumNamesList);
            porperty.intValue = actualSelected;
            EditorGUI.EndProperty();
            EditorGUILayout.EndHorizontal();
        }

        void AddIntProperty(SerializedProperty porperty, string text, string tooltip)
        {
            var orgValue = porperty.intValue;
            var newValue = EditorGUILayout.IntField(new GUIContent(text, tooltip), orgValue);
            porperty.intValue = newValue;
        }

        void AddFloatProperty(SerializedProperty porperty, string text, string tooltip)
        {
            var orgValue = porperty.floatValue;
            var newValue = EditorGUILayout.FloatField(new GUIContent(text, tooltip), orgValue);
            porperty.floatValue = newValue;
        }
    }
}

#endif
