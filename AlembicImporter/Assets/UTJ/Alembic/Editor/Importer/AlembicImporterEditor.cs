#if UNITY_2017_1_OR_NEWER

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;

namespace UTJ.Alembic
{
    [CustomEditor(typeof(AlembicImporter)),CanEditMultipleObjects]
    public class AlembicImporterEditor : ScriptedImporterEditor
    {
        public override void OnInspectorGUI()
        {
            //var importer = serializedObject.targetObject as AlembicImporter;

            DisplayEnumProperty(serializedObject.FindProperty("streamSettings.normals"),Enum.GetNames(typeof(AbcAPI.aiNormalsMode)));
            DisplayEnumProperty(serializedObject.FindProperty("streamSettings.tangents"),Enum.GetNames(typeof(AbcAPI.aiTangentsMode)));
            DisplayEnumProperty(serializedObject.FindProperty("streamSettings.cameraAspectRatio"), Enum.GetNames(typeof(AbcAPI.aiAspectRatioMode)));

            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("streamSettings.swapHandedness"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("streamSettings.swapFaceWinding"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("streamSettings.turnQuadEdges"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleFactor"));
            EditorGUILayout.Separator();

            var abcStartTime = serializedObject.FindProperty("abcStartTime");
            var abcEndTime = serializedObject.FindProperty("abcEndTime");
            var abcFrameCount = serializedObject.FindProperty("abcFrameCount");
            var startFrame = serializedObject.FindProperty("startFrame");
            var endFrame = serializedObject.FindProperty("endFrame");
            var frameLength =  (abcFrameCount.intValue == 1) ? 0 : (abcEndTime.floatValue - abcStartTime.floatValue) / (abcFrameCount.intValue-1);
            var frameRate = (abcFrameCount.intValue == 1) ? 0 : (int)(1.0f/ frameLength);

            float startFrameVal = startFrame.intValue;
            float endFrameVal = endFrame.intValue;
            EditorGUI.BeginDisabledGroup(abcStartTime.hasMultipleDifferentValues || abcEndTime.hasMultipleDifferentValues || abcFrameCount.hasMultipleDifferentValues);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.MinMaxSlider("Time Range",ref startFrameVal,ref endFrameVal,0,abcFrameCount.intValue-1);

            startFrameVal = (float)Math.Floor(startFrameVal);
            endFrameVal = (float)Math.Floor(endFrameVal);

            var startTime = startFrameVal * frameLength + abcStartTime.floatValue;
            var endTime = endFrameVal * frameLength + abcStartTime.floatValue;

            EditorGUILayout.BeginHorizontal();
            
            EditorGUI.showMixedValue = startFrame.hasMultipleDifferentValues;
            var newStartTime = EditorGUILayout.FloatField(new GUIContent(" ","Start time"),startTime,GUILayout.MinWidth(90.0f));
            EditorGUI.showMixedValue = endFrame.hasMultipleDifferentValues;
            var newEndTime = EditorGUILayout.FloatField(new GUIContent(" ","End time"),endTime,GUILayout.MinWidth(90.0f));
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                if (endTime != newEndTime)
                {
                    if (newEndTime < startTime) newEndTime = endTime;
                    if (newEndTime > abcEndTime.floatValue) newEndTime = abcEndTime.floatValue;
                    endFrameVal = (float)Math.Round((newEndTime - abcStartTime.floatValue) * frameRate);
                }
                if (startTime != newStartTime)
                {
                    if (newStartTime > endTime) newStartTime = startTime;
                    if (newStartTime < abcStartTime.floatValue) newStartTime = abcStartTime.floatValue;
                    startFrameVal = (float)Math.Round((newStartTime - abcStartTime.floatValue) * frameRate);
                }
                startFrame.intValue = (int)startFrameVal;
                endFrame.intValue = (int)endFrameVal;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            int frameCount = (int)(endFrameVal - startFrameVal);
            float duration = frameCount * frameLength;

            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.LowerRight;
            if (!endFrame.hasMultipleDifferentValues && !startFrame.hasMultipleDifferentValues && !abcFrameCount.hasMultipleDifferentValues)
            {
                EditorGUILayout.LabelField(new GUIContent(duration.ToString("0.000") +"s at " + frameRate + "fps (" + (frameCount+1) + " frames)"),style);
                EditorGUILayout.LabelField(new GUIContent("frame " + startFrameVal.ToString("0") + " to " + endFrameVal.ToString("0")),style);
            }
            else
            {
                EditorGUILayout.LabelField(new GUIContent("the selected assets have different time ranges or framerates"), style);
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
