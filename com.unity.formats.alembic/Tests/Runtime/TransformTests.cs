using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Util;
using UnityEngine.TestTools;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    class TransformTests : BaseFixture
    {
        [UnityTest]
        public IEnumerator TestCameraTransforms() // Camera flip rotations
        {
            var position = new Vector3(10, 20, 30);
            camera.transform.position = position;
            camera.transform.eulerAngles = new Vector3(10, 20, 30);
            var rotation = camera.transform.rotation;
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            exporter.OneShot();
            yield return null;

            AssetDatabase.Refresh();
            Assert.That(File.Exists(exporter.Recorder.Settings.OutputPath));
            var abc = AssetDatabase.LoadMainAssetAtPath(exporter.Recorder.Settings.OutputPath) as GameObject;
            var cam = abc.GetComponentInChildren<Camera>();
            Assert.That(NearlyEqual(cam.transform.position, position));
            Assert.That(NearlyEqual(cam.transform.rotation, rotation));
        }

        [UnityTest]
        public IEnumerator TestCubeTransforms()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var position = new Vector3(10, 20, 30);
            cube.transform.position = position;
            cube.transform.eulerAngles = new Vector3(10, 20, 30);
            var rotation = cube.transform.rotation;
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            exporter.OneShot();
            yield return null;

            AssetDatabase.Refresh();
            Assert.That(File.Exists(exporter.Recorder.Settings.OutputPath));
            var abc = AssetDatabase.LoadMainAssetAtPath(exporter.Recorder.Settings.OutputPath) as GameObject;
            var c = abc.GetComponentInChildren<MeshFilter>();
            Assert.That(NearlyEqual(c.transform.position, position));
            Assert.That(NearlyEqual(c.transform.rotation, rotation));
        }

        [UnityTest]
        public IEnumerator EmptyGameObjects_AreExported()
        {
            var root = new GameObject("Root");
            exporter.Recorder.Settings.Scope = ExportScope.TargetBranch;
            exporter.Recorder.Settings.TargetBranch = root;
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            exporter.OneShot();
            yield return null;

            AssetDatabase.Refresh();
            Assert.That(File.Exists(exporter.Recorder.Settings.OutputPath));
            var abc = AssetDatabase.LoadMainAssetAtPath(exporter.Recorder.Settings.OutputPath) as GameObject;
            Assert.AreEqual(1, abc.transform.childCount);
            Assert.AreEqual(1, abc.transform.GetChild(0).childCount);
        }
    }
}
