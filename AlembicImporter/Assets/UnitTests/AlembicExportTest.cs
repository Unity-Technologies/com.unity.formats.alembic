using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Exporter;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.Formats.Alembic.Timeline;
using UnityEngine.Formats.Alembic.Util;
using UnityEngine.Playables;
using UnityEngine.TestTools;
using UnityEngine.Timeline;

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

    public class AlembicExportTest : AlembicTestBase {
        private const string c_scene = "TestCloth";
        private const string cd_scene = "TestCreateAndDelete";
        private const string cc_scene = "TestCustomCapturer";
        private const string ce_scene = "TestExport";
        private const string cgui_scene = "TestGUI";
        private AlembicExporter selectedExporter;
        private string selectedExportPath;

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

        // Loads a given scene, generates a random export path and sends out the alembic exporter
        void GenericSceneLoader (string sceneToLoad) {
            SceneManagement.EditorSceneManager.LoadScene (sceneToLoad, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        // Selects the Exporter in the scene and creates a path for it
        void GenericExporterSelector () {
            selectedExporter = GetAlembicExporter ();
            var exportPath = GetRandomAbcFilePath ();
            selectedExportPath = exportPath;
            selectedExporter.recorder.settings.OutputPath = GetAssetsAbsolutePath (selectedExportPath);
        }

        // One shot export
        [UnityTest]
        public IEnumerator TestOneShotExport () {

            GenericSceneLoader (c_scene);
            yield return null;
            GenericExporterSelector ();
            yield return null;
            selectedExporter.OneShot ();
            yield return null;
            TestAbcImported (selectedExportPath);
        }

        // Export Cloth
        [UnityTest]
        public IEnumerator TestClothExport () {
            GenericSceneLoader (c_scene);
            yield return null;
            GenericExporterSelector ();
            yield return null;

            selectedExporter.maxCaptureFrame = 150;
            selectedExporter.BeginRecording ();

            while (!selectedExporter.recorder.recording) {
                yield return null;
            }

            while (selectedExporter.recorder.recording) {
                yield return null;
            }

            TestAbcImported (selectedExportPath);
        }

        //Create And Delete
        [UnityTest]
        public IEnumerator TestCreateAndDelete () {
            GenericSceneLoader (cd_scene);
            // yield while the timeline recorder track plays
            yield return new WaitForSeconds (9f);
            // yield for a few seconds more so the test asset can get registered into the assetdatabase
            yield return null;
            // Afaik, there is no programmatical way of manually accessing Alembic recorder clip settings, thus the path has to be set manually
            TestAbcImported ("Assets/UnitTests/RecorderUnitTests/Recorder.abc");
            Debug.Log (Application.dataPath + "/UnitTests/RecorderUnitTests/Recorder.abc");
            File.Delete (Application.dataPath + "/UnitTests/RecorderUnitTests/Recorder.abc");
        }

        //Test Export
        [UnityTest]
        public IEnumerator TestExport () {
            GenericSceneLoader (ce_scene);
            yield return null;
            GenericExporterSelector ();
            yield return null;

            selectedExporter.maxCaptureFrame = 150;
            selectedExporter.BeginRecording ();

            while (!selectedExporter.recorder.recording) {
                yield return null;
            }

            while (selectedExporter.recorder.recording) {
                yield return null;
            }

            TestAbcImported (selectedExportPath);
        }

        //CustomCapturer
        [UnityTest]
        public IEnumerator TestCustomCapturer () {
            GenericSceneLoader (cc_scene);
            yield return null;
            GenericExporterSelector ();
            yield return null;

            selectedExporter.maxCaptureFrame = 100;
            selectedExporter.BeginRecording ();

            while (!selectedExporter.recorder.recording) {
                yield return null;
            }

            while (selectedExporter.recorder.recording) {
                yield return null;
            }
            yield return null;
            TestAbcImported (selectedExportPath);
            yield return null;
        }

        // GUI  Linear
        [UnityTest]
        public IEnumerator TestGUIUniform () {
            GenericSceneLoader (cgui_scene);
            yield return null;
            GenericExporterSelector ();
            yield return null;

            selectedExporter.recorder.settings.conf.TimeSamplingType = (aeTimeSamplingType) 0;
            selectedExporter.maxCaptureFrame = 150;

            selectedExporter.BeginRecording ();

            while (!selectedExporter.recorder.recording) {
                yield return null;
            }

            while (selectedExporter.recorder.recording) {
                yield return null;
            }

            TestAbcImported (selectedExportPath);
        }

        // GUI  Acyclic
        [UnityTest]
        public IEnumerator TestAcyclic () {
            GenericSceneLoader (cgui_scene);
            yield return null;
            GenericExporterSelector ();
            yield return null;

            selectedExporter.recorder.settings.conf.TimeSamplingType = (aeTimeSamplingType) 2;
            selectedExporter.maxCaptureFrame = 150;

            selectedExporter.BeginRecording ();

            while (!selectedExporter.recorder.recording) {
                yield return null;
            }

            while (selectedExporter.recorder.recording) {
                yield return null;
            }

            TestAbcImported (selectedExportPath);
        }

        // Swap xform test
        [UnityTest]
        public IEnumerator TestXform () {
            GenericSceneLoader (c_scene);
            yield return null;
            GenericExporterSelector ();
            yield return null;

            selectedExporter.recorder.settings.conf.XformType = (aeXformType) aeXformType.Matrix;
            selectedExporter.OneShot ();

            yield return null;

            TestAbcImported (selectedExportPath);

            yield return null;
        }

        // Other file format test
        [UnityTest]
        public IEnumerator TestHDF5 () {
            GenericSceneLoader (c_scene);
            yield return null;
            GenericExporterSelector ();
            yield return null;

            selectedExporter.maxCaptureFrame = 150;
            selectedExporter.recorder.settings.conf.ArchiveType = (aeArchiveType) aeArchiveType.HDF5;

            selectedExporter.BeginRecording ();

            while (!selectedExporter.recorder.recording) {
                yield return null;
            }

            while (selectedExporter.recorder.recording) {
                yield return null;
            }

            TestAbcImported (selectedExportPath);
        }

        // Swap handedness test
        [UnityTest]
        public IEnumerator TestSwapHandedness () {
            GenericSceneLoader (ce_scene);
            yield return null;
            GenericExporterSelector ();
            yield return null;

            selectedExporter.recorder.settings.conf.SwapHandedness = false;
            selectedExporter.maxCaptureFrame = 150;

            selectedExporter.BeginRecording ();

            while (!selectedExporter.recorder.recording) {
                yield return null;
            }

            while (selectedExporter.recorder.recording) {
                yield return null;
            }

            TestAbcImported (selectedExportPath);
        }

        //Small Scale Recording
        [UnityTest]
        public IEnumerator TestScaleFactor () {
            GenericSceneLoader (cc_scene);
            yield return null;
            GenericExporterSelector ();
            yield return null;

            selectedExporter.maxCaptureFrame = 100;
            selectedExporter.recorder.settings.conf.ScaleFactor = 1;

            selectedExporter.BeginRecording ();

            while (!selectedExporter.recorder.recording) {
                yield return null;
            }

            while (selectedExporter.recorder.recording) {
                yield return null;
            }
            yield return null;
            TestAbcImported (selectedExportPath);
            yield return null;
        }

        // Low Frame Rate
        [UnityTest]
        public IEnumerator TestLowFrameRate () {
            GenericSceneLoader (cgui_scene);
            yield return null;
            GenericExporterSelector ();
            yield return null;

            selectedExporter.recorder.settings.conf.TimeSamplingType = (aeTimeSamplingType) 0;
            selectedExporter.recorder.settings.conf.FrameRate = 12;
            selectedExporter.maxCaptureFrame = 150;

            selectedExporter.BeginRecording ();

            while (!selectedExporter.recorder.recording) {
                yield return null;
            }

            while (selectedExporter.recorder.recording) {
                yield return null;
            }

            TestAbcImported (selectedExportPath);
        }

        // High Frame Rate
        [UnityTest]
        public IEnumerator TestHighFrameRate () {
            GenericSceneLoader (cgui_scene);
            yield return null;
            GenericExporterSelector ();
            yield return null;

            selectedExporter.recorder.settings.conf.TimeSamplingType = (aeTimeSamplingType) 0;
            selectedExporter.recorder.settings.conf.FrameRate = 120;
            selectedExporter.maxCaptureFrame = 150;

            selectedExporter.BeginRecording ();

            while (!selectedExporter.recorder.recording) {
                yield return null;
            }

            while (selectedExporter.recorder.recording) {
                yield return null;
            }

            TestAbcImported (selectedExportPath);
        }

        // Swap Faces
        [UnityTest]
        public IEnumerator TestSwapFaces () {
            GenericSceneLoader (cc_scene);
            yield return null;
            GenericExporterSelector ();
            yield return null;

            selectedExporter.recorder.settings.conf.SwapFaces = true;
            selectedExporter.OneShot ();

            yield return null;

            TestAbcImported (selectedExportPath);

            yield return null;
        }
    }
}