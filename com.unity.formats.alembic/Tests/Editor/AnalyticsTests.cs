using NUnit.Framework;
using UnityEditor.Formats.Alembic.Importer;
using UnityEngine;
using UnityEngine.Formats.Alembic.Exporter;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.Formats.Alembic.Util;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    class ImporterAnalyticsTests
    {
        [Test]
        public void CameraDataIsCorrect()
        {
            GetImporter("1416eaf458d8341cfacec4fa81fd0527", out var importer, out var player);
            using (var abcStream = new AlembicStream(new GameObject(), player.StreamDescriptor))
            {
                abcStream.AbcLoad(true, false);
                var evt = AlembicImporterAnalytics.CreateEvent(abcStream.abcTreeRoot, importer);
                var refValue = new AlembicImporterAnalytics.AlembicImporterAnalyticsEvent
                {
                    app = "Maya 2019 AbcExport v1.0",
                    guid = "1416eaf458d8341cfacec4fa81fd0527",
                    render_pipeline = "legacy",
                    camera_node_count = 1,
                    xform_node_count = 1
                };
                Assert.AreEqual(refValue, evt);
            }
        }

        [Test]
        public void MeshDataIsCorrect()
        {
            GetImporter("1a066d124049a413fb12b82470b82811", out var importer, out var player);
            using (var abcStream = new AlembicStream(new GameObject(), player.StreamDescriptor))
            {
                abcStream.AbcLoad(true, false);
                var evt = AlembicImporterAnalytics.CreateEvent(abcStream.abcTreeRoot, importer);
                var refValue = new AlembicImporterAnalytics.AlembicImporterAnalyticsEvent
                {
                    app = "",
                    guid = "1a066d124049a413fb12b82470b82811",
                    material_override_count = 1,
                    max_mesh_index_count = 6036,
                    max_mesh_vertex_count = 1008,
                    mesh_node_count = 1,
                    camera_node_count = 1,
                    render_pipeline = "legacy",
                    xform_node_count = 4
                };
                Assert.AreEqual(refValue, evt);
            }
        }

        [Test]
        public void SubDDataIsCorrect()
        {
            GetImporter("42c8ff3c70d6b46d09da4146d0a44754", out var importer, out var player);
            using (var abcStream = new AlembicStream(new GameObject(), player.StreamDescriptor))
            {
                abcStream.AbcLoad(true, false);
                var evt = AlembicImporterAnalytics.CreateEvent(abcStream.abcTreeRoot, importer);
                var refValue = new AlembicImporterAnalytics.AlembicImporterAnalyticsEvent
                {
                    app = "Houdini 18.5.408",
                    guid = "42c8ff3c70d6b46d09da4146d0a44754",
                    max_mesh_index_count = 576,
                    max_mesh_vertex_count = 98,
                    sub_d_node_count = 1,
                    render_pipeline = "legacy",
                    xform_node_count = 1
                };
                Assert.AreEqual(refValue, evt);
            }
        }

        [Test]
        public void CurveDataIsCorrect()
        {
            GetImporter("253cca792b1714bd985e9752217590a8", out var importer, out var player);
            using (var abcStream = new AlembicStream(new GameObject(), player.StreamDescriptor))
            {
                abcStream.AbcLoad(true, false);
                var evt = AlembicImporterAnalytics.CreateEvent(abcStream.abcTreeRoot, importer);
                var refValue = new AlembicImporterAnalytics.AlembicImporterAnalyticsEvent
                {
                    app = "XGen Spline Abc Writer",
                    guid = "253cca792b1714bd985e9752217590a8",
                    render_pipeline = "legacy",
                    curve_node_count = 1,
                    max_curve_count = 16,
                    xform_node_count = 1
                };
                Assert.AreEqual(refValue, evt);
            }
        }

        [Test]
        public void PointCloudDataIsCorrect()
        {
            GetImporter("369d852cb291c4c6aa11efc087bf3d2b", out var importer, out var player);
            using (var abcStream = new AlembicStream(new GameObject(), player.StreamDescriptor))
            {
                abcStream.AbcLoad(true, false);
                abcStream.AbcUpdateBegin(0.5);
                abcStream.AbcUpdateEnd();
                var evt = AlembicImporterAnalytics.CreateEvent(abcStream.abcTreeRoot, importer);
                var refValue = new AlembicImporterAnalytics.AlembicImporterAnalyticsEvent
                {
                    app = "Maya 2019 AbcExport v1.0",
                    guid = "369d852cb291c4c6aa11efc087bf3d2b",
                    render_pipeline = "legacy",
                    point_cloud_node_count = 1,
                    xform_node_count = 1,
                    max_points_count = 24
                };
                Assert.AreEqual(refValue, evt);
            }
        }

        static void GetImporter(string guid, out AlembicImporter importer, out AlembicStreamPlayer player)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            importer =  AssetImporter.GetAtPath(path) as AlembicImporter;
            player = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<AlembicStreamPlayer>();
        }
    }

    class BuildAnalyticsTests
    {
        [Test]
        public void EventIsCorrect()
        {
            var evt = AlembicBuildAnalytics.CreateEvent(BuildTarget.PS5);
            Assert.AreEqual("PS5", evt.target_platform);
        }
    }

    class ExporterAnalyticsTests
    {
        GameObject go;
        AlembicRecorderSettings settings;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject();
            settings = go.AddComponent<AlembicExporter>().Recorder.Settings;
            settings.CaptureMeshRenderer = true;
            settings.CaptureSkinnedMeshRenderer = true;
            settings.CaptureCamera = true;
            settings.AssumeNonSkinnedMeshesAreConstant = true;

            new GameObject().AddComponent<Camera>();
            new GameObject().AddComponent<SkinnedMeshRenderer>();
            GameObject.CreatePrimitive(PrimitiveType.Plane).GetComponent<MeshFilter>();
        }

        [Test]
        public void OptionsAreObeyedAndComponentsAreCapturedInExportAnalytics([Values] bool exportMesh, [Values] bool exportSkinnedMesh, [Values] bool exportCamera, [Values] bool assumeMeshConstant)
        {
            settings.CaptureMeshRenderer = exportMesh;
            settings.CaptureSkinnedMeshRenderer = exportSkinnedMesh;
            settings.CaptureCamera = exportCamera;
            settings.AssumeNonSkinnedMeshesAreConstant = assumeMeshConstant;

            var evt = AlembicExporterAnalytics.CreateEvent(settings);
            var refEvt = new AlembicExporterAnalytics.AlembicExporterAnalyticsEvent
            {
                capture_mesh = exportMesh,
                skinned_mesh = exportSkinnedMesh,
                camera = exportCamera,
                static_mesh_renderers = assumeMeshConstant
            };

            Assert.AreEqual(refEvt, evt);
        }

        [Test]
        public void ExportBranchIsReflectedCorrectlyInEvent()
        {
            var dummyRoot = new GameObject();
            settings.TargetBranch = dummyRoot;
            settings.Scope = ExportScope.TargetBranch;

            var evt = AlembicExporterAnalytics.CreateEvent(settings);
            var refEvt = new AlembicExporterAnalytics.AlembicExporterAnalyticsEvent
            {
                capture_mesh = false,
                skinned_mesh = false,
                camera = false,
                static_mesh_renderers = true
            };

            Assert.AreEqual(refEvt, evt);
        }
    }
}
