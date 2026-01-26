using NUnit.Framework;
using UnityEditor;

namespace  UnityEngine.Formats.Alembic.EditModeTests
{
    public class EditorTests
    {
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
