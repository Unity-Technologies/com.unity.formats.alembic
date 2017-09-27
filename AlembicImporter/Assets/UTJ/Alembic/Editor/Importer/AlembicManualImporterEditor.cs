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


#if !UNITY_2017_1_OR_NEWER && !ENABLE_SCRIPTED_IMPORTERS

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
                m_ImportSettings.m_pathToAbc = new DataPath(EditorGUILayout.TextField(m_ImportSettings.m_pathToAbc.GetFullPath()));
                if (GUILayout.Button("..."))
                {
                    m_ImportSettings.m_pathToAbc = new DataPath(
                        EditorUtility.OpenFilePanel("Select Alembic File", m_ImportSettings.m_pathToAbc.GetFullPath(), "abc"));
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
                m_ImportSettings.m_shareVertices = EditorGUILayout.Toggle("Merge Vertices (experimental)", m_ImportSettings.m_shareVertices);
            });

            AlembicUI.AddHorzLine(() =>
            {
                m_ImportSettings.m_treatVertexExtraDataAsStatics = EditorGUILayout.Toggle("Vertex extra data is static (experimental)", m_ImportSettings.m_treatVertexExtraDataAsStatics);
            });
            AlembicUI.AddHorzLine(() =>
            {
                m_ImportSettings.m_scaleFactor = EditorGUILayout.FloatField("m_scaleFactor", m_ImportSettings.m_scaleFactor);
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
                m_ImportSettings.m_cacheSamples = EditorGUILayout.Toggle("Cache samples",
                    m_ImportSettings.m_cacheSamples);
            });


            GUI.enabled = m_ImportSettings.m_pathToAbc.leaf != "";

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
                newFile = Path.Combine(newFile, System.IO.Path.GetFileName(m_ImportSettings.m_pathToAbc.leaf));
                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(newFile)))
                        Directory.CreateDirectory(Path.GetDirectoryName(newFile));

                    File.Copy(m_ImportSettings.m_pathToAbc.GetFullPath(), newFile, true);
                }
                catch
                {
                }
                m_ImportSettings.m_pathToAbc = new DataPath(newFile);
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

                SetDefaultAssets(prefab, prefab.transform);
                AssetDatabase.SaveAssets();
            }
        }


        class SetDefaultAssetsContext
        {
            public Material defaultMat = null;
            public Material defaultPointsMat = null;
            public Mesh defaultPointsMesh = null;
        }

        private void SetDefaultAssets( GameObject prefab, Transform goNode, SetDefaultAssetsContext ctx = null)
        {
            if(ctx == null)
            {
                ctx = new SetDefaultAssetsContext();
            }

            // set default material for MeshRenderer
            var mrenderer = goNode.GetComponent<MeshRenderer>();
            if (mrenderer != null)
            {
                if (ctx.defaultMat == null)
                {
                    ctx.defaultMat = new Material(Shader.Find("Standard")) { };
                    AssetDatabase.AddObjectToAsset(ctx.defaultMat, prefab);
                }
                mrenderer.material = ctx.defaultMat;
            }

            // set default material and mesh for AlembicPointsRenderer
            var prenderer = goNode.GetComponent<AlembicPointsRenderer>();
            if (prenderer != null)
            {
                if (ctx.defaultPointsMat == null)
                {
                    ctx.defaultPointsMat = new Material(Shader.Find("Alembic/Standard Instanced"));
                    ctx.defaultPointsMat.name = "Points";
#if UNITY_5_6_OR_NEWER
                    ctx.defaultPointsMat.enableInstancing = true;
#endif
                    AssetDatabase.AddObjectToAsset(ctx.defaultPointsMat, prefab);
                }
                if (ctx.defaultPointsMesh == null)
                {
                    ctx.defaultPointsMesh = IcoSphereGenerator.Generate();
                    ctx.defaultPointsMesh.name = "Points";
                    if (ctx.defaultPointsMesh != null)
                    {
                        AssetDatabase.AddObjectToAsset(ctx.defaultPointsMesh, prefab);
                    }
                }
                prenderer.material = ctx.defaultPointsMat;
                prenderer.sharedMesh = ctx.defaultPointsMesh;
            }

            for ( int i = 0; i < goNode.childCount; i++)
            {
                SetDefaultAssets(prefab, goNode.GetChild(i), ctx);
            }
        }
    }
}
