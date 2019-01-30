using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Exporter;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.TestTools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests {
    public class AlembicTestBase {
        // --- Helpers for creating temporary paths -----
        private string _testDirectory;
        protected string filePath {
            get {
                if (string.IsNullOrEmpty (_testDirectory)) {
                    // Create a directory in the asset path.
                    _testDirectory = GetRandomFileNamePath ("Assets", extName: "");
                    System.IO.Directory.CreateDirectory (_testDirectory);
                }
                return _testDirectory;
            }
        }

        private string _fileNamePrefix;
        protected string fileNamePrefix {
            get { return string.IsNullOrEmpty (_fileNamePrefix) ? "_safe_to_delete__" : _fileNamePrefix; }
            set { _fileNamePrefix = value; }
        }

        private string _fileNameExt;
        protected string fileNameExt { get { return string.IsNullOrEmpty (_fileNameExt) ? ".fbx" : _fileNameExt; } set { _fileNameExt = value; } }

        private string MakeFileName (string baseName = null, string prefixName = null, string extName = null) {
            if (baseName == null) {
                // GetRandomFileName makes a random 8.3 filename
                // We don't want the extension.
                baseName = Path.GetFileNameWithoutExtension (Path.GetRandomFileName ());
            }

            if (prefixName == null)
                prefixName = this.fileNamePrefix;

            if (extName == null)
                extName = this.fileNameExt;

            return prefixName + baseName + extName;
        }

        /// <summary>
        /// Create a random path for a file.
        ///
        /// By default the pathName is null, which defaults to a particular
        /// folder in the Assets folder that will be deleted on termination of
        /// this test.
        ///
        /// By default the prefix is a fixed string. You can set it differently if you care.
        ///
        /// By default the extension is ".fbx". You can set it differently if you care.
        ///
        /// By default we use platform path separators. If you want a random
        /// asset path e.g. for AssetDatabase.LoadMainAssetAtPath or for
        /// PrefabUtility.CreatePrefab, you need to use the '/' as separator
        /// (even on windows).
        ///
        /// See also convenience functions like:
        ///     GetRandomPrefabAssetPath()
        ///     GetRandomFbxFilePath()
        /// </summary>
        protected string GetRandomFileNamePath (
            string pathName = null,
            string prefixName = null,
            string extName = null,
            bool unityPathSeparator = false) {
            string temp;

            if (pathName == null) {
                pathName = this.filePath;
            }

            // repeat until you find a file that does not already exist
            do {
                temp = Path.Combine (pathName, MakeFileName (prefixName: prefixName, extName: extName));
            } while (File.Exists (temp));

            // Unity asset paths need a slash on all platforms.
            if (unityPathSeparator) {
                temp = temp.Replace ('\\', '/');
            }

            return temp;
        }

        /// <summary>
        /// Return a random .abc path that you can use in
        /// the File APIs.
        /// </summary>
        protected string GetRandomAbcFilePath () {
            return GetRandomFileNamePath (extName: ".abc", unityPathSeparator : false);
        }
        // -------------------------------------------------

        [TearDown]
        public virtual void Term () {
            if (string.IsNullOrEmpty (_testDirectory)) {
                return;
            }

            // Delete the directory on the next editor update.  Otherwise,
            // prefabs don't get deleted and the directory delete fails.
            EditorApplication.update += DeleteOnNextUpdate;
        }

        // Helper for the tear-down. This is run from the editor's update loop.
        void DeleteOnNextUpdate () {
            EditorApplication.update -= DeleteOnNextUpdate;
            try {
                Directory.Delete (filePath, recursive : true);
                AssetDatabase.Refresh ();
            } catch (IOException) {
                // ignore -- something else must have deleted this.
            }
        }

    }

    [TestFixture]
    public class AlembicExportTest : AlembicTestBase{
        private AlembicExporter GetAlembicExporter () {
            var alembicExporter = Object.FindObjectOfType<AlembicExporter> ();
            Assert.That (alembicExporter, Is.Not.Null);
            return alembicExporter;
        }

        private string GetAssetsAbsolutePath (string relPath) {
            return Application.dataPath + relPath.Replace ("Assets", "");
        }

        /// <summary>
        /// Test that the abc file was reimported and can be loaded into Unity through
        /// the asset database.
        /// </summary>
        /// <param name="abcPath"></param>
        private void TestAbcImported (string abcPath) {
            AssetDatabase.Refresh ();

            var absPath = GetAssetsAbsolutePath (abcPath);

            Assert.That (string.IsNullOrEmpty (absPath), Is.False);
            Assert.That (File.Exists (absPath));

            // now try loading the asset to see if it imported properly
            var obj = AssetDatabase.LoadMainAssetAtPath (abcPath);
            Assert.That (obj, Is.Not.Null);
            var go = obj as GameObject;
            Assert.That (go, Is.Not.Null);
        }

        // Loads a given scene
        IEnumerator SceneLoader (string sceneToLoad)
        {
            var scene = SceneManagement.EditorSceneManager.LoadSceneInPlayMode(sceneToLoad, new LoadSceneParameters(LoadSceneMode.Single));
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
        }
        // Sets a random, temporary filepath for a given Alembic exporter in the scene.
        string SetupExporter (AlembicExporter exporter = null) {
            if (exporter == null) {
                exporter = GetAlembicExporter ();
            }
            var exportPath = GetRandomAbcFilePath ();
            exporter.recorder.settings.OutputPath = GetAssetsAbsolutePath (exportPath);
            return exportPath;
        }
        // Begin recording and yield until the recording is done
        IEnumerator RecordAlembic (AlembicExporter exporter = null) {
            if (exporter == null) {
                exporter = GetAlembicExporter ();
            }
            exporter.BeginRecording ();

            while (!exporter.recorder.recording) {
                yield return null;
            }

            while (exporter.recorder.recording) {
                yield return null;
            }

        }
        // Loads ands runs generic scenes with vanilla recorder settings
        IEnumerator TestScene (string scene) {
            yield return SceneLoader (scene);
            var exporter = GetAlembicExporter ();
            var exportFile = SetupExporter (exporter);

            // set test specific exporter settings here
            exporter.maxCaptureFrame = 15;

            yield return RecordAlembic (exporter);
            TestAbcImported (exportFile);
        }

        // One shot export
        [UnityTest]
        public IEnumerator TestOneShotExport () {

            yield return SceneLoader ("Assets/Tests/Runtime/TestCloth.unity");
            var exporter = GetAlembicExporter ();
            var exportFile = SetupExporter (exporter);

            exporter.OneShot ();
            yield return null;
            TestAbcImported (exportFile);
        }
        // Export Cloth
        [UnityTest]
        public IEnumerator TestClothExport () {
            yield return TestScene ("Assets/Tests/Runtime/TestCloth.unity");
        }
        //Test Export of multiple, varied assets in a scene
        [UnityTest]
        public IEnumerator TestMultipleExport () {
            yield return TestScene ("Assets/Tests/Runtime/TestExport.unity");
        }
        //CustomCapturer
        [UnityTest]
        public IEnumerator TestCustomCapturer () {
            yield return TestScene ("Assets/Tests/Runtime/TestCustomCapturer.unity");
        }
        // GUI  Linear
        [UnityTest]
        public IEnumerator TestGUIUniform () {
            yield return SceneLoader ("Assets/Tests/Runtime/TestGUI.unity");
            var exporter = GetAlembicExporter ();
            var exportFile = SetupExporter (exporter);

            exporter.recorder.settings.conf.TimeSamplingType = (aeTimeSamplingType) 0;
            exporter.maxCaptureFrame = 15;

            yield return RecordAlembic (exporter);
            TestAbcImported (exportFile);
        }
        // GUI  Acyclic
        [UnityTest]
        public IEnumerator TestAcyclic () {
            yield return SceneLoader ("Assets/Tests/Runtime/TestGUI.unity");
            var exporter = GetAlembicExporter ();
            var exportFile = SetupExporter (exporter);

            exporter.recorder.settings.conf.TimeSamplingType = (aeTimeSamplingType) 2;
            exporter.maxCaptureFrame = 15;

            yield return RecordAlembic (exporter);
            TestAbcImported (exportFile);
        }
        // Swap xform test
        [UnityTest]
        public IEnumerator TestXform () {
            yield return SceneLoader ("Assets/Tests/Runtime/TestCloth.unity");
            var exporter = GetAlembicExporter ();
            var exportFile = SetupExporter (exporter);

            exporter.recorder.settings.conf.XformType = (aeXformType) aeXformType.Matrix;

            exporter.OneShot ();
            yield return null;
            TestAbcImported (exportFile);
        }
        // Other file format test
        [UnityTest]
        public IEnumerator TestHDF5 () {
            yield return SceneLoader ("Assets/Tests/Runtime/TestCloth.unity");
            var exporter = GetAlembicExporter ();
            var exportFile = SetupExporter (exporter);

            exporter.maxCaptureFrame = 15;
            exporter.recorder.settings.conf.ArchiveType = (aeArchiveType) aeArchiveType.HDF5;

            yield return RecordAlembic (exporter);
            TestAbcImported (exportFile);
        }
        // Swap handedness test
        [UnityTest]
        public IEnumerator TestSwapHandedness () {
            yield return SceneLoader ("Assets/Tests/Runtime/TestExport.unity");
            var exporter = GetAlembicExporter ();
            var exportFile = SetupExporter (exporter);

            exporter.recorder.settings.conf.SwapHandedness = false;
            exporter.maxCaptureFrame = 15;

            yield return RecordAlembic (exporter);
            TestAbcImported (exportFile);
        }
        //Small Scale Recording
        [UnityTest]
        public IEnumerator TestScaleFactor () {
            yield return SceneLoader ("Assets/Tests/Runtime/TestCustomCapturer.unity");
            var exporter = GetAlembicExporter ();
            var exportFile = SetupExporter (exporter);

            exporter.maxCaptureFrame = 10;
            exporter.recorder.settings.conf.ScaleFactor = 1;

            yield return RecordAlembic (exporter);
            TestAbcImported (exportFile);
        }
        // Low Frame Rate
        [UnityTest]
        public IEnumerator TestLowFrameRate () {
            yield return SceneLoader ("Assets/Tests/Runtime/TestGUI.unity");
            var exporter = GetAlembicExporter ();
            var exportFile = SetupExporter (exporter);

            exporter.recorder.settings.conf.TimeSamplingType = (aeTimeSamplingType) 0;
            exporter.recorder.settings.conf.FrameRate = 12;
            exporter.maxCaptureFrame = 15;

            yield return RecordAlembic (exporter);
            TestAbcImported (exportFile);
        }
        // High Frame Rate
        [UnityTest]
        public IEnumerator TestHighFrameRate () {
            yield return SceneLoader ("Assets/Tests/Runtime/TestGUI.unity");
            var exporter = GetAlembicExporter ();
            var exportFile = SetupExporter (exporter);

            exporter.recorder.settings.conf.TimeSamplingType = (aeTimeSamplingType) 0;
            exporter.recorder.settings.conf.FrameRate = 120;
            exporter.maxCaptureFrame = 15;

            yield return RecordAlembic (exporter);
            TestAbcImported (exportFile);
        }
        // Swap Faces
        [UnityTest]
        public IEnumerator TestSwapFaces () {
            yield return SceneLoader ("Assets/Tests/Runtime/TestCustomCapturer.unity");
            var exporter = GetAlembicExporter ();
            var exportFile = SetupExporter (exporter);

            exporter.recorder.settings.conf.SwapFaces = true;

            exporter.OneShot ();
            yield return null;
            TestAbcImported (exportFile);
        }
    }
}
