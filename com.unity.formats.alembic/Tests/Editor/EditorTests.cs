using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    class EditorTests
    {
        [Test]
        public void MarshalTests()
        {
            Assert.AreEqual(72, System.Runtime.InteropServices.Marshal.SizeOf(typeof(aePolyMeshData)));
        }

        [Test]
        public void BadGeometryDoesNotCreateNanNormals()
        {
            const string dummyGUID = "4f03ab724b2494f38ae7c6c3d06e0825";
            var path = AssetDatabase.GUIDToAssetPath(dummyGUID);
            var abc = AssetDatabase.LoadMainAssetAtPath(path);
            var instance = PrefabUtility.InstantiatePrefab(abc) as GameObject;
            instance.GetComponent<AlembicStreamPlayer>().UpdateImmediately(0);
            var mesh = instance.GetComponentInChildren<MeshFilter>().sharedMesh;
            var naNs = mesh.normals.Where(x => double.IsNaN(x.x) || double.IsNaN(x.y) || double.IsNaN(x.z));
            Assert.IsEmpty(naNs);
        }

        [Test]
        public void EmptyMeshFileIsHandledGracefully()
        {
            var path = AssetDatabase.GUIDToAssetPath("66b8b570b5eec42bd80704392a7001b5");
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var inst = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            Assert.IsNotNull(inst.GetComponent<AlembicStreamPlayer>());
            Assert.IsEmpty(inst.GetComponentsInChildren<MeshFilter>().Select(x => x.sharedMesh != null));
        }
    }
}
