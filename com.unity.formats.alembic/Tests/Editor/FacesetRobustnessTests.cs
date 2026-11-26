using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    class FacesetRobustnessTests
    {
        // Fixtures are ".abc.bytes" TextAssets so the importer does not run on project load.
        const string k_TempFixtureDir = "Assets/TempFacesetFixtures";

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(k_TempFixtureDir))
                AssetDatabase.DeleteAsset(k_TempFixtureDir);
        }

        static MeshFilter ImportAndGetMeshFilter(string fixtureName)
        {
            var guids = AssetDatabase.FindAssets($"{fixtureName}.abc t:TextAsset");
            Assert.AreEqual(1, guids.Length, $"Fixture '{fixtureName}' was not found.");
            var bytes = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            Directory.CreateDirectory(k_TempFixtureDir);
            var dst = $"{k_TempFixtureDir}/{fixtureName}.abc";
            File.WriteAllBytes(dst, bytes.bytes);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceSynchronousImport);

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(dst);
            Assert.IsNotNull(asset, $"{fixtureName}.abc failed to import.");
            var inst = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            inst.GetComponent<AlembicStreamPlayer>().UpdateImmediately(0);
            var meshFilter = inst.GetComponentInChildren<MeshFilter>();
            Assert.IsNotNull(meshFilter.sharedMesh);
            return meshFilter;
        }

        // Faces: tri, zero-count, tri, tri. Face sets: a = {0}, b = {2, 3}.
        [Test]
        public void ZeroCountFace_KeepsTheRightFacesInEachSubmesh()
        {
            var mesh = ImportAndGetMeshFilter("faceset_zero_count_face").sharedMesh;
            Assert.AreEqual(2, mesh.subMeshCount);
            Assert.AreEqual(3, (int)mesh.GetIndexCount(0));
            Assert.AreEqual(6, (int)mesh.GetIndexCount(1));
            Assert.AreEqual(9, mesh.triangles.Length);
        }

        // Face 1 is in no face set, "unused" and "unused2" are empty and
        // "used" holds face indices 0, 1000000000 and -1.
        [Test]
        public void UnassignedFacesAndUnusedFaceSet_AreImportedWithoutOutOfRangeAccess()
        {
            var meshFilter = ImportAndGetMeshFilter("faceset_unassigned_unused");
            var mesh = meshFilter.sharedMesh;
            Assert.AreEqual(2, mesh.subMeshCount);
            Assert.AreEqual(3, (int)mesh.GetIndexCount(0));
            Assert.AreEqual(3, (int)mesh.GetIndexCount(1));
            Assert.AreEqual(6, mesh.triangles.Length);

            // The unused face sets must not drop the names of the used ones.
            var names = meshFilter.GetComponent<AlembicCustomData>().FaceSetNames;
            Assert.AreEqual(2, names.Count);
            Assert.AreEqual("used", names[0]);
            Assert.AreEqual("", names[1]);
        }
    }
}
