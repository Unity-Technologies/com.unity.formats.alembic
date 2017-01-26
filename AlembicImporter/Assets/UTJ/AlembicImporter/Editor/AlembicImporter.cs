#if UNITY_5_7_OR_NEWER || ENABLE_SCRIPTED_IMPORTERS

using UnityEditor;
using UnityEngine;

namespace UTJ.Alembic
{
	[ScriptedImporter(1, "abc")]
    public class AlembicImporter : ScriptedImporter
	{
		[SerializeField] public AlembicImportSettings m_ImportSettings = new AlembicImportSettings();
		[HideInInspector][SerializeField] public AlembicDiagnosticSettings m_diagSettings = new AlembicDiagnosticSettings();
		[HideInInspector][SerializeField] public AlembicImportMode m_importMode = AlembicImportMode.AutomaticStreamingSetup;

		public override void OnImportAsset(ImportAssetEventArgs args)
		{
			m_ImportSettings.m_pathToAbc = args.AssetSourcePath;
			var mainObject = AlembicImportTasker.Import(m_importMode, m_ImportSettings, m_diagSettings, (stream, mainGO, streamDescr) =>
			{
				GenerateSubAssets(args, mainGO, stream);
				if(streamDescr != null)
					args.AddSubAsset( mainGO.name, streamDescr);
			});
			args.SetMainAsset(mainObject.name, mainObject);
		}

		private void GenerateSubAssets(ImportAssetEventArgs args, GameObject go, AlembicStream stream)
		{
			var material = new Material(Shader.Find("Standard")) { };
			args.AddSubAsset("Default Material", material);

			CollectSubAssets(stream.AlembicTreeRoot, args, material);
		}

		private void CollectSubAssets(AlembicTreeNode node, ImportAssetEventArgs args, Material mat)
		{
			if (m_ImportSettings.m_importMeshes)
			{
				var meshFilter = node.linkedGameObj.GetComponent<MeshFilter>();
				if (meshFilter != null)
				{
					var m = meshFilter.sharedMesh;
					m.name = node.linkedGameObj.name;
					args.AddSubAsset(m.name, m);
				}
			}

			var renderer = node.linkedGameObj.GetComponent<MeshRenderer>();
			if (renderer != null)
				renderer.material = mat;

			foreach( var child in node.children )
				CollectSubAssets(child, args, mat);
		}

	}
}

#endif

