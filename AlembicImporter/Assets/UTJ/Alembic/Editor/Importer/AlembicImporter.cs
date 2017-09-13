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
            System.IO.File.Delete(fullStreamingAssetPath);
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
                System.IO.File.Delete(destPath);    
            }
            else if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }
            System.IO.File.Move(sourcePath, destPath);
            if (System.IO.File.Exists(sourcePath + ".meta"))
            {
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
        [HideInInspector][SerializeField] public AlembicDiagnosticSettings m_diagSettings = new AlembicDiagnosticSettings();
        [HideInInspector][SerializeField] public AlembicImportMode m_importMode = AlembicImportMode.AutomaticStreamingSetup;

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

            CollectSubAssets(ctx, stream.AlembicTreeRoot, material);
        }

        private void CollectSubAssets(AssetImportContext ctx, AlembicTreeNode node,  Material mat)
        {
            if (m_ImportSettings.m_importMeshes)
            {
                var meshFilter = node.linkedGameObj.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    var m = meshFilter.sharedMesh;
                    m.name = node.linkedGameObj.name;
                    ctx.AddSubAsset(m.name, m);
                }
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

