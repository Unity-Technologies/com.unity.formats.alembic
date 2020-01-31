using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.Formats.Alembic.Exporter;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.Formats.Alembic.Util;

namespace UnityEditor.Formats.Alembic.Exporter
{
    [CustomEditor(typeof(AlembicExporter))]
    class AlembicExporterEditor : Editor
    {
        bool m_foldCaptureComponents;
        bool m_foldMeshComponents;

        internal static bool DrawSettings(SerializedObject so,
            UnityEngine.Formats.Alembic.Util.AlembicRecorderSettings settings,
            string pathSettings, ref bool foldCaptureComponents, ref bool foldMeshComponents, bool recorder)
        {
            bool dirty = false;
            if (!recorder)
            {
                // output path
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Output Path", EditorStyles.boldLabel);
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUI.BeginChangeCheck();
                    settings.OutputPath = EditorGUILayout.TextField(settings.OutputPath);
                    if (EditorGUI.EndChangeCheck())
                        dirty = true;

                    if (GUILayout.Button("...", GUILayout.Width(24)))
                    {
                        var dir = "";
                        var filename = "";
                        try
                        {
                            dir = Path.GetDirectoryName(settings.OutputPath);
                            filename = Path.GetFileName(settings.OutputPath);
                        }
                        catch (Exception)
                        {
                        }

                        var path = EditorUtility.SaveFilePanel("Output Path", dir, filename, "abc");
                        if (path.Length > 0)
                        {
                            settings.OutputPath = path;
                            dirty = true;
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
                GUILayout.Space(5);
            }


            // alembic settings
            EditorGUILayout.LabelField("Alembic Settings", EditorStyles.boldLabel);
            {
#if !UNITY_EDITOR_LINUX
                var archiveProp = so.FindProperty(pathSettings + "conf.archiveType");
                using (var hScope = new EditorGUILayout.HorizontalScope())
                {
                    using (var propertyScope = new EditorGUI.PropertyScope(hScope.rect,
                        new GUIContent(archiveProp.displayName), archiveProp))
                    {
                        using (var changeScope = new EditorGUI.ChangeCheckScope())
                        {
                            var val = EditorGUILayout.EnumPopup(propertyScope.content, (ArchiveType) archiveProp.intValue, null,
                                true);
                            if (changeScope.changed)
                            {
                                archiveProp.intValue = (int)(ArchiveType)val;
                            }
                        }
                    }
                }
#endif
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "conf.xformType"));
                if (!recorder)
                {
                    var timeSamplingType = so.FindProperty(pathSettings + "conf.timeSamplingType");
                    EditorGUILayout.PropertyField(timeSamplingType);
                    if (timeSamplingType.intValue == (int) TimeSamplingType.Uniform)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "conf.frameRate"));
                        EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "fixDeltaTime"));
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "conf.swapHandedness"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "conf.swapFaces"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "conf.scaleFactor"));
            }
            GUILayout.Space(5);


            // capture settings
            EditorGUILayout.LabelField("Capture Settings", EditorStyles.boldLabel);
            var scope = so.FindProperty(pathSettings + "scope");
            EditorGUILayout.PropertyField(scope);
            if (scope.intValue == (int)ExportScope.TargetBranch)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                settings.TargetBranch = EditorGUILayout.ObjectField("Target", settings.TargetBranch, typeof(GameObject), true) as GameObject;
                if (EditorGUI.EndChangeCheck())
                    dirty = true;
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "assumeNonSkinnedMeshesAreConstant"), new GUIContent("Static MeshRenderers"));
            GUILayout.Space(5);

            foldCaptureComponents = EditorGUILayout.Foldout(foldCaptureComponents, "Capture Components");
            if (foldCaptureComponents)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "captureMeshRenderer"), new GUIContent("MeshRenderer"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "captureSkinnedMeshRenderer"), new GUIContent("SkinnedMeshRenderer"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "captureCamera"), new GUIContent("Camera"));
                EditorGUI.indentLevel--;
            }

            foldMeshComponents = EditorGUILayout.Foldout(foldMeshComponents, "Mesh Components");
            if (foldMeshComponents)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "meshNormals"), new GUIContent("Normals"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "meshUV0"), new GUIContent("UV 0"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "meshUV1"), new GUIContent("UV 1"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "meshColors"), new GUIContent("Vertex Color"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "meshSubmeshes"), new GUIContent("Submeshes"));
                EditorGUI.indentLevel--;
            }

            return dirty;
        }

        public override void OnInspectorGUI()
        {
            var t = target as AlembicExporter;
            var recorder = t.Recorder;
            var settings = recorder.Settings;
            var so = serializedObject;

            var pathSettings = "m_recorder.m_settings.";
            var dirty = DrawSettings(so, settings, pathSettings, ref m_foldCaptureComponents, ref m_foldMeshComponents, false);

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
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "detailedLog"));
            }
            GUILayout.Space(10);

            so.ApplyModifiedProperties();
            if (dirty)
            {
                EditorUtility.SetDirty(target);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                // capture control
                EditorGUILayout.LabelField("Capture Control", EditorStyles.boldLabel);
                if (recorder.Recording)
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
}
