using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    // UUM-160861: names must round-trip through the native plugin as UTF-8 (export and import).
    class NonAsciiNameTests : BaseFixture
    {
        [UnityTest]
        public IEnumerator ObjectNamesRoundTripAsUtf8([Values("Kėdė", "椅子")] string objectName)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;

            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            exporter.OneShot();
            yield return null;

            AssetDatabase.Refresh();
            Assert.That(File.Exists(exporter.Recorder.Settings.OutputPath));
            var abc = AssetDatabase.LoadMainAssetAtPath(exporter.Recorder.Settings.OutputPath) as GameObject;
            Assert.IsNotNull(abc, "Exported file failed to import");

            var found = false;
            foreach (var t in abc.GetComponentsInChildren<Transform>())
                found |= t.name == objectName;
            Assert.IsTrue(found, $"No object named '{objectName}' after export/import");
        }

        // Covers "Café" too: it fits the Windows ANSI code page, so it must not be written as raw UTF-8 bytes.
        [UnityTest]
        public IEnumerator ExportingToNonAsciiPath_WritesFileWithThatName([Values("Kėdė", "Café", "椅子")] string fileName)
        {
            GameObject.CreatePrimitive(PrimitiveType.Cube);

            var outputPath = $"Assets/{fileName}_{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}.abc";
            exporter.Recorder.Settings.OutputPath = outputPath;
            deleteFileList.Add(outputPath);
            exporter.OneShot();
            yield return null;

            Assert.That(File.Exists(outputPath), $"Expected '{outputPath}' to be written");
            AssetDatabase.Refresh();
            var abc = AssetDatabase.LoadMainAssetAtPath(outputPath) as GameObject;
            Assert.IsNotNull(abc, "Exported file failed to import");
            Assert.IsNotNull(abc.GetComponentInChildren<MeshFilter>(), "Imported asset has no meshes");
        }
    }
}
