using System.IO;
using UnityEngine;
using UnityEditor;

namespace UTJ.Alembic
{


    public class AlembicManualImporterEditor : EditorWindow
    {
        private AlembicImportSettings m_ImportSettings = new AlembicImportSettings();
        private AlembicDiagnosticSettings m_DiagSettings = new AlembicDiagnosticSettings();
        private AlembicImportMode m_ImportMode = AlembicImportMode.AutomaticStreamingSetup;
        private bool m_CopyIntoStreamingAssets = true;
        private bool m_GeneratePrefab = true;


#if !UNITY_5_7_OR_NEWER && !ENABLE_SCRIPTED_IMPORTERS

        [MenuItem("Assets/Alembic/Import asset...")]
#endif
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(AlembicManualImporterEditor));
        }

        Rect NewControlRect(bool hasLabel, float height, float hpad = 5, float vpad = 5)
        {
            Rect r = EditorGUILayout.GetControlRect(hasLabel, height + vpad);

            r.x += hpad;
            r.width -= 2*hpad;
            r.y += vpad;
            r.height -= vpad;

            return r;
        }

        public void OnGUI()
        {
            titleContent = new GUIContent("Alembic file importer");
            // File Path
            AlembicUI.AddHorzLine(() =>
            {
                EditorGUILayout.PrefixLabel("Alembic File:");
                m_ImportSettings.m_pathToAbc = EditorGUILayout.TextField(m_ImportSettings.m_pathToAbc);
                if (GUILayout.Button("..."))
                {
                    m_ImportSettings.m_pathToAbc = EditorUtility.OpenFilePanel("Select Alembic File", m_ImportSettings.m_pathToAbc,
                        "abc");
                }
            });

            AlembicUI.AddHorzLine(() =>
            {
                m_GeneratePrefab = EditorGUILayout.Toggle("Generate prefab", m_GeneratePrefab);
            });

            // Import Mode
            AlembicUI.AddHorzLine(() =>
            {
                string[] enumNamesList = System.Enum.GetNames(typeof(AlembicImportMode));
                m_ImportMode = (AlembicImportMode) EditorGUILayout.Popup("Mode", (int) m_ImportMode, enumNamesList);
            });

            AlembicUI.AddHorzLine(() =>
            {
                GUI.enabled = m_ImportMode > AlembicImportMode.NoSupportForStreaming;
                m_CopyIntoStreamingAssets = EditorGUILayout.Toggle("Copy localy", m_CopyIntoStreamingAssets);
                GUI.enabled = true;
            });

            //
            // Import Details
            AlembicUI.AddHorzLine(() =>
            {
                EditorGUILayout.LabelField("Details:", EditorStyles.boldLabel);
            });
            AlembicUI.AddHorzLine(() =>
            {
                m_ImportSettings.m_swapHandedness = EditorGUILayout.Toggle("Swap handedness", m_ImportSettings.m_swapHandedness);
            });
            AlembicUI.AddHorzLine(() =>
            {
                m_ImportSettings.m_swapFaceWinding = EditorGUILayout.Toggle("Swap face winding", m_ImportSettings.m_swapFaceWinding);
            });
            AlembicUI.AddHorzLine(() =>
            {
                m_ImportSettings.m_submeshPerUVTile = EditorGUILayout.Toggle("Submesh per UV tile",
                    m_ImportSettings.m_submeshPerUVTile);
            });
            AlembicUI.AddHorzLine(() =>
            {
                string[] enumNamesList = System.Enum.GetNames(typeof(AbcAPI.aiNormalsMode));
                m_ImportSettings.m_normalsMode =
                    (AbcAPI.aiNormalsMode) EditorGUILayout.Popup("Normals", (int) m_ImportSettings.m_normalsMode, enumNamesList);
            });
            AlembicUI.AddHorzLine(() =>
            {
                string[] enumNamesList = System.Enum.GetNames(typeof(AbcAPI.aiTangentsMode));
                m_ImportSettings.m_tangentsMode =
                    (AbcAPI.aiTangentsMode) EditorGUILayout.Popup("Tangents", (int) m_ImportSettings.m_tangentsMode, enumNamesList);
            });
            AlembicUI.AddHorzLine(() =>
            {
                string[] enumNamesList = System.Enum.GetNames(typeof(AbcAPI.aiAspectRatioMode));
                m_ImportSettings.m_aspectRatioMode =
                    (AbcAPI.aiAspectRatioMode)
                    EditorGUILayout.Popup("Aspect ration", (int) m_ImportSettings.m_aspectRatioMode, enumNamesList);
            });

            //
            // Advanced
            AlembicUI.AddHorzLine(() =>
            {
                EditorGUILayout.LabelField("Advanced:", EditorStyles.boldLabel);
            });

            AlembicUI.AddHorzLine(() =>
            {
                m_ImportSettings.m_useThreads = EditorGUILayout.Toggle("Use ABC threads", m_ImportSettings.m_useThreads);
            });
            AlembicUI.AddHorzLine(() =>
            {
                m_ImportSettings.m_sampleCacheSize = EditorGUILayout.IntField("Sample cache size",
                    m_ImportSettings.m_sampleCacheSize);
            });


            GUI.enabled = m_ImportSettings.m_pathToAbc != "";

            if (GUILayout.Button("Import"))
            {
                ImportAsset();
                Close();
            }
        }

        private void ImportAsset()
        {
            if (m_CopyIntoStreamingAssets)
            {
                var newFile = Path.Combine(Application.streamingAssetsPath, "AlembicData");
                newFile = Path.Combine(newFile, System.IO.Path.GetFileName(m_ImportSettings.m_pathToAbc));
                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(newFile)))
                        Directory.CreateDirectory(Path.GetDirectoryName(newFile));

                    File.Copy(m_ImportSettings.m_pathToAbc, newFile, true);
                }
                catch
                {
                }
                m_ImportSettings.m_pathToAbc = newFile;
            }

            var rootGO = AlembicImportTasker.Import(m_ImportMode, m_ImportSettings, m_DiagSettings, FinalizeImport );

            if(m_GeneratePrefab)
                DestroyImmediate(rootGO);
        }

        private void FinalizeImport(AlembicStream stream, GameObject newGo, AlembicStreamDescriptor streamDescr)
        {
            if (m_GeneratePrefab)
            {
                var prefabPath = "Assets/" + newGo.name + ".prefab";

                var prefab = PrefabUtility.CreatePrefab(prefabPath, newGo);
                AssetDatabase.SaveAssets();

                if (m_ImportMode > AlembicImportMode.NoSupportForStreaming)
                {
                    AssetDatabase.AddObjectToAsset(streamDescr, prefabPath);
                    AssetDatabase.SaveAssets();

                    if (m_ImportMode == AlembicImportMode.AutomaticStreamingSetup)
                    {
                        var streamDescrAsset =
                            AssetDatabase.LoadAssetAtPath(prefabPath, typeof(AlembicStreamDescriptor)) as AlembicStreamDescriptor;
                        var player = prefab.GetComponent<AlembicStreamPlayer>();
                        player.m_StreamDescriptor = streamDescrAsset;
                    }
                }
                AssetDatabase.SaveAssets();

                SetDefaultMaterial(prefab, prefab.transform);

                AssetDatabase.SaveAssets();
            }
        }


        private void SetDefaultMaterial( GameObject prefab, Transform goNode, Material defaultMat = null )
        {
            if (defaultMat == null)
            {
                defaultMat = new Material(Shader.Find("Standard")) { };
                AssetDatabase.AddObjectToAsset(defaultMat, prefab);
            }

            // Set new mat
            var renderer = goNode.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.material = defaultMat;

            for ( int i = 0; i < goNode.childCount; i++)
                SetDefaultMaterial(prefab, goNode.GetChild(i), defaultMat);
        }

    }
}
