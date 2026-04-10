#if UNITY_6000_4_OR_NEWER
using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Util;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    /// <summary>
    /// Tests for deterministic export ordering (scene + hierarchy path), Unity 6.4+.
    /// </summary>
    class StableHierarchySortTests
    {
        [Test]
        public void SortComponentsByStableSceneHierarchy_NullOrTrivial_DoesNotThrow()
        {
            AlembicRecorder.SortComponentsByStableSceneHierarchy(null);
            AlembicRecorder.SortComponentsByStableSceneHierarchy(new Component[0]);
        }

        [UnityTest]
        public IEnumerator AppendStableHierarchySortKey_SiblingOrderMatchesSiblingIndex()
        {
            var sceneName = "StableHierarchySortTests_" + System.Guid.NewGuid();
            var scene = SceneManager.CreateScene(sceneName);
            SceneManager.SetActiveScene(scene);

            var first = new GameObject("first");
            var second = new GameObject("second");
            first.transform.SetSiblingIndex(0);
            second.transform.SetSiblingIndex(1);

            var a = first.AddComponent<MeshRenderer>();
            var b = second.AddComponent<MeshRenderer>();

            var sbA = new StringBuilder();
            var sbB = new StringBuilder();
            AlembicRecorder.AppendStableHierarchySortKey(sbA, a);
            AlembicRecorder.AppendStableHierarchySortKey(sbB, b);

            Assert.Less(string.CompareOrdinal(sbA.ToString(), sbB.ToString()), 0);

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator AppendStableHierarchySortKey_ParentBeforeChild()
        {
            var sceneName = "StableHierarchySortTests_" + System.Guid.NewGuid();
            var scene = SceneManager.CreateScene(sceneName);
            SceneManager.SetActiveScene(scene);

            var parent = new GameObject("parent");
            var child = new GameObject("child");
            child.transform.SetParent(parent.transform);

            var cParent = parent.AddComponent<MeshRenderer>();
            var cChild = child.AddComponent<MeshRenderer>();

            var sbP = new StringBuilder();
            var sbC = new StringBuilder();
            AlembicRecorder.AppendStableHierarchySortKey(sbP, cParent);
            AlembicRecorder.AppendStableHierarchySortKey(sbC, cChild);

            Assert.Less(string.CompareOrdinal(sbP.ToString(), sbC.ToString()), 0);

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator SortComponentsByStableSceneHierarchy_OrdersByHierarchy()
        {
            var sceneName = "StableHierarchySortTests_" + System.Guid.NewGuid();
            var scene = SceneManager.CreateScene(sceneName);
            SceneManager.SetActiveScene(scene);

            var root = new GameObject("root");
            var childA = new GameObject("a");
            var childB = new GameObject("b");
            childA.transform.SetParent(root.transform);
            childB.transform.SetParent(root.transform);
            childA.transform.SetSiblingIndex(0);
            childB.transform.SetSiblingIndex(1);

            var r = root.AddComponent<MeshRenderer>();
            var a = childA.AddComponent<MeshRenderer>();
            var b = childB.AddComponent<MeshRenderer>();

            var components = new[] { b, r, a };
            AlembicRecorder.SortComponentsByStableSceneHierarchy(components);

            Assert.AreSame(r, components[0]);
            Assert.AreSame(a, components[1]);
            Assert.AreSame(b, components[2]);

            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
#endif
