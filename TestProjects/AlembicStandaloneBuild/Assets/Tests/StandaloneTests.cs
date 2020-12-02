using System.Collections;
using NUnit.Framework;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UnityEngine.Formats.Alembic.StandaloneTests
{
    public class StandaloneTests
    {
        [SetUp]
        public void SetUp()
        {
            SceneManager.LoadScene("Scene");
        }

        [UnityTest]
        public IEnumerator AlembicUpdatesCorrectly()
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
    }
}
