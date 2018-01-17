#if UNITY_2017_1_OR_NEWER

using System.Collections.Generic;
using System.IO;
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
            var streamingAssetPath = assetPath.Replace("Assets","");
            AlembicStream.DisconnectStreamsWithPath(streamingAssetPath);

            var fullStreamingAssetPath = Application.streamingAssetsPath + streamingAssetPath;
            File.SetAttributes(fullStreamingAssetPath, FileAttributes.Normal);
            File.Delete(fullStreamingAssetPath);
            File.SetAttributes(fullStreamingAssetPath + ".meta", FileAttributes.Normal);
            File.Delete(fullStreamingAssetPath + ".meta");

            return AssetDeleteResult.DidNotDelete;
        }

        public static AssetMoveResult OnWillMoveAsset(string from, string to)
        {
            if (Path.GetExtension(from.ToLower()) != ".abc")
                return AssetMoveResult.DidNotMove;
            var streamDestPath = to.Replace("Assets" , "");
            var streamSourcePath = from.Replace("Assets" , "");
            AlembicStream.DisconnectStreamsWithPath(streamSourcePath);
            AlembicStream.RemapStreamsWithPath(streamSourcePath,streamDestPath);

            var destPath = Application.streamingAssetsPath + streamDestPath;
            var sourcePath = Application.streamingAssetsPath + streamSourcePath;

            var directoryPath = Path.GetDirectoryName(destPath);
            if (File.Exists(destPath))
            {
                File.SetAttributes(destPath + ".meta", FileAttributes.Normal);
                File.Delete(destPath);    
            }
            else if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            if (File.Exists(destPath))
                File.SetAttributes(destPath, FileAttributes.Normal);
            File.Move(sourcePath, destPath);
            if (File.Exists(destPath + ".meta"))
            {
                File.SetAttributes(destPath + ".meta", FileAttributes.Normal);
                File.Move(sourcePath + ".meta", destPath+ ".meta");    
            }
            AssetDatabase.Refresh(ImportAssetOptions.Default);
            AlembicStream.ReconnectStreamsWithPath(streamDestPath);        

            return AssetMoveResult.DidNotMove;
        } 
    }
        
    [ScriptedImporter(1, "abc")]
    public class AlembicImporter : ScriptedImporter
    {
        [SerializeField] public AlembicStreamSettings streamSettings = new AlembicStreamSettings();
        [SerializeField] public float scaleFactor = 0.01f;
        [SerializeField] public int startFrame = int.MinValue;
        [SerializeField] public int endFrame = int.MaxValue;        
        [SerializeField] public float AbcStartTime;
        [SerializeField] public float AbcEndTime;
        [SerializeField] public int AbcFrameCount;
        [SerializeField] public string importWarning;
        [SerializeField] public List<string> varyingTopologyMeshNames = new List<string>();
        [SerializeField] public List<string> splittingMeshNames = new List<string>();

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var shortAssetPath = ctx.assetPath.Replace("Assets", "");
            AlembicStream.DisconnectStreamsWithPath(shortAssetPath);
            var sourcePath = Application.dataPath + shortAssetPath;
            var destPath = Application.streamingAssetsPath + shortAssetPath;
            var directoryPath = Path.GetDirectoryName(destPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            if (File.Exists(destPath))
                File.SetAttributes(destPath, FileAttributes.Normal);
            File.Copy(sourcePath, destPath ,true);

            var fileName = Path.GetFileNameWithoutExtension(destPath);
            var go = new GameObject(fileName);
            go.transform.localScale *= scaleFactor;
            
            AlembicStreamDescriptor streamDescriptor = ScriptableObject.CreateInstance<AlembicStreamDescriptor>();
            streamDescriptor.name = go.name + "_ABCDesc";
            streamDescriptor.pathToAbc = shortAssetPath;
            streamDescriptor.settings = streamSettings;

            using (var abcStream = new AlembicStream(go, streamDescriptor))
            {
                abcStream.AbcLoad();
                AbcStartTime = abcStream.AbcStartTime;
                AbcEndTime = abcStream.AbcEndTime;
                AbcFrameCount = abcStream.AbcFrameCount;

                startFrame = startFrame < 0 ? 0 : startFrame;
                endFrame = endFrame > AbcFrameCount-1 ? AbcFrameCount-1 : endFrame;

                streamDescriptor.minFrame = startFrame;
                streamDescriptor.maxFrame = endFrame;
                streamDescriptor.abcFrameCount = AbcFrameCount;
                streamDescriptor.abcDuration = AbcEndTime - AbcStartTime;
                streamDescriptor.abcStartTime = AbcStartTime;

                var streamPlayer = go.AddComponent<AlembicStreamPlayer>();
                streamPlayer.streamDescriptor = streamDescriptor;
                streamPlayer.startFrame = startFrame;
                streamPlayer.endFrame = endFrame;

                AddObjectToAsset(ctx,streamDescriptor.name, streamDescriptor);
                GenerateSubAssets(ctx, abcStream.alembicTreeRoot,streamDescriptor);

                AlembicStream.ReconnectStreamsWithPath(shortAssetPath);
           
#if UNITY_2017_3_OR_NEWER
                ctx.AddObjectToAsset(go.name, go);
                ctx.SetMainObject(go);
#else
                ctx.SetMainAsset(go.name, go);
#endif
            }  
        }

        private void GenerateSubAssets( AssetImportContext ctx,AlembicTreeNode root,AlembicStreamDescriptor streamDescr)
        {
            var material = new Material(Shader.Find("Standard"));
            AddObjectToAsset(ctx,"Default Material", material);

            if (streamDescr.Duration>0)
            {
                Keyframe[] frames = new Keyframe[2];
                frames[0].value = 0.0f;
                frames[0].time = 0.0f;
                frames[0].tangentMode = (int)AnimationUtility.TangentMode.Linear;
                frames[0].outTangent = 1.0f;
                frames[1].value = streamDescr.Duration;
                frames[1].time = streamDescr.Duration;
                frames[1].tangentMode = (int)AnimationUtility.TangentMode.Linear;
                frames[1].inTangent = 1.0f;
                AnimationCurve curve = new AnimationCurve(frames); 
                var animationClip = new AnimationClip();
                animationClip.SetCurve("",typeof(AlembicStreamPlayer),"currentTime",curve);
                animationClip.name = root.linkedGameObj.name + "_Clip";

                AddObjectToAsset(ctx,"Default Animation", animationClip);
            }
            varyingTopologyMeshNames = new List<string>();
            splittingMeshNames = new List<string>();

            CollectSubAssets(ctx, root, material);
            
            streamDescr.hasVaryingTopology = varyingTopologyMeshNames.Count > 0;
        }

        private void CollectSubAssets(AssetImportContext ctx, AlembicTreeNode node,  Material mat)
        {
            AlembicMesh mesh = node.GetAlembicObj<AlembicMesh>();
            if (mesh!=null)
            {
                if ((streamSettings.shareVertices || streamSettings.treatVertexExtraDataAsStatics) && 
                    mesh.summary.topologyVariance == AbcAPI.aiTopologyVariance.Heterogeneous)
                {
                    varyingTopologyMeshNames.Add(node.linkedGameObj.name);   
                }
                else if (streamSettings.shareVertices && mesh.sampleSummary.splitCount > 1)
                {
                    splittingMeshNames.Add(node.linkedGameObj.name);
                }
            }

            var meshFilter = node.linkedGameObj.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                var m = meshFilter.sharedMesh;
                m.name = node.linkedGameObj.name;
                AddObjectToAsset(ctx,m.name, m);
            }

            var renderer = node.linkedGameObj.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = mat;

            foreach( var child in node.children )
                CollectSubAssets(ctx, child, mat);
        }

        private static void AddObjectToAsset(AssetImportContext ctx,string identifier, Object asset)
        {
#if UNITY_2017_3_OR_NEWER
            ctx.AddObjectToAsset(identifier, asset);
#else
            ctx.AddSubAsset(identifier, asset);
#endif
        }
    }
}

#endif

