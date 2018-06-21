using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Formats.Alembic.Exporter;
using System.IO;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    public class AlembicExportTest
    {
        private string m_pathToScene;

        [Test]
        public void TestOneShotExport()
        {
            // open scene
            var sceneSource = Application.dataPath + "/Tests/TestCloth.unity";
            SceneManagement.EditorSceneManager.OpenScene(sceneSource, SceneManagement.OpenSceneMode.Single);
            
            // export one shot
            var alembicExporter = Object.FindObjectOfType<AlembicExporter>();
            Assert.That(alembicExporter, Is.Not.Null);

            var exportPath = Application.dataPath + "/Temp/test.abc";

            alembicExporter.recorder.settings.OutputPath = exportPath;

            alembicExporter.OneShot();

            Assert.That(string.IsNullOrEmpty(exportPath), Is.False);
            Assert.That(File.Exists(exportPath));
        }

        [Test]
        public void TestClothExport()
        {
            // open scene
            var sceneSource = Application.dataPath + "/Tests/TestCloth.unity";
            SceneManagement.EditorSceneManager.OpenScene(sceneSource, SceneManagement.OpenSceneMode.Single);

            // start playing
            EditorApplication.isPlaying = true;

            // begin recording
            // end recording

            // end playing
            EditorApplication.isPlaying = false;

            // check fbx was created in expected location
            // check fbx is imported + is not empty
        }
    }
}
