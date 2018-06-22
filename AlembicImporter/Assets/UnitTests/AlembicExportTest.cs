using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEngine.Formats.Alembic.Exporter;
using System.IO;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    public class AlembicExportTest
    {
        private string m_pathToScene;

        private AlembicExporter GetAlembicExporter()
        {
            var alembicExporter = Object.FindObjectOfType<AlembicExporter>();
            Assert.That(alembicExporter, Is.Not.Null);
            return alembicExporter;
        }

        [UnityTest]
        public IEnumerator TestOneShotExport()
        {
            // open scene
            var sceneSource = "TestCloth";
            SceneManagement.EditorSceneManager.LoadScene(sceneSource, UnityEngine.SceneManagement.LoadSceneMode.Single);

            // yield once while scene loads
            yield return null;
            Debug.Log("begin testing");

            // export one shot
            var alembicExporter = GetAlembicExporter();
            alembicExporter.captureOnStart = false;

            var exportPath = Application.dataPath + "/Temp/test.abc";

            alembicExporter.recorder.settings.OutputPath = exportPath;

            alembicExporter.OneShot();

            yield return null;
            AssetDatabase.Refresh();

            Assert.That(string.IsNullOrEmpty(exportPath), Is.False);
            Assert.That(File.Exists(exportPath));

            // now try loading the asset to see if it imported properly
            var obj = AssetDatabase.LoadMainAssetAtPath("Assets/Temp/test.abc");
            Assert.That(obj, Is.Not.Null);
            var go = obj as GameObject;
            Assert.That(go, Is.Not.Null);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClothExport()
        {
            // open scene
            var sceneSource = "TestCloth";// Application.dataPath + "/Tests/TestCloth.unity";
            SceneManagement.EditorSceneManager.LoadScene(sceneSource, UnityEngine.SceneManagement.LoadSceneMode.Single);

            // yield once while scene loads
            yield return null;

            var alembicExporter = GetAlembicExporter();

            var exportPath = Application.dataPath + "/Temp/test2.abc";

            alembicExporter.recorder.settings.OutputPath = exportPath;
            alembicExporter.maxCaptureFrame = 300;

            alembicExporter.BeginRecording();

            while (!alembicExporter.recorder.recording)
            {
                yield return null;
            }

            while (alembicExporter.recorder.recording)
            {
                yield return null;
            }
            Debug.Log("exported file");

            // check fbx was created in expected location
            // check fbx is imported + is not empty
            yield return null;
        }
    }
}
