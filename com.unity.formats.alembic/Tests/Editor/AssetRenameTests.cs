using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
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
            const string dummyGUID = "1a066d124049a413fb12b82470b82811"; // GUID of DummyAlembic.abc
            var path = AssetDatabase.GUIDToAssetPath(dummyGUID);
            var srcDummyFile = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AlembicStreamPlayer>().First().StreamDescriptor.PathToAbc;
            File.Copy(srcDummyFile,copiedAbcFile, true);
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
            Assert.AreEqual("abc",go.name);
            var ret = AssetDatabase.RenameAsset(copiedAbcFile,"new.abc");
            AssetDatabase.Refresh();
            Assert.AreEqual("abc",go.name);
            Assert.IsEmpty(ret);
            deleteFileList.Add("Assets/new.abc");
        }
    }
}
