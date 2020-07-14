using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    class SkinnedMeshTests : BaseFixture
    {
        const string cubeGUID = "a1dfa941512f24dadbe30205db7d8eea"; // GUID of cube_skinRig.fbx
        GameObject rootScaled, boneScaled;

        [SetUp]
        public new void SetUp()
        {
            var path = AssetDatabase.GUIDToAssetPath(cubeGUID);
            var skinnedCube = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            rootScaled = PrefabUtility.InstantiatePrefab(skinnedCube) as GameObject;
            rootScaled.name = "RootScaled";
            rootScaled.transform.localScale = new Vector3(0.1f, 0.2f, 0.3f);
            boneScaled = PrefabUtility.InstantiatePrefab(skinnedCube) as GameObject;
            boneScaled.name = "BoneScaled";
            var joint = boneScaled.transform.GetChild(0);
            joint.transform.localScale = new Vector3(0.1f, 0.2f, 0.3f);
        }

        [UnityTest]
        public IEnumerator TestBoneScaling()
        {
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            exporter.OneShot();
            yield return null;

            AssetDatabase.Refresh();
            Assert.That(File.Exists(exporter.Recorder.Settings.OutputPath));
            var abc = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadMainAssetAtPath(exporter.Recorder.Settings.OutputPath) as GameObject) as GameObject;
            yield return null;

            var boneScaledImported = abc.transform.GetChild(0);
            var rootScaledImported = abc.transform.GetChild(2);

            Assert.That(boneScaledImported.name.StartsWith("BoneScaled"));
            Assert.That(rootScaledImported.name.StartsWith("RootScaled"));
            Assert.IsTrue(MeshWorldPosCompare(boneScaled.GetComponentInChildren<SkinnedMeshRenderer>(),
                boneScaledImported.GetComponentInChildren<MeshFilter>()));
            Assert.IsTrue(MeshWorldPosCompare(rootScaled.GetComponentInChildren<SkinnedMeshRenderer>(),
                rootScaledImported.GetComponentInChildren<MeshFilter>()));
        }

        static bool MeshWorldPosCompare(SkinnedMeshRenderer skinnedMesh, MeshFilter mesh2)
        {
            var m1v = new List<Vector3>();
            var m2v = new List<Vector3>();
            var mesh1 = new Mesh();
            skinnedMesh.BakeMesh(mesh1);
            var w2l = skinnedMesh.transform.worldToLocalMatrix;
            var scale = Matrix4x4.Scale(new Vector3(
                w2l.GetColumn(0).magnitude,
                w2l.GetColumn(1).magnitude,
                w2l.GetColumn(2).magnitude
            ));
            mesh1.GetVertices(m1v);
            mesh2.sharedMesh.GetVertices(m2v);
            Object.DestroyImmediate(mesh1);
            for (var i = 0; i < m1v.Count; ++i)
            {
                var p1 = (scale * skinnedMesh.transform.worldToLocalMatrix).MultiplyPoint(m1v[i]);
                var p2 = mesh2.transform.worldToLocalMatrix.MultiplyPoint(m2v[i]);
                var dist = p1 - p2;
                if (dist.magnitude > 1e-3)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
