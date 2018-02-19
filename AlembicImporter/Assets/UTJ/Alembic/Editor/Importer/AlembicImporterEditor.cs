#if UNITY_2017_1_OR_NEWER

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;

namespace UTJ.Alembic
{
    [CustomEditor(typeof(AlembicImporter)), CanEditMultipleObjects]
    public class AlembicImporterEditor : ScriptedImporterEditor
    {
        bool m_foldComponents = true;

        public override void OnInspectorGUI()
        {
            var importer = serializedObject.targetObject as AlembicImporter;

            DisplayEnumProperty(serializedObject.FindProperty("streamSettings.normals"), Enum.GetNames(typeof(aiNormalsMode)));
            DisplayEnumProperty(serializedObject.FindProperty("streamSettings.tangents"), Enum.GetNames(typeof(aiTangentsMode)));
            DisplayEnumProperty(serializedObject.FindProperty("streamSettings.cameraAspectRatio"), Enum.GetNames(typeof(aiAspectRatioMode)));
            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("streamSettings.scaleFactor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("streamSettings.swapHandedness"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("streamSettings.swapFaceWinding"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("streamSettings.turnQuadEdges"));
            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("streamSettings.importPointPolygon"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("streamSettings.importLinePolygon"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("streamSettings.importTrianglePolygon"));
            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("streamSettings.interpolateSamples"));
            EditorGUILayout.Separator();

            m_foldComponents = EditorGUILayout.Foldout(m_foldComponents, "Components");
            if(m_foldComponents) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("streamSettings.importXform"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("streamSettings.importCamera"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("streamSettings.importPolyMesh"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("streamSettings.importPoints"));
                EditorGUILayout.Separator();
                EditorGUI.indentLevel--;
            }

            var startTimeProp = serializedObject.FindProperty("startTime");
            var endTimeProp = serializedObject.FindProperty("endTime");
            if (startTimeProp.doubleValue == endTimeProp.doubleValue)
            {
                startTimeProp.doubleValue = importer.abcStartTime;
                endTimeProp.doubleValue = importer.abcEndTime;
            }

            var startTime = (float)startTimeProp.doubleValue;
            var endTime = (float)endTimeProp.doubleValue;

            EditorGUI.BeginDisabledGroup(startTimeProp.hasMultipleDifferentValues || endTimeProp.hasMultipleDifferentValues);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.MinMaxSlider("Time Range", ref startTime, ref endTime, (float)importer.abcStartTime, (float)importer.abcEndTime);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.showMixedValue = startTimeProp.hasMultipleDifferentValues;
            var newStartTime = EditorGUILayout.FloatField(new GUIContent(" ", "Start time"), startTime, GUILayout.MinWidth(90.0f));
            EditorGUI.showMixedValue = endTimeProp.hasMultipleDifferentValues;
            var newEndTime = EditorGUILayout.FloatField(new GUIContent(" ", "End time"), endTime, GUILayout.MinWidth(90.0f));
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                if (startTime != newStartTime)
                    newStartTime = Mathf.Clamp(newStartTime, (float)importer.abcStartTime, (float)importer.abcEndTime);
                if (endTime != newEndTime)
                    newEndTime = Mathf.Clamp(newEndTime, (float)importer.abcStartTime, (float)importer.abcEndTime);
                startTimeProp.doubleValue = newStartTime;
                endTimeProp.doubleValue = newEndTime;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            float duration = endTime - startTime;

            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.LowerRight;
            if (!startTimeProp.hasMultipleDifferentValues && !endTimeProp.hasMultipleDifferentValues)
            {
                EditorGUILayout.LabelField(new GUIContent(duration.ToString("0.000") + "s"), style);
            }

            base.ApplyRevertGUI();
        }

        static void DisplayEnumProperty(SerializedProperty normalsMode,string[] displayNames)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, new GUIContent(normalsMode.displayName), normalsMode);
            EditorGUI.showMixedValue = normalsMode.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var normalsModeNew = EditorGUI.Popup(rect, normalsMode.displayName, normalsMode.intValue, displayNames.Select(ObjectNames.NicifyVariableName).ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                normalsMode.intValue = normalsModeNew;
            }
            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();
        }
    }
}

#endif
