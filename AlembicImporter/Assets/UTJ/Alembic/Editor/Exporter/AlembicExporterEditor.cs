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
        bool m_foldCaptureComponents;
        bool m_foldMeshComponents;

        public override void OnInspectorGUI()
        {
            var t = target as AlembicExporter;
            var recorder = t.recorder;
            var settings = recorder.settings;
            var so = serializedObject;

            var pathSettings = "m_recorder.m_settings";

            // output path
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Output Path", EditorStyles.boldLabel);
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                settings.outputPath = EditorGUILayout.TextField(settings.outputPath);
                if (GUILayout.Button("...", GUILayout.Width(24)))
                {
                    var dir = "";
                    var filename = "";
                    try
                    {
                        dir = Path.GetDirectoryName(settings.outputPath);
                        filename = Path.GetFileName(settings.outputPath);
                    }
                    catch (Exception) { }

                    var path = EditorUtility.SaveFilePanel("Output Path", dir, filename, "abc");
                    if (path.Length > 0)
                    {
                        settings.outputPath = path;
                    }
                }
                EditorGUILayout.EndHorizontal();
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(target);
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }
            }
            GUILayout.Space(5);


            // alembic settings
            EditorGUILayout.LabelField("Alembic Settings", EditorStyles.boldLabel);
            {
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".conf.archiveType"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".conf.xformType"));
                var timeSamplingType = so.FindProperty(pathSettings + ".conf.timeSamplingType");
                EditorGUILayout.PropertyField(timeSamplingType);
                if (timeSamplingType.intValue == (int)aeTimeSamplingType.Uniform)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".conf.frameRate"));
                    EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".fixDeltaTime"));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".conf.swapHandedness"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".conf.swapFaces"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".conf.scaleFactor"));
            }
            GUILayout.Space(5);


            // capture settings
            EditorGUILayout.LabelField("Capture Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".scope"));
            EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".assumeNonSkinnedMeshesAreConstant"));
            GUILayout.Space(5);

            m_foldCaptureComponents = EditorGUILayout.Foldout(m_foldCaptureComponents, "Capture Components");
            if (m_foldCaptureComponents)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".captureMeshRenderer"), new GUIContent("MeshRenderer"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".captureSkinnedMeshRenderer"), new GUIContent("SkinnedMeshRenderer"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".captureParticleSystem"), new GUIContent("ParticleSystem"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".captureCamera"), new GUIContent("Camera"));
                EditorGUI.indentLevel--;
            }

            m_foldMeshComponents = EditorGUILayout.Foldout(m_foldMeshComponents, "Mesh Components");
            if (m_foldMeshComponents)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".meshNormals"), new GUIContent("Normals"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".meshUV0"), new GUIContent("UV 1"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".meshUV1"), new GUIContent("UV 2"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".meshColors"), new GUIContent("Vertex Color"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".meshSubmeshes"), new GUIContent("Submeshes"));
                EditorGUI.indentLevel--;
            }
            {
                var m_captureOnStart = so.FindProperty("m_captureOnStart");
                EditorGUILayout.PropertyField(m_captureOnStart);
                if (m_captureOnStart.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(so.FindProperty("m_ignoreFirstFrame"));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.PropertyField(so.FindProperty("m_maxCaptureFrame"));
            }
            GUILayout.Space(5);

            // misc settigs
            EditorGUILayout.LabelField("Misc", EditorStyles.boldLabel);
            {
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + ".detailedLog"));
            }
            GUILayout.Space(10);

            so.ApplyModifiedProperties();

            // capture control
            EditorGUILayout.LabelField("Capture Control", EditorStyles.boldLabel);
            if (recorder.recording)
            {
                if (GUILayout.Button("End Recording"))
                    t.EndRecording();
            }
            else
            {
                if (GUILayout.Button("Begin Recording"))
                    t.BeginRecording();

                if (GUILayout.Button("One Shot"))
                    t.OneShot();
            }
        }
    }
}
