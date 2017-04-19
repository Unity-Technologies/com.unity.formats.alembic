#if UNITY_2017_1_OR_NEWER

using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;

namespace UTJ.Alembic
{
    [ScriptedImporter(1, "abc")]
    public class AlembicImporter : ScriptedImporter
    {
        [SerializeField] public AlembicImportSettings m_ImportSettings = new AlembicImportSettings();
        [HideInInspector][SerializeField] public AlembicDiagnosticSettings m_diagSettings = new AlembicDiagnosticSettings();
        [HideInInspector][SerializeField] public AlembicImportMode m_importMode = AlembicImportMode.AutomaticStreamingSetup;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            m_ImportSettings.m_pathToAbc =  new DataPath(ctx.assetPath);
            var mainObject = AlembicImportTasker.Import(m_importMode, m_ImportSettings, m_diagSettings, (stream, mainGO, streamDescr) =>
            {
                GenerateSubAssets(ctx, mainGO, stream);
                if(streamDescr != null)
                    ctx.AddSubAsset( mainGO.name, streamDescr);
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

