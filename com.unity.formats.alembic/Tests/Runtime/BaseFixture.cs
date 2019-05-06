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
            Assert.GreaterOrEqual(player.duration, minDuration); // More than empty

            return go;
        }

        protected IEnumerator RecordAlembic()
        {
            exporter.BeginRecording();
            while (!exporter.recorder.recording)
            {
                yield return null;
            }

            while (exporter.recorder.recording)
            {
                yield return null;
            }
        }

        [SetUp]
        public void SetUp()
        {
            var scene = SceneManager.CreateScene(sceneName);
            SceneManager.SetActiveScene(scene);
            var go = new GameObject("Recorder");
            exporter = go.AddComponent<AlembicExporter>();
            exporter.maxCaptureFrame = 10;
            exporter.recorder.settings.OutputPath =
                "Assets/" + Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + ".abc";
            exporter.captureOnStart = false;

            var cam = new GameObject("Cam");
            cam.AddComponent<Camera>();
            cam.transform.localPosition = new Vector3(0, 1, -10);
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
