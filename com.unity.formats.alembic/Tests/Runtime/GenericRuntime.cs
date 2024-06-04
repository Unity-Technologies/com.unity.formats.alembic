using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    public class GenericRuntime
    {
#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator VelocitiesAreReset_WhenPlaybackIsPaused([Values("a6d019a425afe49d7a8fd029c82c0455", "d0f12062215204c6991895f6a51dd627")] string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            var player = go.GetComponent<AlembicStreamPlayer>();
            var mesh = player.GetComponentInChildren<MeshFilter>().sharedMesh;
            player.CurrentTime = 1 / 60f;
            yield return null;
            var velocity = new List<Vector3>();
            mesh.GetUVs(5, velocity);

            Assert.IsTrue(velocity.Any(x => x != Vector3.zero));
            yield return new WaitForEndOfFrame();
            mesh.GetUVs(5, velocity);
            Assert.IsTrue(velocity.All(x => x == Vector3.zero));

            GameObject.DestroyImmediate(go);
        }

#endif
    }
}
