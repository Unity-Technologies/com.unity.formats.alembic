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
        public override void OnInspectorGUI()
        {
            var importer = serializedObject.targetObject as AlembicImporter;
            var pathSettings = "streamSettings.";

            EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "scaleFactor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "swapHandedness"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "interpolateSamples"));
                EditorGUILayout.Separator();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importVisibility"));
                //EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importXform"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importCameras"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importMeshes"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importPoints"));
                EditorGUILayout.Separator();

                // time range
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
                style.normal.textColor = Color.gray;
                if (!startTimeProp.hasMultipleDifferentValues && !endTimeProp.hasMultipleDifferentValues)
                {
                    EditorGUILayout.LabelField(new GUIContent(duration.ToString("0.000") + "s"), style);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.LabelField("Geometry", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;
                DisplayEnumProperty(serializedObject.FindProperty(pathSettings + "normals"), Enum.GetNames(typeof(aiNormalsMode)));
                DisplayEnumProperty(serializedObject.FindProperty(pathSettings + "tangents"), Enum.GetNames(typeof(aiTangentsMode)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "flipFaces"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "turnQuadEdges"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Cameras", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;
                DisplayEnumProperty(serializedObject.FindProperty(pathSettings + "cameraAspectRatio"), Enum.GetNames(typeof(aiAspectRatioMode)));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Separator();

            // revive this if needed
            //EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importPointPolygon"));
            //EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importLinePolygon"));
            //EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importTrianglePolygon"));
            //EditorGUILayout.Separator();


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
