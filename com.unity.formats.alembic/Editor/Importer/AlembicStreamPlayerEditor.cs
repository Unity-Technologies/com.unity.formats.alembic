using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Formats.Alembic.Importer;

namespace UnityEditor.Formats.Alembic.Importer
{
    [CustomEditor(typeof(AlembicStreamPlayer)), CanEditMultipleObjects]
    internal class AlembicStreamPlayerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup((target.hideFlags & HideFlags.NotEditable) != HideFlags.None);

            SerializedProperty streamDescriptorObj = serializedObject.FindProperty("streamDescriptor");
            SerializedProperty startTime = serializedObject.FindProperty("startTime");
            SerializedProperty endTime = serializedObject.FindProperty("endTime");

            var streamPlayer = target as AlembicStreamPlayer;
            var targetStreamDesc = streamPlayer.StreamDescriptor;
            var multipleTimeRanges = false;
            foreach (AlembicStreamPlayer player in targets)
            {
                //
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(streamDescriptorObj);
            EditorGUI.EndDisabledGroup();
            if (streamDescriptorObj.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("The stream descriptor could not be found.", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField(new GUIContent("Time Range"));
            EditorGUI.BeginDisabledGroup(multipleTimeRanges);

            var abcStart = targetStreamDesc.mediaStartTime;
            var abcEnd = targetStreamDesc.mediaEndTime;
            var start = streamPlayer.StartTime;
            var end = streamPlayer.EndTime;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.MinMaxSlider(" ", ref start, ref end, abcStart, abcEnd);
            if (EditorGUI.EndChangeCheck())
            {
                startTime.doubleValue = start;
                endTime.doubleValue = end;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("seconds"));
            EditorGUI.BeginChangeCheck();
            EditorGUIUtility.labelWidth = 35.0f;
            EditorGUI.showMixedValue = startTime.hasMultipleDifferentValues;
            var newStartTime = EditorGUILayout.FloatField(new GUIContent("from", "Start time"), start, GUILayout.MinWidth(80.0f));
            GUILayout.FlexibleSpace();
            EditorGUIUtility.labelWidth = 20.0f;
            EditorGUI.showMixedValue = endTime.hasMultipleDifferentValues;
            var newEndTime = EditorGUILayout.FloatField(new GUIContent("to", "End time"), end, GUILayout.MinWidth(80.0f));
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                startTime.doubleValue = newStartTime;
                endTime.doubleValue = newEndTime;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            EditorGUIUtility.labelWidth = 0.0f;

            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.LowerRight;
            if (!endTime.hasMultipleDifferentValues && !startTime.hasMultipleDifferentValues)
            {
                EditorGUILayout.LabelField(new GUIContent((end - start).ToString("0.000") + "s"), style);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentTime"), new GUIContent("Time"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("vertexMotionScale"));
            EditorGUILayout.Space();

#if UNITY_2018_3_OR_NEWER
            var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(streamPlayer.gameObject);
            if (prefabStatus == PrefabInstanceStatus.NotAPrefab || prefabStatus == PrefabInstanceStatus.Disconnected)
#else
            if (PrefabUtility.GetPrefabType(streamPlayer.gameObject) == PrefabType.DisconnectedModelPrefabInstance)
#endif
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(16);
                if (GUILayout.Button("Recreate Missing Nodes", GUILayout.Width(180)))
                {
                    streamPlayer.LoadStream(true);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.EndDisabledGroup();

            this.serializedObject.ApplyModifiedProperties();
        }
    }
}
