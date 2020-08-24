using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Exporter;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    class BaseFixture
    {
        internal AlembicExporter exporter;
        protected readonly List<string> deleteFileList = new List<string>();
        protected GameObject camera;
        const string sceneName = "Scene";

        protected GameObject TestAbcImported(string abcPath, double minDuration = 0.1)
        {
            AssetDatabase.Refresh();
            Assert.That(File.Exists(abcPath));

            // now try loading the asset to see if it imported properly
            var obj = AssetDatabase.LoadMainAssetAtPath(abcPath);
            Assert.That(obj, Is.Not.Null);
            var go = obj as GameObject;
            Assert.That(go, Is.Not.Null);

            var player = go.GetComponent<AlembicStreamPlayer>();
            Assert.GreaterOrEqual(player.Duration, minDuration); // More than empty

            return go;
        }

        protected IEnumerator RecordAlembic()
        {
            exporter.BeginRecording();
            while (!exporter.Recorder.Recording)
            {
                yield return null;
            }

            while (exporter.Recorder.Recording)
            {
                yield return null;
            }
        }

        protected static bool NearlyEqual(float f1, float f2, float eps = 1e-5f)
        {
            return Mathf.Abs(f1 - f2) < eps;
        }

        protected static bool NearlyEqual(Vector3 v1, Vector3 v2, float eps = Vector3.kEpsilon)
        {
            return NearlyEqual(v1.x, v2.x, eps) &&
                NearlyEqual(v1.y, v2.y, eps) &&
                NearlyEqual(v1.z, v2.z, eps);
        }

        protected static bool NearlyEqual(Quaternion v1, Quaternion v2, float eps = Quaternion.kEpsilon)
        {
            return NearlyEqual(v1.x, v2.x, eps) &&
                NearlyEqual(v1.y, v2.y, eps) &&
                NearlyEqual(v1.z, v2.z, eps) &&
                NearlyEqual(v1.w, v2.w, eps) ||
                NearlyEqual(v1.x, -v2.x, eps) &&
                NearlyEqual(v1.y, -v2.y, eps) &&
                NearlyEqual(v1.z, -v2.z, eps) &&
                NearlyEqual(v1.w, -v2.w, eps);
        }

        [SetUp]
        public void SetUp()
        {
            var scene = SceneManager.CreateScene(sceneName);
            SceneManager.SetActiveScene(scene);
            var go = new GameObject("Recorder");
            exporter = go.AddComponent<AlembicExporter>();
            exporter.MaxCaptureFrame = 20;
            exporter.Recorder.Settings.OutputPath =
                "Assets/" + Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + ".abc";
            exporter.CaptureOnStart = false;

            camera = new GameObject("Cam");
            camera.AddComponent<Camera>();
            camera.transform.localPosition = new Vector3(0, 1, -10);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var asyncOperation = SceneManager.UnloadSceneAsync(sceneName);
            while (!asyncOperation.isDone)
            {
                yield return null;
            }

            foreach (var file in deleteFileList)
            {
                File.Delete(file);
            }

            deleteFileList.Clear();
        }
    }
}
