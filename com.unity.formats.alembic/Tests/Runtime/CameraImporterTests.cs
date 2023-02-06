using NUnit.Framework;
using System.Collections;
using UnityEngine;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    class CameraImporterTests : BaseFixture
    {
        const string assetGUID = "5cad963015b10664990b48c55b0761fe";
        GameObject instance;

        [SetUp]
        public new void SetUp()
        {
            var path = AssetDatabase.GUIDToAssetPath(assetGUID);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
        }
#if HDRP_AVAILABLE
        [Test]
        public void HDCamera_HasExtraComponent()
        {
            var camera = instance.GetComponentInChildren<Camera>();

            Assert.That(camera.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>(), Is.Not.Null,
                "HDAdditionalCameraData not found on camera gameobject.");
        }
#elif URP_AVAILABLE
        [Test]
        public void UniversalCamera_HasExtraComponent()
        {
            var camera = instance.GetComponentInChildren<Camera>();

            Assert.That(camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(), Is.Not.Null,
                "UniversalAdditionalCameraData not found on camera gameobject.");
        }
#endif
    }
}
