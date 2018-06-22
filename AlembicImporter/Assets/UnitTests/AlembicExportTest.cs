using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEngine.Formats.Alembic.Exporter;
using System.IO;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    public class AlembicTestBase
    {
        // --- Helpers for creating temporary paths -----
        private string _testDirectory;
        protected string filePath
        {
            get
            {
                if (string.IsNullOrEmpty(_testDirectory))
                {
                    // Create a directory in the asset path.
                    _testDirectory = GetRandomFileNamePath("Assets", extName: "");
                    System.IO.Directory.CreateDirectory(_testDirectory);
                }
                return _testDirectory;
            }
        }

        private string _fileNamePrefix;
        protected string fileNamePrefix
        {
            get { return string.IsNullOrEmpty(_fileNamePrefix) ? "_safe_to_delete__" : _fileNamePrefix; }
            set { _fileNamePrefix = value; }
        }

        private string _fileNameExt;
        protected string fileNameExt { get { return string.IsNullOrEmpty(_fileNameExt) ? ".fbx" : _fileNameExt; } set { _fileNameExt = value; } }

        private string MakeFileName(string baseName = null, string prefixName = null, string extName = null)
        {
            if (baseName == null)
            {
                // GetRandomFileName makes a random 8.3 filename
                // We don't want the extension.
                baseName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
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
        protected string GetRandomFileNamePath(
                string pathName = null,
                string prefixName = null,
                string extName = null,
                bool unityPathSeparator = false)
        {
            string temp;

            if (pathName == null)
            {
                pathName = this.filePath;
            }

            // repeat until you find a file that does not already exist
            do
            {
                temp = Path.Combine(pathName, MakeFileName(prefixName: prefixName, extName: extName));
            } while (File.Exists(temp));

            // Unity asset paths need a slash on all platforms.
            if (unityPathSeparator)
            {
                temp = temp.Replace('\\', '/');
            }

            return temp;
        }

        /// <summary>
        /// Return a random .abc path that you can use in
        /// the File APIs.
        /// </summary>
        protected string GetRandomAbcFilePath()
        {
            return GetRandomFileNamePath(extName: ".abc", unityPathSeparator: false);
        }
        // -------------------------------------------------


        [TearDown]
        public virtual void Term()
        {
            if (string.IsNullOrEmpty(_testDirectory))
            {
                return;
            }

            // Delete the directory on the next editor update.  Otherwise,
            // prefabs don't get deleted and the directory delete fails.
            EditorApplication.update += DeleteOnNextUpdate;
        }

        // Helper for the tear-down. This is run from the editor's update loop.
        void DeleteOnNextUpdate()
        {
            EditorApplication.update -= DeleteOnNextUpdate;
            try
            {
                Directory.Delete(filePath, recursive: true);
                AssetDatabase.Refresh();
            }
            catch (IOException)
            {
                // ignore -- something else must have deleted this.
            }
        }
    }

    public class AlembicExportTest : AlembicTestBase
    {
        private const string c_scene = "TestCloth";

        private AlembicExporter GetAlembicExporter()
        {
            var alembicExporter = Object.FindObjectOfType<AlembicExporter>();
            Assert.That(alembicExporter, Is.Not.Null);
            return alembicExporter;
        }

        private string GetAssetsAbsolutePath(string relPath)
        {
            return Application.dataPath + relPath.Replace("Assets", "");
        }

        /// <summary>
        /// Test that the abc file was reimported and can be loaded into Unity through
        /// the asset database.
        /// </summary>
        /// <param name="abcPath"></param>
        private void TestAbcImported(string abcPath)
        {
            AssetDatabase.Refresh();

            var absPath = GetAssetsAbsolutePath(abcPath);

            Assert.That(string.IsNullOrEmpty(absPath), Is.False);
            Assert.That(File.Exists(absPath));

            // now try loading the asset to see if it imported properly
            var obj = AssetDatabase.LoadMainAssetAtPath(abcPath);
            Assert.That(obj, Is.Not.Null);
            var go = obj as GameObject;
            Assert.That(go, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator TestOneShotExport()
        {
            // open scene
            SceneManagement.EditorSceneManager.LoadScene(c_scene, UnityEngine.SceneManagement.LoadSceneMode.Single);

            // yield once while scene loads
            yield return null;

            // export one shot
            var alembicExporter = GetAlembicExporter();
            var exportPath = GetRandomAbcFilePath();

            alembicExporter.recorder.settings.OutputPath = GetAssetsAbsolutePath(exportPath);

            alembicExporter.OneShot();

            yield return null;

            TestAbcImported(exportPath);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClothExport()
        {
            // open scene
            SceneManagement.EditorSceneManager.LoadScene(c_scene, UnityEngine.SceneManagement.LoadSceneMode.Single);

            // yield once while scene loads
            yield return null;

            var alembicExporter = GetAlembicExporter();
            var exportPath = GetRandomAbcFilePath();

            alembicExporter.recorder.settings.OutputPath = GetAssetsAbsolutePath(exportPath);
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

            TestAbcImported(exportPath);
        }
    }
}
