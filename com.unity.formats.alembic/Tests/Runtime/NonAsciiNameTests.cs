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
    }
}
