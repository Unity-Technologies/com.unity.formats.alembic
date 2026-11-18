using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    // UUM-160861: native calls must marshal strings as UTF-8 (CoreCLR defaults to the ANSI code page).
    public class NonAsciiPathTests
    {
        const string cubesColouredGuid = "f30dca527f4e947248046d6d12750c2d";

        [TestCase("Assets/Kėdė.abc")]
        [TestCase("Assets/Café.abc")]
        [TestCase("Assets/椅子.abc")]
        public void ImportingFileWithNonAsciiName_Works(string dest)
        {
            try
            {
                var src = AssetDatabase.GUIDToAssetPath(cubesColouredGuid);
                Assert.IsTrue(AssetDatabase.CopyAsset(src, dest));
                AssetDatabase.ImportAsset(dest, ImportAssetOptions.ForceSynchronousImport);

                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(dest);
                Assert.IsNotNull(asset, "Asset failed to import");
                Assert.IsNotNull(asset.GetComponentInChildren<MeshFilter>(), "Imported asset has no meshes");
                Assert.IsNotNull(asset.GetComponent<AlembicStreamPlayer>());
            }
            finally
            {
                AssetDatabase.DeleteAsset(dest);
            }
        }
    }
}
