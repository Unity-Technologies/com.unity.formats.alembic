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
            AddBoolProperty(serSettings.FindPropertyRelative(() => settings.m_TurnQuadEdges), "Turn Quad Edges", "");
            AddBoolProperty(serSettings.FindPropertyRelative(() => settings.m_shareVertices), "Merge Vertices (experimental)", "Allow vertex sharing between faces when possible.");
            AddBoolProperty(serSettings.FindPropertyRelative(() => settings.m_treatVertexExtraDataAsStatics), "Vertex extra data is static (exp.)", "When set, UV's/normals/tangents are fetched from file only on topology change event.");
            AddFloatProperty(serSettings.FindPropertyRelative(() => settings.m_scaleFactor), "Scale factor", "Apply a uniform scale to the root node of the model");

            AddEnumProperty(serSettings.FindPropertyRelative(() => settings.m_normalsMode), "Normals mode", "", settings.m_normalsMode.GetType());
            AddEnumProperty(serSettings.FindPropertyRelative(() => settings.m_tangentsMode), "Tangent mode", "", settings.m_tangentsMode.GetType());
            AddEnumProperty(serSettings.FindPropertyRelative(() => settings.m_aspectRatioMode), "Aspect ratio mode", "", settings.m_aspectRatioMode.GetType());

            EditorGUILayout.Separator();
            var min = serSettings.FindPropertyRelative(() => settings.m_minTime).floatValue;
            var max = serSettings.FindPropertyRelative(() => settings.m_maxTime).floatValue;
            var startVal = serSettings.FindPropertyRelative(() => settings.m_startTime);
            var endVal = serSettings.FindPropertyRelative(() => settings.m_endTime);
            var startFloat = startVal.floatValue;
            var endFloat = endVal.floatValue;
            
            EditorGUILayout.MinMaxSlider("Time range",ref startFloat,ref endFloat,min,max);
            EditorGUILayout.BeginHorizontal();
            startVal.floatValue = EditorGUILayout.FloatField(new GUIContent(" ","Start time"),startFloat,GUILayout.MinWidth(90.0f));
            endVal.floatValue = EditorGUILayout.FloatField(new GUIContent(" ","End time"),endFloat,GUILayout.MinWidth(90.0f));
            EditorGUILayout.EndHorizontal();

            if (startVal.floatValue < min)
                startVal.floatValue = min;
            if (endVal.floatValue > max)
                endVal.floatValue = max;
            if (startVal.floatValue > endVal.floatValue)
                startVal.floatValue = min;

            EditorGUILayout.Separator();
            AddBoolProperty(serSettings.FindPropertyRelative(() => settings.m_cacheSamples), "Cache samples", "Cache all samples upfront");
            
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
