using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UnityEngine.Formats.Alembic.StandaloneTests
{
    class StandaloneTests
    {
        [SetUp]
        public void SetUp()
        {
            SceneManager.LoadScene("Scene");
        }

        [UnityTest]
        public IEnumerator ImportedAlembicUpdatesCorrectly()
        {
            var abc = GameObject.Find("human").GetComponent<AlembicStreamPlayer>();
            Assert.IsNotNull(abc);
            var mesh = abc.GetComponentInChildren<MeshFilter>().sharedMesh;
            var position = new Vector3(0.0633399412f, 0.357630074f, 0.540497243f);
            Assert.That((position - mesh.vertices[0]).magnitude, Is.Not.EqualTo(0).Within(1e-5));
            abc.UpdateImmediately(1);
            Assert.That((position - mesh.vertices[0]).magnitude, Is.EqualTo(0).Within(1e-5));

            yield return null;
        }

        [UnityTest]
        public IEnumerator ImporterlessAlembicUpdatesCorrectly()
        {
            var abc = GameObject.Find("Importless").GetComponent<AlembicStreamPlayer>();
            Assert.IsNotNull(abc);
            var mesh = abc.GetComponentInChildren<MeshFilter>().sharedMesh;
            var position = new Vector3(0.0633399412f, 0.357630074f, 0.540497243f);
            Assert.That((position - mesh.vertices[0]).magnitude, Is.Not.EqualTo(0).Within(1e-5));
            abc.UpdateImmediately(1);
            Assert.That((position - mesh.vertices[0]).magnitude, Is.EqualTo(0).Within(1e-5));

            yield return null;
        }

#if UNITY_STANDALONE
        [UnityTest]
        public IEnumerator SpawnedAlembicUpdatesCorrectly()
        {
            var abc = new GameObject().AddComponent<AlembicStreamPlayer>();
            var humanPath = Path.Combine(Path.GetTempPath(), "human.abc"); // This is being copied by the build postprocessor from the test project
            var ret = abc.LoadFromFile(humanPath);
            Assert.IsTrue(ret);
            var mesh = abc.GetComponentInChildren<MeshFilter>().sharedMesh;
            var position = new Vector3(0.0633399412f, 0.357630074f, 0.540497243f);
            Assert.That((position - mesh.vertices[0]).magnitude, Is.Not.EqualTo(0).Within(1e-5));
            abc.UpdateImmediately(1);
            Assert.That((position - mesh.vertices[0]).magnitude, Is.EqualTo(0).Within(1e-5));

            yield return null;
        }

#endif
    }
}
