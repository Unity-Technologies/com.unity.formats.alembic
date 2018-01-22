using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace UTJ.Alembic
{
    [CustomEditor(typeof(AlembicExporter))]
    public class AlembicExporterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var t = target as AlembicExporter;

            GUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Output Path", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            t.m_outputPath = EditorGUILayout.TextField(t.m_outputPath);
            if (GUILayout.Button("...", GUILayout.Width(24)))
            {
                var dir = "";
                var filename = "";
                try
                {
                    dir = Path.GetDirectoryName(t.m_outputPath);
                    filename = Path.GetFileName(t.m_outputPath);
                }
                catch(Exception) { }

                var path = EditorUtility.SaveFilePanel("Output Path", dir, filename, "abc");
                if (path.Length > 0)
                {
                    t.m_outputPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            EditorGUILayout.LabelField("Alembic Settings", EditorStyles.boldLabel);
            {
                var conf = t.m_conf;
                EditorGUI.BeginChangeCheck();
                conf.archiveType = (AbcAPI.aeArchiveType)EditorGUILayout.EnumPopup("Archive Type", conf.archiveType);
                conf.xformType = (AbcAPI.aeXFormType)EditorGUILayout.EnumPopup("Xform Type", conf.xformType);
                conf.timeSamplingType = (AbcAPI.aeTimeSamplingType)EditorGUILayout.EnumPopup("Time Sampling Type", conf.timeSamplingType);
                if (conf.timeSamplingType == AbcAPI.aeTimeSamplingType.Uniform)
                {
                    EditorGUI.indentLevel++;
                    conf.frameRate = EditorGUILayout.FloatField("Frame Rate", conf.frameRate);
                    t.m_fixDeltaTime = EditorGUILayout.Toggle("Fix Delta Time", t.m_fixDeltaTime);
                    EditorGUI.indentLevel--;
                }
                conf.swapHandedness = EditorGUILayout.Toggle("Swap Handedness", conf.swapHandedness);
                conf.swapFaces = EditorGUILayout.Toggle("Swap Faces", conf.swapFaces);
                conf.scaleFactor = EditorGUILayout.FloatField("Scale Factor", conf.scaleFactor);
                conf.startTime = EditorGUILayout.FloatField("Start Time", conf.startTime);
                if (EditorGUI.EndChangeCheck())
                {
                    t.m_conf = conf;
                }
            }
            GUILayout.Space(5);

            EditorGUILayout.LabelField("Capture Settings", EditorStyles.boldLabel);
            {
                t.m_scope = (AlembicExporter.Scope)EditorGUILayout.EnumPopup("Scope", t.m_scope);
                t.m_ignoreDisabled = EditorGUILayout.Toggle("Ignore Disabled", t.m_ignoreDisabled);
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Capture Components");
                EditorGUI.indentLevel++;
                t.m_captureMeshRenderer = EditorGUILayout.Toggle("MeshRenderer", t.m_captureMeshRenderer);
                t.m_captureSkinnedMeshRenderer = EditorGUILayout.Toggle("SkinnedMeshRenderer", t.m_captureSkinnedMeshRenderer);
                t.m_captureParticleSystem = EditorGUILayout.Toggle("ParticleSystem", t.m_captureParticleSystem);
                t.m_captureCamera = EditorGUILayout.Toggle("Camera", t.m_captureCamera);
                t.m_customCapturer = EditorGUILayout.Toggle("Custom Capturer", t.m_customCapturer);
                EditorGUI.indentLevel--;
                GUILayout.Space(5);
                t.m_captureOnStart = EditorGUILayout.Toggle("Capture On Start", t.m_captureOnStart);
                if(t.m_captureOnStart)
                {
                    EditorGUI.indentLevel++;
                    t.m_ignoreFirstFrame = EditorGUILayout.Toggle("Ignore First Frame", t.m_ignoreFirstFrame);
                    EditorGUI.indentLevel--;
                }
                t.m_maxCaptureFrame = EditorGUILayout.IntField("Max Capture Frame", t.m_maxCaptureFrame);
            }
            GUILayout.Space(5);

            EditorGUILayout.LabelField("Misc", EditorStyles.boldLabel);
            t.m_detailedLog = EditorGUILayout.Toggle("Detailed Log", t.m_detailedLog);

            GUILayout.Space(10);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(t);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            EditorGUILayout.LabelField("Capture Control", EditorStyles.boldLabel);
            if (t.isRecording)
            {
                if (GUILayout.Button("End Capture"))
                    t.EndCapture();
            }
            else
            {
                if (GUILayout.Button("Begin Capture"))
                    t.BeginCapture();

                if (GUILayout.Button("One Shot"))
                    t.OneShot();
            }
        }
    }
}
