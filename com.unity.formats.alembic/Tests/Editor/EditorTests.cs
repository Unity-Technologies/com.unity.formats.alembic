using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.Formats.Alembic.Timeline;
using UnityEngine.Playables;
using UnityEngine.TestTools;
using UnityEngine.Timeline;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    class EditorTests
    {
        readonly List<string> deleteFileList = new List<string>();

        [TearDown]
        public void TearDown()
        {
            foreach (var file in deleteFileList)
            {
                AssetDatabase.DeleteAsset(file);
                var meta = file + ".meta";
                if (File.Exists(meta))
                {
                    File.Delete(meta);
                }
            }

            deleteFileList.Clear();
        }

        [Test]
        public void MarshalTests()
        {
            Assert.AreEqual(72, System.Runtime.InteropServices.Marshal.SizeOf(typeof(aePolyMeshData)));
        }

        [Test]
        public void BadGeometryDoesNotCreateNanNormals()
        {
            const string dummyGUID = "4f03ab724b2494f38ae7c6c3d06e0825";
            var path = AssetDatabase.GUIDToAssetPath(dummyGUID);
            var abc = AssetDatabase.LoadMainAssetAtPath(path);
            var instance = PrefabUtility.InstantiatePrefab(abc) as GameObject;
            instance.GetComponent<AlembicStreamPlayer>().UpdateImmediately(0);
            var mesh = instance.GetComponentInChildren<MeshFilter>().sharedMesh;
            var naNs = mesh.normals.Where(x => double.IsNaN(x.x) || double.IsNaN(x.y) || double.IsNaN(x.z));
            Assert.IsEmpty(naNs);
        }

        [Test]
        public void EmptyMeshFileIsHandledGracefully()
        {
            var path = AssetDatabase.GUIDToAssetPath("66b8b570b5eec42bd80704392a7001b5");
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var inst = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            Assert.IsNotNull(inst.GetComponent<AlembicStreamPlayer>());
            Assert.IsEmpty(inst.GetComponentsInChildren<MeshFilter>().Select(x => x.sharedMesh != null));
        }

        [UnityTest]
        public IEnumerator AddCurveRenderingSettingIsObeyed([Values] bool addCurve)
        {
            var path = AssetDatabase.GUIDToAssetPath("253cca792b1714bd985e9752217590a8"); // curves asset
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var inst = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            var player = inst.GetComponent<AlembicStreamPlayer>();
            Assert.IsNotNull(player);
            player.StreamDescriptor.Settings.CreateCurveRenderers = addCurve;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            Assert.IsNotNull(inst.GetComponentsInChildren<AlembicCurves>());
            inst.GetComponentInChildren<AlembicCurvesRenderer>();
            Assert.IsTrue(inst.GetComponentInChildren<AlembicCurvesRenderer>() != null ^ addCurve);
        }

        [UnityTest]
        public IEnumerator MultipleActivationsTracksCanActOnTheSameStreamPlayer()
        {
            var path = AssetDatabase.GUIDToAssetPath("253cca792b1714bd985e9752217590a8");  // curves asset
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var inst = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            var player = inst.GetComponent<AlembicStreamPlayer>();
            Assert.IsNotNull(player);
            var director = new GameObject().AddComponent<PlayableDirector>();
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            director.playableAsset = timeline;

            var abcTrack = timeline.CreateTrack<AlembicTrack>(null, "");
            var clip = abcTrack.CreateClip<AlembicShotAsset>();
            var abcAsset = clip.asset as AlembicShotAsset;
            var refAbc = new ExposedReference<AlembicStreamPlayer> {exposedName = Guid.NewGuid().ToString()};
            abcAsset.StreamPlayer = refAbc;
            director.SetReferenceValue(refAbc.exposedName, player);
            var a1 = timeline.CreateTrack<ActivationTrack>();

            var a2 = timeline.CreateTrack<ActivationTrack>();
            var c2 = a2.CreateDefaultClip();
            c2.start = 2;
            c2.duration = 1;

            director.SetGenericBinding(a1, player.gameObject);
            director.SetGenericBinding(a2, player.gameObject);
            director.RebuildGraph();
            director.time = 0;
            yield return null;
            director.time = 2.5;
            director.Evaluate();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SelectingAnAlembicPlayer_ShouldNotLeakResources()
        {
            var path = AssetDatabase.GUIDToAssetPath("1a066d124049a413fb12b82470b82811"); // GUID of DummyAlembic.abc
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var player = asset.GetComponent<AlembicStreamPlayer>();
            Assert.IsNull(player.abcStream);
            Selection.activeObject = asset;
            yield return null;
            Assert.IsNull(player.abcStream);
        }

        [Test]
        public void Importerless_ShouldGenerateTheCorrectNumberOfMaterialSlots()
        {
            var path = AssetDatabase.GUIDToAssetPath("f30dca527f4e947248046d6d12750c2d"); // GUID of twoTris_twoFaceSets...
            var player = new GameObject().AddComponent<AlembicStreamPlayer>();

            var ret = player.LoadFromFile(path);
            Assert.IsTrue(ret);

            var materials = player.GetComponentInChildren<MeshRenderer>().sharedMaterials;
            var subMeshes = player.GetComponentInChildren<MeshFilter>().sharedMesh.subMeshCount;
            Assert.AreEqual(2, subMeshes);
            Assert.AreEqual(subMeshes, materials.Length);
        }

        [Test]
        public void UpgradingOldPrefabs_DoNotGetSwitchedToImporterless()
        {
            var path = AssetDatabase.GUIDToAssetPath("c01136857b8dc481bb9babb33803ed4a"); // GUID of DummyAlembic.prefab
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            var player = go.GetComponent<AlembicStreamPlayer>();
            Assert.AreEqual(AlembicStreamPlayer.AlembicStreamSource.Internal, player.StreamSource);
            Assert.AreEqual(typeof(AlembicStreamDescriptor), player.StreamDescriptor.GetType());
            var asd = player.StreamDescriptor as AlembicStreamDescriptor;
            Assert.IsTrue(!string.IsNullOrEmpty(asd.PathToAbc));
            Assert.IsTrue(File.Exists(asd.PathToAbc));
        }

        [UnityTest]
        public IEnumerator ImportingFilesShouldNotGenerateUndoEvents()
        {
            Undo.IncrementCurrentGroup();
            var go = new GameObject();
            Undo.RegisterCreatedObjectUndo(go, "Create Object");
            Undo.IncrementCurrentGroup();

            yield return null;
            yield return null;

            const string dummyGUID = "1a066d124049a413fb12b82470b82811"; // GUID of DummyAlembic.abc
            const string copiedAbcFile = "Assets/abc.abc";
            var path = AssetDatabase.GUIDToAssetPath(dummyGUID);
            var srcDummyFile = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AlembicStreamPlayer>().First()
                .StreamDescriptor.PathToAbc;
            File.Copy(srcDummyFile, copiedAbcFile, true);
            deleteFileList.Add(copiedAbcFile);

            AssetDatabase.Refresh();
            yield return null;
            yield return null;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(copiedAbcFile);
            Assert.IsNotNull(asset);

            Undo.PerformUndo();

            Assert.IsTrue(go == null); // If the alembic importer pushed undo events, i will not undo the creation of the gameobject. If this is dead, it means there were no extra undo events between.
        }

        [Test]
        public void Velocities_AreConsistentWithMotion()
        {
            var path = AssetDatabase.GUIDToAssetPath("a6d019a425afe49d7a8fd029c82c0455"); // GUID of triangleRigged_asymetricalVertexTransform_24fps_2f.abc
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            var player = go.GetComponent<AlembicStreamPlayer>();
            var mesh = player.GetComponentInChildren<MeshFilter>().sharedMesh;
            player.UpdateImmediately(0);

            var vert0 = new List<Vector3>();
            var velocity0 = new List<Vector3>();
            mesh.GetVertices(vert0);
            mesh.GetUVs(5, velocity0);

            Assert.IsTrue(velocity0.All(x => x == Vector3.zero));

            player.UpdateImmediately(1 / 60f);
            var vert1 = new List<Vector3>();
            var velocity1 = new List<Vector3>();
            mesh.GetVertices(vert1);
            mesh.GetUVs(5, velocity1);

            var computedVelocity = vert1.Zip(vert0, (v1, v0) => v1 - v0).ToList();
            CollectionAssert.AreEqual(velocity1, computedVelocity);
        }

        [Test]
        public void Velocities_AreScaled()
        {
            var path = AssetDatabase.GUIDToAssetPath("a6d019a425afe49d7a8fd029c82c0455"); // GUID of triangleRigged_asymetricalVertexTransform_24fps_2f.abc
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            var player = go.GetComponent<AlembicStreamPlayer>();
            var mesh = player.GetComponentInChildren<MeshFilter>().sharedMesh;
            player.UpdateImmediately(1 / 60f);

            var vert0 = new List<Vector3>();
            var velocity0 = new List<Vector3>();
            mesh.GetVertices(vert0);
            mesh.GetUVs(5, velocity0);

            var expectedVelocity = new[] {new Vector3(0, 8, 0), new Vector3(-4, 0, 0), new Vector3(0, 0, 0)};
            CollectionAssert.AreEqual(expectedVelocity, velocity0);

            player.VertexMotionScale = 0.5f;
            player.UpdateImmediately(0);
            player.UpdateImmediately(1 / 60f); // Bug that needs to change time to update velocity
            mesh.GetUVs(5, velocity0);
            var expected = expectedVelocity.Select(x => 0.5f * x).ToArray();
            var error = expected.Zip(velocity0, (x, y) => (x - y).magnitude);
            Assert.IsTrue(error.All(x => x < 1e-5));
        }
    }
}
