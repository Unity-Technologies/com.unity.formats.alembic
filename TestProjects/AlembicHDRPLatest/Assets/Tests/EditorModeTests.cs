using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.Formats.Alembic.Tests
{
    class EditorModeTests
    {
        [UnityTest]
        public IEnumerator EnteringPlaymode_DoesNotYieldErrors()
        {
            yield return new EnterPlayMode();
            Assert.IsTrue(true);
            yield return new ExitPlayMode();
        }

        [Test]
        public void CorruptAlembicFileDoesNotCrashInEditor()
        {
            var fileSource = "Assets/Data~/CrashAsset.abc";
            //generate unique name, as previous import failures might prevent the import process to run.
            var fileDestination = $"Assets/CrashAsset-{GUID.Generate().ToString()}.abc";

            try
            {
                AssetDatabase.StartAssetEditing();
                FileUtil.CopyFileOrDirectory(fileSource, fileDestination);
                AssetDatabase.StopAssetEditing();
                AssetDatabase.ImportAsset(fileDestination);
            }
            finally
            {
                AssetDatabase.DeleteAsset(fileDestination);
            }

            Assert.Pass("Editor has not crashed.");
        }
    }
}
