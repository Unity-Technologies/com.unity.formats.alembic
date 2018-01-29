using System;
using UnityEngine;
using UnityEditor;

namespace UTJ.Alembic
{
    [CustomEditor(typeof(AlembicStreamPlayer)),CanEditMultipleObjects]
    public class AlembicStreamPlayerEditor : Editor
    {
        public override void OnInspectorGUI()
        {   
            SerializedProperty vertexMotionScale = serializedObject.FindProperty("m_vertexMotionScale");
            SerializedProperty streamDescriptorObj = serializedObject.FindProperty("m_streamDescriptor");
            SerializedProperty currentTime = serializedObject.FindProperty("m_currentTime");
            SerializedProperty endFrame = serializedObject.FindProperty("m_endFrame");
            SerializedProperty startFrame = serializedObject.FindProperty("m_startFrame");
            
            var targetStreamDesc = (target as AlembicStreamPlayer).m_streamDescriptor;
            var minFrame = targetStreamDesc.minFrame;
            var maxFrame = targetStreamDesc.maxFrame;
            var frameLength = targetStreamDesc.frameLength;
            var frameRate = frameLength==0.0f ? 0.0f : 1.0f / frameLength;
            var hasVaryingTopology= false;
            var hasAcyclicFramerate = false;
            var multipleFramerates = false;
            var multipleTimeRanges = false;
            foreach (AlembicStreamPlayer player in targets)
            {
                if (player.m_streamDescriptor.minFrame != minFrame) multipleTimeRanges = true;
                if (player.m_streamDescriptor.maxFrame != maxFrame) multipleTimeRanges = true;
                if (player.m_streamDescriptor.frameLength != frameLength) multipleFramerates = true;
                if (player.m_streamDescriptor.hasVaryingTopology) hasVaryingTopology = true;
                if (player.m_streamDescriptor.hasAcyclicFramerate) hasAcyclicFramerate = true;
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
            EditorGUI.BeginDisabledGroup(multipleFramerates || multipleTimeRanges);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("frames"));            
            EditorGUIUtility.labelWidth = 35.0f;
            
            EditorGUILayout.PropertyField(startFrame,new GUIContent("from","Start frame"),GUILayout.MaxWidth(60.0f));
            EditorGUILayout.Space();
            EditorGUIUtility.labelWidth = 20.0f;
            
            EditorGUILayout.PropertyField(endFrame,new GUIContent("to","Start frame"),GUILayout.MaxWidth(60.0f));
            EditorGUILayout.EndHorizontal();

            float startFrameVal = startFrame.intValue;
            float endFrameVal = endFrame.intValue;
            EditorGUIUtility.labelWidth = 0.0f;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.MinMaxSlider(" ",ref startFrameVal,ref endFrameVal,minFrame,maxFrame);
            if (EditorGUI.EndChangeCheck())
            {
                startFrame.intValue = (int)startFrameVal;
                endFrame.intValue = (int)endFrameVal;    
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("seconds"));
            EditorGUI.BeginChangeCheck();
            EditorGUIUtility.labelWidth = 35.0f;
            EditorGUI.showMixedValue = startFrame.hasMultipleDifferentValues;
            var newStartTime = EditorGUILayout.FloatField(new GUIContent("from","Start time"),targetStreamDesc.abcStartTime +  startFrameVal * frameLength,GUILayout.MinWidth(80.0f));
            GUILayout.FlexibleSpace();
            EditorGUIUtility.labelWidth = 20.0f;
            EditorGUI.showMixedValue = endFrame.hasMultipleDifferentValues;
            var newEndTime = EditorGUILayout.FloatField(new GUIContent("to","End time"),targetStreamDesc.abcStartTime + endFrameVal * frameLength,GUILayout.MinWidth(80.0f));
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                endFrameVal = (float)Math.Round((newEndTime - targetStreamDesc.abcStartTime) * frameRate);
                startFrameVal = (float)Math.Round((newStartTime - targetStreamDesc.abcStartTime) * frameRate);

                startFrame.intValue = (int)startFrameVal;
                endFrame.intValue = (int)endFrameVal;    
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            EditorGUIUtility.labelWidth = 0.0f;

            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.LowerRight;
            if (!endFrame.hasMultipleDifferentValues && !startFrame.hasMultipleDifferentValues && !hasAcyclicFramerate)
            {
                int numFrames = (int)(endFrameVal - startFrameVal);
                float duration = numFrames * frameLength;
                EditorGUILayout.LabelField(new GUIContent(duration.ToString("0.000") + "s at " + frameRate + "fps (" + (numFrames+1) + " frames).", "Frame rate"), style);
            }
            else
            {
                EditorGUILayout.LabelField(new GUIContent("--s at --fps (-- frames).", "Frame rate"), style);
            }
            
            EditorGUILayout.PropertyField(currentTime,new GUIContent("Time"));

            EditorGUILayout.PropertyField(vertexMotionScale);
            this.serializedObject.ApplyModifiedProperties();
        }
    }
}
