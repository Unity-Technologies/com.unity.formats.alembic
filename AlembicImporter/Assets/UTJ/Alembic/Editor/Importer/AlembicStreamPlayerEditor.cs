using System;
using UnityEngine;
using UnityEditor;

namespace UTJ.Alembic
{
    [CustomEditor(typeof(AlembicStreamPlayer)),CanEditMultipleObjects]
    public class AlembicStreamPlayerEditor : Editor
    {
        bool m_foldMisc = false;

        public override void OnInspectorGUI()
        {   
            SerializedProperty vertexMotionScale = serializedObject.FindProperty("m_vertexMotionScale");
            SerializedProperty streamDescriptorObj = serializedObject.FindProperty("m_streamDescriptor");
            SerializedProperty currentTime = serializedObject.FindProperty("m_currentTime");
            SerializedProperty startTime = serializedObject.FindProperty("m_startTime");
            SerializedProperty endTime = serializedObject.FindProperty("m_endTime");
            SerializedProperty asyncLoad = serializedObject.FindProperty("m_asyncLoad");

            var streamPlayer = target as AlembicStreamPlayer;
            var targetStreamDesc = streamPlayer.m_streamDescriptor;
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
                EditorGUILayout.HelpBox("The stream descriptor could not be found.",MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField(new GUIContent("Time Range"));
            EditorGUI.BeginDisabledGroup(multipleTimeRanges);

            var abcStart = (float)targetStreamDesc.abcStartTime;
            var abcEnd = (float)targetStreamDesc.abcEndTime;
            var start = (float)streamPlayer.m_startTime;
            var end = (float)streamPlayer.m_endTime;
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

            EditorGUILayout.PropertyField(currentTime, new GUIContent("Time"));
            EditorGUILayout.PropertyField(vertexMotionScale);
            EditorGUILayout.Space();

            m_foldMisc = EditorGUILayout.Foldout(m_foldMisc, "Misc");
            if(m_foldMisc)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(asyncLoad);
                EditorGUI.indentLevel--;
            }

            this.serializedObject.ApplyModifiedProperties();
        }
    }
}
