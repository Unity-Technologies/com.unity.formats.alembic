using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.TestTools;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    class ImportlessTests : BaseFixture
    {
        string path;
        AlembicStreamPlayer player;

        [SetUp]
        public new void SetUp()
        {
            const string dummyGUID = "1a066d124049a413fb12b82470b82811"; // GUID of DummyAlembic.abc
            path = AssetDatabase.GUIDToAssetPath(dummyGUID);
            var go = new GameObject("Alembic");
            player = go.AddComponent<AlembicStreamPlayer>();
        }

        [Test]
        public void RemovingAlembicStreamPlayerKeepsTheMesh()
        {
            Assert.IsTrue(player.LoadFromFile(path));
            var mesh = player.gameObject.GetComponentInChildren<MeshFilter>().sharedMesh;
            Assert.IsNotNull(mesh);
            Object.DestroyImmediate(player);
            Assert.IsNotNull(mesh);
        }

        [Test]
        public void ChangingImportParamsTakesEffectImmediately()
        {
            Assert.IsTrue(player.LoadFromFile(path));
            var mesh = player.gameObject.GetComponentInChildren<MeshFilter>().sharedMesh;
            Assert.IsNotNull(mesh);
            var p0 = player.gameObject.GetComponentInChildren<MeshFilter>().transform.position;
            player.Settings.ScaleFactor = 10 * player.Settings.ScaleFactor;
            player.ReloadStream();

            var p1 = player.gameObject.GetComponentInChildren<MeshFilter>().transform.position;
            Assert.AreEqual(10 * p0, p1);
        }

        [Test]
        public void ReloadStream_CreatesMissingNodes_IfNeeded([Values] bool createMissing)
        {
            Assert.IsTrue(player.LoadFromFile(path));
            Assert.AreEqual(2, player.transform.childCount);
            Object.DestroyImmediate(player.transform.GetChild(0).gameObject);
            player.ReloadStream(createMissing);
            Assert.AreEqual(createMissing ? 2 : 1, player.transform.childCount);
        }

        [Test]
        public void RemovedObsoleteGameObjects_Removes()
        {
            Assert.IsTrue(player.LoadFromFile(path));
            Assert.AreEqual(2, player.transform.childCount);
            var cube = new GameObject();
            cube.transform.parent = player.transform;
            Assert.AreEqual(3, player.transform.childCount);
            player.RemoveObsoleteGameObjects();
            Assert.AreEqual(2, player.transform.childCount);
        }
    }
}
