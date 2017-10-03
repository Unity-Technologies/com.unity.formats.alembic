#if UNITY_2017_1_OR_NEWER

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;

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
            System.IO.File.Delete(fullStreamingAssetPath);
            File.SetAttributes(fullStreamingAssetPath + ".meta", FileAttributes.Normal);
            System.IO.File.Delete(fullStreamingAssetPath + ".meta");

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
            if (System.IO.File.Exists(destPath))
            {
                File.SetAttributes(destPath + ".meta", FileAttributes.Normal);
                System.IO.File.Delete(destPath);    
            }
            else if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }
            if (System.IO.File.Exists(destPath))
                File.SetAttributes(destPath, FileAttributes.Normal);
            System.IO.File.Move(sourcePath, destPath);
            if (System.IO.File.Exists(destPath + ".meta"))
            {
                File.SetAttributes(destPath + ".meta", FileAttributes.Normal);
                System.IO.File.Move(sourcePath + ".meta", destPath+ ".meta");    
            }
            AssetDatabase.Refresh(ImportAssetOptions.Default);
            AlembicStream.ReconnectStreamsWithPath(streamDestPath);        

            return AssetMoveResult.DidNotMove;
        } 
    }
        
    [ScriptedImporter(1, "abc")]
    public class AlembicImporter : ScriptedImporter
    {
        [SerializeField] public AlembicImportSettings m_ImportSettings = new AlembicImportSettings();
        [SerializeField] public AlembicDiagnosticSettings m_diagSettings = new AlembicDiagnosticSettings();
        [SerializeField] public AlembicImportMode m_importMode = AlembicImportMode.AutomaticStreamingSetup;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var shortAssetPath = ctx.assetPath.Replace("Assets", "");
            AlembicStream.DisconnectStreamsWithPath(shortAssetPath);
            var sourcePath = Application.dataPath + shortAssetPath;
            var destPath = Application.streamingAssetsPath + shortAssetPath;
            var directoryPath = Path.GetDirectoryName(destPath);
            if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }
            if (System.IO.File.Exists(destPath))
                File.SetAttributes(destPath, FileAttributes.Normal);
            System.IO.File.Copy(sourcePath, destPath ,true);
            m_ImportSettings.m_pathToAbc =  new DataPath(destPath);

            var mainObject = AlembicImportTasker.Import(m_importMode, m_ImportSettings, m_diagSettings, (stream, mainGO, streamDescr) =>
            {
                GenerateSubAssets(ctx, mainGO, stream);
                if(streamDescr != null)
                    ctx.AddSubAsset( mainGO.name, streamDescr);

                AlembicStream.ReconnectStreamsWithPath(shortAssetPath);
            });
            ctx.SetMainAsset(mainObject.name, mainObject);            
        }

        private void GenerateSubAssets( AssetImportContext ctx, GameObject go, AlembicStream stream)
        {
            var material = new Material(Shader.Find("Standard")) { };
            ctx.AddSubAsset("Default Material", material);

            var duration = m_ImportSettings.m_endTime - m_ImportSettings.m_startTime;
            if (m_importMode == AlembicImportMode.AutomaticStreamingSetup && duration>0)
            {
                Keyframe[] frames = new Keyframe[2];
                frames[0].value = 0.0f;
                frames[0].time = 0.0f;
                frames[0].tangentMode = (int)AnimationUtility.TangentMode.Linear;
                frames[0].outTangent = 1.0f;
                frames[1].value = duration;
                frames[1].time = duration;
                frames[1].tangentMode = (int)AnimationUtility.TangentMode.Linear;
                frames[1].inTangent = 1.0f;
                AnimationCurve curve = new AnimationCurve(frames); 
                var animationClip = new AnimationClip();
                animationClip.SetCurve("",typeof(AlembicStreamPlayer),"m_time",curve);
                animationClip.name = "Default Animation";
                ctx.AddSubAsset("Default Animation", animationClip);
            }

            CollectSubAssets(ctx, stream.AlembicTreeRoot, material);
        }

        private static float CalculateLinearTangent(AnimationCurve curve, int index, int toIndex)
        {
             return (float) (((double) curve[index].value - (double) curve[toIndex].value) / ((double) curve[index].time - (double) curve[toIndex].time));
        }

        private static void CollectSubAssets(AssetImportContext ctx, AlembicTreeNode node,  Material mat)
        {
           
            var meshFilter = node.linkedGameObj.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                var m = meshFilter.sharedMesh;
                m.name = node.linkedGameObj.name;
                ctx.AddSubAsset(m.name, m);
            }

            var renderer = node.linkedGameObj.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.material = mat;

            foreach( var child in node.children )
                CollectSubAssets(ctx, child, mat);
        }

    }
}

#endif

