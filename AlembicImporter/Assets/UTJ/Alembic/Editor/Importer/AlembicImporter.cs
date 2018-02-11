#if UNITY_2017_1_OR_NEWER

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UTJ.Alembic
{
    public class AlembicAssetModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions rao)
        {
            if (Path.GetExtension(assetPath.ToLower()) != ".abc")
                return AssetDeleteResult.DidNotDelete;
            var streamingAssetPath = AlembicImporter.MakeShortAssetPath(assetPath);
            AlembicStream.DisconnectStreamsWithPath(streamingAssetPath);

            try
            {
                var fullStreamingAssetPath = Application.streamingAssetsPath + streamingAssetPath;
                File.SetAttributes(fullStreamingAssetPath, FileAttributes.Normal);
                File.Delete(fullStreamingAssetPath);
                File.SetAttributes(fullStreamingAssetPath + ".meta", FileAttributes.Normal);
                File.Delete(fullStreamingAssetPath + ".meta");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(e);
            }

            return AssetDeleteResult.DidNotDelete;
        }

        public static AssetMoveResult OnWillMoveAsset(string from, string to)
        {
            if (Path.GetExtension(from.ToLower()) != ".abc")
                return AssetMoveResult.DidNotMove;
            var streamDstPath = AlembicImporter.MakeShortAssetPath(to);
            var streamSrcPath = AlembicImporter.MakeShortAssetPath(from);
            AlembicStream.DisconnectStreamsWithPath(streamSrcPath);
            AlembicStream.RemapStreamsWithPath(streamSrcPath,streamDstPath);

            var dstPath = Application.streamingAssetsPath + streamDstPath;
            var srcPath = Application.streamingAssetsPath + streamSrcPath;

            try
            {
                var directoryPath = Path.GetDirectoryName(dstPath);
                if (File.Exists(dstPath))
                {
                    File.SetAttributes(dstPath + ".meta", FileAttributes.Normal);
                    File.Delete(dstPath);
                }
                else if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                if (File.Exists(dstPath))
                    File.SetAttributes(dstPath, FileAttributes.Normal);
                File.Move(srcPath, dstPath);
                if (File.Exists(dstPath + ".meta"))
                {
                    File.SetAttributes(dstPath + ".meta", FileAttributes.Normal);
                    File.Move(srcPath + ".meta", dstPath + ".meta");
                }

                AssetDatabase.Refresh(ImportAssetOptions.Default);
                AlembicStream.ReconnectStreamsWithPath(streamDstPath);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(e);
            }
            return AssetMoveResult.DidNotMove;
        } 
    }

    [ScriptedImporter(1, "abc")]
    public class AlembicImporter : ScriptedImporter
    {
        [SerializeField] public AlembicStreamSettings streamSettings = new AlembicStreamSettings();
        [SerializeField] public double abcStartTime; // read only
        [SerializeField] public double abcEndTime;   // read only
        [SerializeField] public double startTime = double.MinValue;
        [SerializeField] public double endTime = double.MaxValue;
        [SerializeField] public string importWarning;
        [SerializeField] public List<string> varyingTopologyMeshNames = new List<string>();
        [SerializeField] public List<string> splittingMeshNames = new List<string>();

        public static string MakeShortAssetPath(string assetPath)
        {
            return Regex.Replace(assetPath, "^Assets", "");
        }


        public override void OnImportAsset(AssetImportContext ctx)
        {
            var shortAssetPath = MakeShortAssetPath(ctx.assetPath);
            AlembicStream.DisconnectStreamsWithPath(shortAssetPath);
            var sourcePath = Application.dataPath + shortAssetPath;
            var destPath = Application.streamingAssetsPath + shortAssetPath;
            var directoryPath = Path.GetDirectoryName(destPath);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            if (File.Exists(destPath))
                File.SetAttributes(destPath, FileAttributes.Normal);
            File.Copy(sourcePath, destPath ,true);

            var fileName = Path.GetFileNameWithoutExtension(destPath);
            var go = new GameObject(fileName);
            
            var streamDescriptor = ScriptableObject.CreateInstance<AlembicStreamDescriptor>();
            streamDescriptor.name = go.name + "_ABCDesc";
            streamDescriptor.pathToAbc = shortAssetPath;
            streamDescriptor.settings = streamSettings;

            using (var abcStream = new AlembicStream(go, streamDescriptor))
            {
                abcStream.AbcLoad(true);

                var tr = abcStream.abcTimeRange;
                streamDescriptor.abcStartTime = abcStartTime = startTime = tr.startTime;
                streamDescriptor.abcEndTime = abcEndTime = endTime = tr.endTime;

                var streamPlayer = go.AddComponent<AlembicStreamPlayer>();
                streamPlayer.streamDescriptor = streamDescriptor;
                streamPlayer.startTime = startTime;
                streamPlayer.endTime = endTime;

                var subassets = new Subassets(ctx);
                subassets.Add(streamDescriptor.name, streamDescriptor);
                GenerateSubAssets(subassets, abcStream.abcTreeRoot, streamDescriptor);

                AlembicStream.ReconnectStreamsWithPath(shortAssetPath);

#if UNITY_2017_3_OR_NEWER
                ctx.AddObjectToAsset(go.name, go);
                ctx.SetMainObject(go);
#else
                ctx.SetMainAsset(go.name, go);
#endif
            }  
        }

        class Subassets
        {
            AssetImportContext m_ctx;
            Material m_defaultMaterial;
            Material m_defaultPointsMaterial;
            Material m_defaultPointsMotionVectorMaterial;

            public Subassets(AssetImportContext ctx)
            {
                m_ctx = ctx;
            }

            public Material defaultMaterial
            {
                get
                {
                    if (m_defaultMaterial == null)
                    {
                        m_defaultMaterial = new Material(Shader.Find("Alembic/Standard"));
                        m_defaultMaterial.hideFlags = HideFlags.NotEditable;
                        m_defaultMaterial.name = "Default Material";
                        Add("Default Material", m_defaultMaterial);
                    }
                    return m_defaultMaterial;
                }
            }

            public Material pointsMaterial
            {
                get
                {
                    if (m_defaultPointsMaterial == null)
                    {
                        m_defaultPointsMaterial = new Material(Shader.Find("Alembic/Points Standard"));
                        m_defaultPointsMaterial.hideFlags = HideFlags.NotEditable;
                        m_defaultPointsMaterial.name = "Default Points";
                        Add("Default Points", m_defaultPointsMaterial);
                    }
                    return m_defaultPointsMaterial;
                }
            }

            public Material pointsMotionVectorMaterial
            {
                get
                {
                    if (m_defaultPointsMotionVectorMaterial == null)
                    {
                        m_defaultPointsMotionVectorMaterial = new Material(Shader.Find("Alembic/PointsMotionVectors"));
                        m_defaultPointsMotionVectorMaterial.hideFlags = HideFlags.NotEditable;
                        m_defaultPointsMotionVectorMaterial.name = "Points Motion Vector";
                        Add("Points Motion Vector", m_defaultPointsMotionVectorMaterial);
                    }
                    return m_defaultPointsMotionVectorMaterial;
                }
            }

            public void Add(string identifier, Object asset)
            {
#if UNITY_2017_3_OR_NEWER
                m_ctx.AddObjectToAsset(identifier, asset);
#else
                m_ctx.AddSubAsset(identifier, asset);
#endif
            }
        }

        private void GenerateSubAssets(Subassets subassets, AlembicTreeNode root, AlembicStreamDescriptor streamDescr)
        {
            if (streamDescr.duration > 0)
            {
                var frames = new Keyframe[2];
                frames[0].value = 0.0f;
                frames[0].time = 0.0f;
                frames[0].outTangent = 1.0f;
                frames[1].value = (float)streamDescr.duration;
                frames[1].time = (float)streamDescr.duration;
                frames[1].inTangent = 1.0f;

                var curve = new AnimationCurve(frames);
                AnimationUtility.SetKeyLeftTangentMode(curve, 0, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, 1, AnimationUtility.TangentMode.Linear);

                var animationClip = new AnimationClip();
                animationClip.SetCurve("", typeof(AlembicStreamPlayer), "currentTime", curve);
                animationClip.name = root.gameObject.name + "_Clip";
                animationClip.hideFlags = HideFlags.NotEditable;

                subassets.Add("Default Animation", animationClip);
            }
            varyingTopologyMeshNames = new List<string>();
            splittingMeshNames = new List<string>();

            CollectSubAssets(subassets, root);

            streamDescr.hasVaryingTopology = varyingTopologyMeshNames.Count > 0;
        }

        private void CollectSubAssets(Subassets subassets, AlembicTreeNode node)
        {
            var mesh = node.GetAlembicObj<AlembicMesh>();
            if (mesh != null)
            {
                var sum = mesh.summary;
                if (mesh.summary.topologyVariance == aiTopologyVariance.Heterogeneous)
                    varyingTopologyMeshNames.Add(node.gameObject.name);
                else if (mesh.sampleSummary.splitCount > 1)
                    splittingMeshNames.Add(node.gameObject.name);
            }

            int submeshCount = 0;
            var meshFilter = node.gameObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                var m = meshFilter.sharedMesh;
                submeshCount = m.subMeshCount;
                m.name = node.gameObject.name;
                subassets.Add(m.name, m);
            }

            var renderer = node.gameObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mats = new Material[submeshCount];
                for (int i = 0; i < submeshCount; ++i)
                    mats[i] = subassets.defaultMaterial;
                renderer.sharedMaterials = mats;
            }

            var apr = node.gameObject.GetComponent<AlembicPointsRenderer>();
            if (apr != null)
            {
                var cubeGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                apr.sharedMesh = cubeGO.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(cubeGO);

                apr.sharedMaterials = new Material[] { subassets.pointsMaterial };
                apr.motionVectorMaterial = subassets.pointsMotionVectorMaterial;
            }

            foreach ( var child in node.children)
                CollectSubAssets(subassets, child);
        }
    }
}

#endif

