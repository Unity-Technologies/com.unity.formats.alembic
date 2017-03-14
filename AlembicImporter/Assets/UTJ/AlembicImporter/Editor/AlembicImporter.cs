#if ENABLE_SCRIPTED_IMPORTERS

using UnityEditor;
using UnityEngine;

namespace UTJ.Alembic
{
	[UnityEditor.Experimental.ScriptedImporter(1, "abc")]
    public class AlembicImporter : UnityEditor.Experimental.ScriptedImporter
	{
		[SerializeField] public AlembicImportSettings m_ImportSettings = new AlembicImportSettings();
		[HideInInspector][SerializeField] public AlembicDiagnosticSettings m_diagSettings = new AlembicDiagnosticSettings();
		[HideInInspector][SerializeField] public AlembicImportMode m_importMode = AlembicImportMode.AutomaticStreamingSetup;

		public override void OnImportAsset()
		{
			m_ImportSettings.m_pathToAbc = assetSourcePath;
			var mainObject = AlembicImportTasker.Import(m_importMode, m_ImportSettings, m_diagSettings, (stream, mainGO, streamDescr) =>
			{
				GenerateSubAssets(mainGO, stream);
				if(streamDescr != null)
					AddSubAsset( mainGO.name, streamDescr);
			});
			SetMainAsset(mainObject.name, mainObject);
		}

		private void GenerateSubAssets( GameObject go, AlembicStream stream)
		{
			var material = new Material(Shader.Find("Standard")) { };
			AddSubAsset("Default Material", material);

			CollectSubAssets(stream.AlembicTreeRoot, material);
		}

		private void CollectSubAssets(AlembicTreeNode node,  Material mat)
		{
			if (m_ImportSettings.m_importMeshes)
			{
				var meshFilter = node.linkedGameObj.GetComponent<MeshFilter>();
				if (meshFilter != null)
				{
					var m = meshFilter.sharedMesh;
					m.name = node.linkedGameObj.name;
					AddSubAsset(m.name, m);
				}
			}

			var renderer = node.linkedGameObj.GetComponent<MeshRenderer>();
			if (renderer != null)
				renderer.material = mat;

			foreach( var child in node.children )
				CollectSubAssets(child, mat);
		}

	}
}

#endif

