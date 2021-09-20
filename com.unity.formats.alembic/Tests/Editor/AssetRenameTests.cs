using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Formats.Alembic.Importer;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    public class AssetRenameTests
    {
        readonly List<string> deleteFileList = new List<string>();
        const string copiedAbcFile = "Assets/abc.abc";
        GameObject go;

        [SetUp]
        public void SetUp()
        {
            const string dummyGUID = "a6d019a425afe49d7a8fd029c82c0455";
            var path = AssetDatabase.GUIDToAssetPath(dummyGUID);
            var srcDummyFile = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AlembicStreamPlayer>().First().StreamDescriptor.PathToAbc;
            File.Copy(srcDummyFile, copiedAbcFile, true);
            AssetDatabase.Refresh();
            var asset = AssetDatabase.LoadMainAssetAtPath(copiedAbcFile);
            go = PrefabUtility.InstantiatePrefab(asset) as GameObject;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var file in deleteFileList)
            {
                AssetDatabase.DeleteAsset(file);
            }

            deleteFileList.Clear();
        }

        [Test]
        public void TestRenameDoesNotRenameInstances()
        {
            Assert.AreEqual("abc", go.name);
            var ret = AssetDatabase.RenameAsset(copiedAbcFile, "new.abc");
            deleteFileList.Add("Assets/new.abc");
            AssetDatabase.Refresh();
            Assert.AreEqual("abc", go.name);
            Assert.IsEmpty(ret);
        }

        [Test]
        public void StreamUpdatesAfterRenamingAsset()
        {
            deleteFileList.Add("Assets/new.abc");
            var asp = go.GetComponent<AlembicStreamPlayer>();
            AssetDatabase.RenameAsset(copiedAbcFile, "new.abc");
            AssetDatabase.Refresh();

            asp.UpdateImmediately(0);
            var mesh = go.GetComponentInChildren<MeshFilter>().sharedMesh;
            var v0 = mesh.vertices;
            asp.UpdateImmediately(asp.Duration / 2);
            var v1 = mesh.vertices;

            CollectionAssert.AreNotEqual(v0, v1);
        }

        [Test]
        public void ChangingOptionsOnExistingImportedAssetsWorks()
        {
            deleteFileList.Add(copiedAbcFile);
            var path = AssetDatabase.GUIDToAssetPath("369d852cb291c4c6aa11efc087bf3d2b");
            File.Copy(path, copiedAbcFile, true);

            AssetDatabase.ImportAsset(copiedAbcFile, ImportAssetOptions.ForceSynchronousImport);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(copiedAbcFile);
            Assert.IsNull(asset.GetComponentInChildren<AlembicPointsCloud>());

            var importer = AssetImporter.GetAtPath(copiedAbcFile) as AlembicImporter;
            importer.StreamSettings.ImportPoints = true;
            EditorUtility.SetDirty(importer);
            AssetDatabase.ImportAsset(copiedAbcFile, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh();
            Assert.IsNotNull(asset.GetComponentInChildren<AlembicPointsCloud>());
        }
    }
}
