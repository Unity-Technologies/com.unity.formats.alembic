using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;

namespace UnityEditor.Formats.Alembic.Importer
{
    class FileManipulations
    {
        List<string> m_FilesToDelete = new List<string>();

        [TearDown]
        public void TearDown()
        {
            foreach (var path in m_FilesToDelete)
                AssetDatabase.DeleteAsset(path);
        }

        [Test]
        public void MoveFileFromFileSystem_PathToABC_IsUpdated()
        {
            string guid = "dd3554fc098614b9e99b49873fe18cd6";
            string fileToCopy = "Assets/myAsset.abc";
            string fullPath = Application.dataPath + "/myAsset.abc";

            string newLocation = "Assets/Tests/myAsset.abc";
            string newLocationfullPath = Application.dataPath + "/Tests/myAsset.abc";

            var path = AssetDatabase.GUIDToAssetPath(guid);

            bool success = AssetDatabase.CopyAsset(path, fileToCopy);
            Assume.That(success, Is.True, $"AssetDatabase could not copy the original file at {path}");

            var desc = AssetDatabase.LoadAssetAtPath<AlembicStreamDescriptor>(fileToCopy);
            Assume.That(desc.PathToAbc, Is.EqualTo(fileToCopy));

            Directory.CreateDirectory(Path.GetDirectoryName(newLocationfullPath));

            File.Move(fullPath, newLocationfullPath);
            File.Move(fullPath + ".meta", newLocationfullPath + ".meta");
            AssetDatabase.Refresh();

            m_FilesToDelete.Add(Path.GetDirectoryName(newLocation));

            desc = AssetDatabase.LoadAssetAtPath<AlembicStreamDescriptor>(newLocation);
            Assert.That(desc.PathToAbc, Is.EqualTo(newLocation),
                "PathToAbc should have been updated to the new path.");
        }
    }
}
