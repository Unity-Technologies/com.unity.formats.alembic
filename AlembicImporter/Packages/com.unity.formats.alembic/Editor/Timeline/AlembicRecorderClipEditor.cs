using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Timeline;

namespace UTJ.Alembic
{
    [CustomEditor(typeof(AlembicRecorderClip))]
    public class AlembicRecorderClipEditor : Editor
    {
        TimelineAsset m_timelineAsset;
        bool m_foldCaptureComponents;
        bool m_foldMeshComponents;

        public override void OnInspectorGUI()
        {
            var t = target as AlembicRecorderClip;
            var settings = t.settings;
            var so = serializedObject;

            bool dirty = false;
            var pathSettings = "m_settings.";

            // output path
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Output Path", EditorStyles.boldLabel);
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                settings.outputPath = EditorGUILayout.TextField(settings.outputPath);
                if (EditorGUI.EndChangeCheck())
                    dirty = true;

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
                        dirty = true;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(5);

            // alembic settings
            EditorGUILayout.LabelField("Alembic Settings", EditorStyles.boldLabel);
            {
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "conf.archiveType"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "conf.xformType"));
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
                t.targetBranch = EditorGUILayout.ObjectField("Target", t.targetBranch, typeof(GameObject), true) as GameObject;
                if (EditorGUI.EndChangeCheck())
                    dirty = true;
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "assumeNonSkinnedMeshesAreConstant"));
            GUILayout.Space(5);

            m_foldCaptureComponents = EditorGUILayout.Foldout(m_foldCaptureComponents, "Capture Components");
            if (m_foldCaptureComponents)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "captureMeshRenderer"), new GUIContent("MeshRenderer"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "captureSkinnedMeshRenderer"), new GUIContent("SkinnedMeshRenderer"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "captureParticleSystem"), new GUIContent("ParticleSystem"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "captureCamera"), new GUIContent("Camera"));
                EditorGUI.indentLevel--;
            }

            m_foldMeshComponents = EditorGUILayout.Foldout(m_foldMeshComponents, "Mesh Components");
            if (m_foldMeshComponents)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "meshNormals"), new GUIContent("Normals"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "meshUV0"), new GUIContent("UV 1"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "meshUV1"), new GUIContent("UV 2"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "meshColors"), new GUIContent("Vertex Color"));
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "meshSubmeshes"), new GUIContent("Submeshes"));
                EditorGUI.indentLevel--;
            }
            {
                EditorGUILayout.PropertyField(so.FindProperty("m_ignoreFirstFrame"));
            }
            GUILayout.Space(5);

            // misc settigs
            EditorGUILayout.LabelField("Misc", EditorStyles.boldLabel);
            {
                EditorGUILayout.PropertyField(so.FindProperty(pathSettings + "detailedLog"));
            }
            GUILayout.Space(10);


            // frame rate
            m_timelineAsset = FindTimelineAsset();
            if (m_timelineAsset != null)
            {
                so.FindProperty(pathSettings + "conf.frameRate").floatValue = m_timelineAsset.editorSettings.fps;
                so.FindProperty(pathSettings + "fixDeltaTime").boolValue = true;
            }

            so.ApplyModifiedProperties();
            if (dirty)
            {
                EditorUtility.SetDirty(m_timelineAsset);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }

        TimelineAsset FindTimelineAsset()
        {
            if (!AssetDatabase.Contains(target))
                return null;

            var path = AssetDatabase.GetAssetPath(target);
            var objs = AssetDatabase.LoadAllAssetsAtPath(path);

            foreach (var obj in objs)
            {
                if (obj != null && AssetDatabase.IsMainAsset(obj))
                    return obj as TimelineAsset;
            }
            return null;
        }
    }
}
