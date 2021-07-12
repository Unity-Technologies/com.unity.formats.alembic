using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.TestTools;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    class ClothTests : BaseFixture
    {
        private Cloth cloth;
        static IEnumerator TestPlaneContents(GameObject go)
        {
            var root = PrefabUtility.InstantiatePrefab(go) as GameObject;
            var player = root.GetComponent<AlembicStreamPlayer>();

            var meshFilter = root.GetComponentsInChildren<MeshFilter>().First(x => x.name == "Plane");
            player.CurrentTime = 0;
            yield return new WaitForEndOfFrame();
            var vt0 = meshFilter.sharedMesh.vertices[0];
            var vCount0 = meshFilter.sharedMesh.vertices.Length;
            var idxCount0 = meshFilter.sharedMesh.GetIndexCount(0);
            var subMeshCount0 = meshFilter.sharedMesh.subMeshCount;
            player.CurrentTime = (float)player.Duration;
            yield return new WaitForEndOfFrame();
            var vt1 = meshFilter.sharedMesh.vertices[0];
            var vCount1 = meshFilter.sharedMesh.vertices.Length;
            var idxCount1 = meshFilter.sharedMesh.GetIndexCount(0);
            var subMeshCount1 = meshFilter.sharedMesh.subMeshCount;

            Assert.AreNotEqual(vt0, vt1); // It moves

            // Geom does not grow
            Assert.AreEqual(vCount0, vCount1);
            Assert.AreEqual(idxCount0, idxCount1);
            Assert.AreEqual(subMeshCount0, subMeshCount1);
        }

        [SetUp]
        public new void SetUp()
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localPosition = new Vector3(0, -1, 0);
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            cloth = plane.AddComponent<Cloth>();
            cloth.sphereColliders = new[] {new ClothSphereColliderPair(sphere.GetComponent<SphereCollider>())};
            cloth.clothSolverFrequency = 300;
        }

        [UnityTest]
        public IEnumerator TestDefaultExportParams()
        {
            yield return RecordAlembic();
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            var go = TestAbcImported(exporter.Recorder.Settings.OutputPath);
            yield return TestPlaneContents(go);
        }
    }
}
