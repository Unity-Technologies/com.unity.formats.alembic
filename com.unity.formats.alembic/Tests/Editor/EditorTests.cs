using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Formats.Alembic.Importer;
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
                AssetDatabase.DeleteAsset(file);
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

        [Test]
        public void CorruptedAlembicFileIsHandledGracefully()
        {
            var path = AssetDatabase.GUIDToAssetPath("0c10a673a92234124a1fc31297198530");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(asset);
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

        [Test]
        public void Alembic_WithInvisibleNode_SetAddCurveRenderers_True_DoesNotThrowException()
        {
            // make a copy of the prefab
            var originPath = AssetDatabase.GUIDToAssetPath("728c5b2b461c74d4991ce0a5e90433af"); // F.head model
            var path = "Assets/!InvisibleNodeTest.abc";
            AssetDatabase.CopyAsset(originPath, path);
            deleteFileList.Add(path);

            // set CreateCurveRenderer to true
            var importer = (AlembicImporter)AssetImporter.GetAtPath(path);
            importer.StreamSettings.CreateCurveRenderers = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // instantiate prefab
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var inst = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            var player = inst.GetComponent<AlembicStreamPlayer>();
            Assume.That(player != null);

            // Assert
            var curvesRenderer = inst.GetComponentInChildren<AlembicCurvesRenderer>(true);
            Assert.IsTrue(curvesRenderer != null);
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
            var refAbc = new ExposedReference<AlembicStreamPlayer> { exposedName = Guid.NewGuid().ToString() };
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
            var player = LoadAndInstantiate("c01136857b8dc481bb9babb33803ed4a");
            Assert.AreEqual(AlembicStreamPlayer.AlembicStreamSource.Internal, player.StreamSource);
            Assert.AreEqual(typeof(AlembicStreamDescriptor), player.StreamDescriptor.GetType());
            var asd = player.StreamDescriptor as AlembicStreamDescriptor;
            Assert.IsTrue(!string.IsNullOrEmpty(asd.PathToAbc));
            Assert.IsTrue(File.Exists(EditorHelper.BuildPathIfNecessary(asd.PathToAbc)));
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
            AssetDatabase.CopyAsset(srcDummyFile, copiedAbcFile);
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
            var player = LoadAndInstantiate("a6d019a425afe49d7a8fd029c82c0455");
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
        public void Computed_Velocities_AreScaled()
        {
            var player = LoadAndInstantiate("a6d019a425afe49d7a8fd029c82c0455");
            var mesh = player.GetComponentInChildren<MeshFilter>().sharedMesh;
            player.UpdateImmediately(1 / 60f);

            var vert0 = new List<Vector3>();
            var velocity0 = new List<Vector3>();
            mesh.GetVertices(vert0);
            mesh.GetUVs(5, velocity0);

            var expectedVelocity = new[] { new Vector3(0, 8, 0), new Vector3(-4, 0, 0), new Vector3(0, 0, 0) };
            {
                var error = expectedVelocity.Zip(velocity0, (x, y) => (x - y).magnitude);
                Assert.IsTrue(error.All(x => x < 1e-5));
            }
            player.VertexMotionScale = 0.5f;
            player.UpdateImmediately(0);
            player.UpdateImmediately(1 / 60f); // Bug that needs to change time to update velocity
            mesh.GetUVs(5, velocity0);
            {
                var expected = expectedVelocity.Select(x => 0.5f * x).ToArray();
                var error = expected.Zip(velocity0, (x, y) => (x - y).magnitude);
                Assert.IsTrue(error.All(x => x < 1e-5));
            }
        }

        [Test]
        public void Read_Velocities_AreNotScaled() // This is very strange. Need to decide if this is a bug
        {
            var player = LoadAndInstantiate("d0f12062215204c6991895f6a51dd627");
            var mesh = player.GetComponentInChildren<MeshFilter>().sharedMesh;
            player.UpdateImmediately(1 / 60f);

            var velocity0 = new List<Vector3>();
            mesh.GetUVs(5, velocity0);

            var v0 = new Vector3(0.00163127552f, 3.62623905E-05f, 0.0247734953f);
            var v100 = new Vector3(0.00122647523f, 5.96942518E-05f, 0.0172897689f);
            Assert.That(v0, Is.EqualTo(velocity0[0]).Within(1e-5));
            Assert.That(v100, Is.EqualTo(velocity0[100]).Within(1e-5));

            player.VertexMotionScale = 0.5f;
            player.UpdateImmediately(0);
            player.UpdateImmediately(1 / 60f); // Bug that needs to change time to update velocity
            mesh.GetUVs(5, velocity0);

            Assert.That(player.VertexMotionScale * v0, Is.Not.EqualTo(velocity0[0]).Within(1e-5));
            Assert.That(player.VertexMotionScale * v100, Is.Not.EqualTo(velocity0[100]).Within(1e-5));
        }

        [Test]
        public void FacesetNames_AreReadCorrectly()
        {
            var path = AssetDatabase.GUIDToAssetPath("f30dca527f4e947248046d6d12750c2d"); // GUID of cubes_coloured.abc
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            var facesetNames = new[] { "phong2SG", "phong1SG" };
            CollectionAssert.AreEqual(facesetNames, asset.GetComponentInChildren<AlembicCustomData>().FaceSetNames);

            var go = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            go.GetComponent<AlembicStreamPlayer>().UpdateImmediately(0);
            CollectionAssert.AreEqual(facesetNames, go.GetComponentInChildren<AlembicCustomData>().FaceSetNames);
        }

        [Test]
        public void FacesetNamesWithStrangeOrdering_IsCorrect()
        {
            var path = AssetDatabase.GUIDToAssetPath("ade5a7d01a0fd4cec986910e81ef1df3"); // GUID of cubes_coloured.abc
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            var facesetNames = new[] { "fs_top", "fs_bottom" };
            CollectionAssert.AreEqual(facesetNames, asset.GetComponentInChildren<AlembicCustomData>().FaceSetNames);

            var go = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            go.GetComponent<AlembicStreamPlayer>().UpdateImmediately(0);
            CollectionAssert.AreEqual(facesetNames, go.GetComponentInChildren<AlembicCustomData>().FaceSetNames);
        }

        [Test]
        public void SubDAndPolyMeshes_ReadTheSameWay()
        {
            Vector3[] vertexPoly, vertexSubd;
            {
                var playerPoly = LoadAndInstantiate("d3dedb3122bcd4d3787b29d185751353");
                var meshPoly = playerPoly.GetComponentInChildren<MeshFilter>().sharedMesh;
                playerPoly.UpdateImmediately(0);
                vertexPoly = meshPoly.vertices;
            }
            {
                var playerSubd = LoadAndInstantiate("42c8ff3c70d6b46d09da4146d0a44754");
                var meshSubd = playerSubd.GetComponentInChildren<MeshFilter>().sharedMesh;
                playerSubd.UpdateImmediately(0);
                vertexSubd = meshSubd.vertices;
            }

            CollectionAssert.AreEqual(vertexSubd, vertexPoly);
        }

        [Test]
        public void SubDMeshes_Animate()
        {
            var player = LoadAndInstantiate("d844f566b42f54aa4a4f8a1bc3a121af");
            var mesh = player.GetComponentInChildren<MeshFilter>().sharedMesh;
            player.UpdateImmediately(player.StreamDescriptor.MediaStartTime);
            var v0 = mesh.vertices;

            Assert.IsTrue(v0.Any(x => x != Vector3.zero)); // some data
            player.UpdateImmediately((player.StreamDescriptor.MediaStartTime + player.StreamDescriptor.MediaEndTime) / 2);
            CollectionAssert.AreNotEqual(v0, mesh.vertices);
        }

        [Test]
        public void SubDVariableTopologyMeshes_Animate()
        {
            var player = LoadAndInstantiate("942e560acda81ab47b439395d265a587");
            var mesh = player.GetComponentInChildren<MeshFilter>().sharedMesh;
            player.UpdateImmediately(player.StreamDescriptor.MediaStartTime);
            var v0 = mesh.vertices;

            Assert.IsTrue(v0.Any(x => x != Vector3.zero)); // some data
            player.UpdateImmediately(player.StreamDescriptor.MediaEndTime);
            CollectionAssert.AreNotEqual(v0, mesh.vertices);
            Assert.AreNotEqual(v0.Length, mesh.vertices.Length);
        }

        [UnityTest]
        public IEnumerator EmptyStreamPlayer_DoesNotAssert()
        {
            var go = new GameObject();
            go.AddComponent<AlembicStreamPlayer>();

            yield return null;

            Assert.IsTrue(true); // should not have Thrown exceptions
        }

        [Test]
        public void StreamedAlembicFilesKeepMaterialAssignmentsWhenChangingStream()
        {
            const string dst = "Assets/dst.abc";
            var src = AssetDatabase.GUIDToAssetPath("1a066d124049a413fb12b82470b82811");
            AssetDatabase.CopyAsset(src, dst);
            deleteFileList.Add(dst);
            var player = new GameObject().AddComponent<AlembicStreamPlayer>();
            player.LoadFromFile(src);
            var mat = AlembicMesh.GetDefaultMaterial();
            var renderer = player.GetComponentInChildren<MeshRenderer>();
            renderer.material = mat;

            Assert.AreEqual(mat, renderer.sharedMaterial);

            player.LoadFromFile(dst);
            Assert.AreEqual(mat, renderer.sharedMaterial);
        }

        [Test]
        public void NormalsAlwaysComputeImportOptionWorks()
        {
            const string badTriangleGUID = "6ee46b60872584073a7db242b67ec63d";
            const string copyTrianglePath = "Assets/src.abc";

            // create copy of bad triangle
            var src = AssetDatabase.GUIDToAssetPath(badTriangleGUID);
            AssetDatabase.CopyAsset(src, copyTrianglePath);
            deleteFileList.Add(copyTrianglePath);

            AssetDatabase.ImportAsset(copyTrianglePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(copyTrianglePath) as AlembicImporter;
            importer.StreamSettings.Normals = NormalsMode.AlwaysCalculate;
            EditorUtility.SetDirty(importer);
            AssetDatabase.ImportAsset(copyTrianglePath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh();

            var mesh = AssetDatabase.LoadAssetAtPath<GameObject>(copyTrianglePath).GetComponentInChildren<MeshFilter>().sharedMesh;
            var expectedNormals = new Vector3[] {
                new Vector3(0, 1, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, 1, 0),
            };

            Assert.IsNotNull(mesh);

            Assert.AreEqual(expectedNormals.Length, mesh.normals.Length);
            var dists = mesh.normals.Zip(expectedNormals, Vector3.Distance);
            Assert.IsTrue(dists.All(d => d < 1e-5));
        }

        [Test]
        public void CalculateNormalsIfMissingImportOption_DoesNotRecalculateNormals()
        {
            const string badTriangleGUID = "6ee46b60872584073a7db242b67ec63d";
            const string copyTrianglePath = "Assets/src.abc";

            // create copy of bad triangle
            var src = AssetDatabase.GUIDToAssetPath(badTriangleGUID);
            AssetDatabase.CopyAsset(src, copyTrianglePath);
            deleteFileList.Add(copyTrianglePath);

            AssetDatabase.ImportAsset(copyTrianglePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(copyTrianglePath) as AlembicImporter;
            importer.StreamSettings.Normals = NormalsMode.CalculateIfMissing;
            EditorUtility.SetDirty(importer);
            AssetDatabase.ImportAsset(copyTrianglePath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh();

            var mesh = AssetDatabase.LoadAssetAtPath<GameObject>(copyTrianglePath).GetComponentInChildren<MeshFilter>().sharedMesh;
            var expectedNormals = new Vector3[] {
                new Vector3(-0, -0.998879254f, -0.0473323129f),
                new Vector3(0.554307222f, 0.402543187f, -0.728493333f),
                new Vector3(0.293467999f, -0.0240235999f, 0.955667019f),
            };

            Assert.IsNotNull(mesh);
            CollectionAssert.AreEqual(expectedNormals, mesh.normals);
        }

        [Test]
        public void ImporterAllowsNotImportingMeshesAfterMappingMaterials()
        {
            const string dst = "Assets/src.abc";
            const string matPath = "Assets/mat.mat";
            var src = AssetDatabase.GUIDToAssetPath("1a066d124049a413fb12b82470b82811");
            AssetDatabase.CopyAsset(src, dst);
            var mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, matPath);
            deleteFileList.Add(dst);
            deleteFileList.Add(matPath);

            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(dst) as AlembicImporter;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "backflip (FFFFFB70)/Man_Sample (FFFFFB6C)/Man_Sample (FFFFFB6A)/Man_Sample:000:submesh[0]"), mat);
            importer.StreamSettings.ImportMeshes = false;
            EditorUtility.SetDirty(importer);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh();

            var mesh = AssetDatabase.LoadAssetAtPath<GameObject>(dst).GetComponentInChildren<MeshFilter>();

            Assert.IsNull(mesh);
        }

        [Test]
        public void ImporterAcceptsMismatchedGameObjectPathPathMapping()
        {
            const string dst = "Assets/src.abc";
            const string matPath = "Assets/mat.mat";
            var src = AssetDatabase.GUIDToAssetPath("1a066d124049a413fb12b82470b82811");
            AssetDatabase.CopyAsset(src, dst);
            var mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, matPath);
            deleteFileList.Add(dst);
            deleteFileList.Add(matPath);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(dst) as AlembicImporter;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "some/random/path:999:SomeName"), mat);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceSynchronousImport);

            var mesh = AssetDatabase.LoadAssetAtPath<GameObject>(dst).GetComponentInChildren<MeshFilter>();

            Assert.IsNotNull(mesh);
        }

        [Test]
        public void ImporterAcceptsMismatchedFacesetNames()
        {
            const string dst = "Assets/src.abc";
            const string matPath = "Assets/mat.mat";
            var src = AssetDatabase.GUIDToAssetPath("1a066d124049a413fb12b82470b82811");
            AssetDatabase.CopyAsset(src, dst);
            var mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, matPath);
            deleteFileList.Add(dst);
            deleteFileList.Add(matPath);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(dst) as AlembicImporter;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "backflip (FFFFFB70)/Man_Sample (FFFFFB6C)/Man_Sample (FFFFFB6A)/Man_Sample:099:submesh[0]"), mat);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceSynchronousImport);

            var mesh = AssetDatabase.LoadAssetAtPath<GameObject>(dst).GetComponentInChildren<MeshFilter>();

            Assert.IsNotNull(mesh);
        }

        [Test]
        public void InactiveMeshesAreNotSerializedInTheScene()
        {
            var player = LoadAndInstantiate("a648a7ac564ae48ef80f302ee12eb71d");
            var meshFlags = player.GetComponentsInChildren<MeshFilter>(includeInactive: true)
                .Select(x => x.sharedMesh.hideFlags);

            Assert.IsTrue(meshFlags.All(x => (x & HideFlags.DontSave) != 0));
        }

        static AlembicStreamPlayer LoadAndInstantiate(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            var player = go.GetComponent<AlembicStreamPlayer>();

            return player;
        }
    }
}
