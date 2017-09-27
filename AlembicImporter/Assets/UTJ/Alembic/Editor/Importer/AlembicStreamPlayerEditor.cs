using UnityEngine;
using UnityEditor;

namespace UTJ.Alembic
{
    [CustomEditor(typeof(AlembicStreamPlayer)),CanEditMultipleObjects]
    public class AlembicStreamPlayerEditor : Editor
    {
        public override void OnInspectorGUI()
        {   
            SerializedProperty streamDescriptorObj = serializedObject.FindProperty("m_StreamDescriptor");
            SerializedProperty currentTime = serializedObject.FindProperty("m_time");
            SerializedProperty endTime = serializedObject.FindProperty("m_PlaybackSettings.m_endTime");
            SerializedProperty startTime = serializedObject.FindProperty("m_PlaybackSettings.m_startTime");
            SerializedProperty vertexMotionScale = serializedObject.FindProperty("m_PlaybackSettings.m_vertexMotionScale");
            SerializedProperty cycle = serializedObject.FindProperty("m_PlaybackSettings.m_cycle");
            SerializedProperty interpolateSamples = serializedObject.FindProperty("m_PlaybackSettings.m_InterpolateSamples");

            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(streamDescriptorObj);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(cycle, false);
            EditorGUILayout.PropertyField(startTime,false);
            EditorGUILayout.PropertyField(endTime,false);

            if (endTime.floatValue < startTime.floatValue)
                endTime.floatValue = startTime.floatValue;

            var duration = endTime.floatValue - startTime.floatValue;

            currentTime.floatValue = EditorGUILayout.Slider("time", currentTime.floatValue,0,duration);

            EditorGUILayout.PropertyField(interpolateSamples, false);
            EditorGUILayout.PropertyField(vertexMotionScale, false);
                
            this.serializedObject.ApplyModifiedProperties();
        }
    }
}
