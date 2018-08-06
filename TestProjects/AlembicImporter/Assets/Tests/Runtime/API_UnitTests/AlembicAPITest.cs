using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Exporter;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.Formats.Alembic.Timeline;
using UnityEngine.Formats.Alembic.Util;
using UnityEngine.TestTools;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests {
    public class AlembicAPITest {
        // Loads a given scene
        IEnumerator SceneLoader (string sceneToLoad) {
            SceneManagement.EditorSceneManager.LoadScene (sceneToLoad, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return null;
        }
        // Sets up generic export location
        aeConfig SetUpExport (AlembicRecorder recorderTarget) {
            recorderTarget.settings.OutputPath = "Assets/API_UnitTests/TestAPI.abc";
            return recorderTarget.settings.Conf;
        }
        // Deletes Test File
        void TestAndScrub () {
            Assert.That (Application.dataPath + "/API_UnitTests/TestAPI.abc", Is.Not.Null);
            File.Delete (Application.dataPath + "/API_UnitTests/TestAPI.abc");
        }
        // Records a frame then stops the recorder
        IEnumerator Record (AlembicRecorder recorderTarget) {
            var result = recorderTarget.BeginRecording ();
            Assert.That (result, Is.True);
            recorderTarget.ProcessRecording ();
            yield return null;
            recorderTarget.EndRecording ();
        }
        // Test Capture Components
        [UnityTest]
        public IEnumerator PublicCaptureComponents () {
            SceneLoader ("TestAPI");
            using (AlembicRecorder recorder = new AlembicRecorder ()) {
                var conf = SetUpExport (recorder);
                recorder.settings.AssumeNonSkinnedMeshesAreConstant = false;
                recorder.settings.CaptureMeshRenderer = true;
                recorder.settings.CaptureCamera = true;
                recorder.settings.MeshSubmeshes = true;
                Record (recorder);
            }
            TestAndScrub ();
            yield return null;
        }
        // Test AeConfig Settings
        [UnityTest]
        public IEnumerator PublicAeConfig () {
            SceneLoader ("TestAPI");
            using (AlembicRecorder recorder = new AlembicRecorder ()) {
                var conf = SetUpExport (recorder);

                conf.FrameRate = 24;
                conf.ScaleFactor = 1;
                conf.SwapHandedness = true;
                Record (recorder);
            }
            TestAndScrub ();
            yield return null;
        }
        // Test Archive types
        [UnityTest]
        public IEnumerator PublicAeArchiveType () {
            SceneLoader ("TestAPI");
            using (AlembicRecorder recorder = new AlembicRecorder ()) {
                var conf = SetUpExport (recorder);
                conf.ArchiveType = UnityEngine.Formats.Alembic.Sdk.aeArchiveType.Ogawa;
                conf.ArchiveType = UnityEngine.Formats.Alembic.Sdk.aeArchiveType.HDF5;
                Record (recorder);
            }
            TestAndScrub ();
            yield return null;
        }
        // Test AeConfig set and load
        [UnityTest]
        public IEnumerator PublicAeConfigSaveLoad () {
            SceneLoader ("TestAPI");
            using (AlembicRecorder recorder = new AlembicRecorder ()) {
                var conf = SetUpExport (recorder);
                recorder.settings.Conf = conf;
                Record (recorder);
            }
            TestAndScrub ();
            yield return null;
        }
        // Test Dispose current recording
        [UnityTest]
        public IEnumerator PublicDispose () {
            SceneLoader ("TestAPI");
            using (AlembicRecorder recorder = new AlembicRecorder ()) {
                var conf = SetUpExport (recorder);
                var result = recorder.BeginRecording ();
                Assert.That (result, Is.True);
                yield return null;
                recorder.ProcessRecording ();
                recorder.Dispose ();
                recorder.EndRecording ();
            }
            TestAndScrub ();
            yield return null;
        }
    }
}