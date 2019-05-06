using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.TestTools;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    class ClothTests : BaseFixture
    {
        static IEnumerator TestPlaneContents(GameObject go)
        {
            var root = PrefabUtility.InstantiatePrefab(go) as GameObject;
            var player = root.GetComponent<AlembicStreamPlayer>();

            var meshFiler = root.GetComponentInChildren<MeshFilter>();
            player.CurrentTime = 0;
            yield return new WaitForEndOfFrame();
            var t0 = meshFiler.sharedMesh.vertices[0];
            player.CurrentTime = (float)player.duration;
            yield return new WaitForEndOfFrame();
            var t1 = meshFiler.sharedMesh.vertices[0];
            Assert.AreNotEqual(t0, t1);
        }

        [SetUp]
        public new void SetUp()
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localPosition = new Vector3(0, -1, 0);
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var cloth = plane.AddComponent<Cloth>();
            cloth.sphereColliders = new[] {new ClothSphereColliderPair(sphere.GetComponent<SphereCollider>())};
            cloth.clothSolverFrequency = 300;
        }

        [UnityTest]
        public IEnumerator TestDefaultExportParams()
        {
            yield return RecordAlembic();
            deleteFileList.Add(exporter.recorder.settings.OutputPath);
            var go = TestAbcImported(exporter.recorder.settings.OutputPath);
            yield return TestPlaneContents(go);
        }
    }
}
