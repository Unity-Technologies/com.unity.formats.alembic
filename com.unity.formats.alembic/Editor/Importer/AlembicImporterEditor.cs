#if UNITY_2017_1_OR_NEWER

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace UnityEditor.Formats.Alembic.Importer
{
    [CustomEditor(typeof(AlembicImporter)), CanEditMultipleObjects]
    internal class AlembicImporterEditor : ScriptedImporterEditor
    {
        public override void OnInspectorGUI()
        {
            var importer = serializedObject.targetObject as AlembicImporter;
            var pathSettings = "streamSettings.";

            EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "scaleFactor"),
                    new GUIContent("Scale Factor", "How much to scale the models compared to what is in the source file."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "swapHandedness"),
                    new GUIContent("Swap Handedness", "Swap X coordinate"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "interpolateSamples"),
                    new GUIContent("Interpolate Samples", "Interpolate transforms and vertices (if topology is constant)."));
                EditorGUILayout.Separator();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importVisibility"),
                    new GUIContent("Import Visibility", "Import visibility animation."));
                //EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importXform"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importCameras"),
                    new GUIContent("Import Cameras", ""));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importMeshes"),
                    new GUIContent("Import Meshes", ""));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(pathSettings + "importPoints"),
                    new GUIContent("Import Points", ""));
                EditorGUILayout.Separator();

                // time range
                var startTimeProp = serializedObject.FindProperty("startTime");
                var endTimeProp = serializedObject.FindProperty("endTime");
                if (startTimeProp.doubleValue == endTimeProp.doubleValue)
                {
                    startTimeProp.doubleValue = importer.AbcStartTime;
                    endTimeProp.doubleValue = importer.AbcEndTime;
                }

                var startTime = (float)startTimeProp.doubleValue;
                var endTime = (float)endTimeProp.doubleValue;

                EditorGUI.BeginDisabledGroup(startTimeProp.hasMultipleDifferentValues || endTimeProp.hasMultipleDifferentValues);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider("Time Range", ref startTime, ref endTime, (float)importer.AbcStartTime, (float)importer.AbcEndTime);

                EditorGUILayout.BeginHorizontal();
                EditorGUI.showMixedValue = startTimeProp.hasMultipleDifferentValues;
                var newStartTime = EditorGUILayout.FloatField(new GUIContent(" ", "Start time"), startTime, GUILayout.MinWidth(90.0f));
                EditorGUI.showMixedValue = endTimeProp.hasMultipleDifferentValues;
                var newEndTime = EditorGUILayout.FloatField(new GUIContent(" ", "End time"), endTime, GUILayout.MinWidth(90.0f));
                EditorGUI.showMixedValue = false;

                if (EditorGUI.EndChangeCheck())
                {
                    if (startTime != newStartTime)
                        newStartTime = Mathf.Clamp(newStartTime, (float)importer.AbcStartTime, (float)importer.AbcEndTime);
                    if (endTime != newEndTime)
                        newEndTime = Mathf.Clamp(newEndTime, (float)importer.AbcStartTime, (float)importer.AbcEndTime);
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
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Cameras", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;
                DisplayEnumProperty(serializedObject.FindProperty(pathSettings + "cameraAspectRatio"), Enum.GetNames(typeof(aiAspectRatioMode)),
                    new GUIContent("Aspect Ratio", ""));
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

        static void DisplayEnumProperty(SerializedProperty prop, string[] displayNames, GUIContent guicontent = null)
        {
            if (guicontent == null)
                guicontent = new GUIContent(prop.displayName);

            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, guicontent, prop);
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();

            GUIContent[] options = new GUIContent[displayNames.Length];
            for (int i = 0; i < options.Length; ++i)
                options[i] = new GUIContent(ObjectNames.NicifyVariableName(displayNames[i]), "");

            var normalsModeNew = EditorGUI.Popup(rect, guicontent, prop.intValue, options);
            if (EditorGUI.EndChangeCheck())
                prop.intValue = normalsModeNew;

            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();
        }
    }
}

#endif
