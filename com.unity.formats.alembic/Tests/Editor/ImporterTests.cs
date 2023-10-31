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
        
        [Test]
        public void FaceColorAttributes_AreProcessedCorrectly()
        {
            string guid = "45d4eb6bc4cd3ac479e0f4a21b192ed9";

            var path = AssetDatabase.GUIDToAssetPath(guid);

            var meshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            GameObject.Instantiate(meshPrefab); // needed to add the mesh filter component

            var meshFilter = GameObject.Find("cube_face").GetComponentInChildren<MeshFilter>();

            Color[] expectedColors =
            {
                new (0.136873f, 0.570807f, 0.034585f, 1.0f),
                new (0.807873f, 0.292345f, 0.0282118f, 1.0f),
                new (0.638699f, 0.320457f, 0.676918f, 1.0f),
                new (0.849678f, 0.0295019f, 0.185701f, 1.0f),
                new (0.410384f, 0.133891f, 0.293169f, 1.0f),
                new (0.0560673f, 0.955198f, 0.474951f, 1.0f)
            };

            for (int i = 0; i < expectedColors.Length; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    var meshColor = meshFilter.sharedMesh.colors[i * 4 + j];
                    Assert.IsTrue(expectedColors[i] == meshColor,"Expected: {expectedColors[i]}, But was: {meshColor}");
                }
            }
        }
    }

}
