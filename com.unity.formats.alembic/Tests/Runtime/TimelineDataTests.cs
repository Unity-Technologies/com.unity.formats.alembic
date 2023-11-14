using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Formats.Alembic.Exporter;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.Formats.Alembic.Timeline;
using UnityEngine.Formats.Alembic.Util;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Assert = NUnit.Framework.Assert;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    class TimelineDataTests : BaseFixture
    {
        PlayableDirector director;
        GameObject cube;

        static IEnumerator TestCubeContents(GameObject go)
        {
            var root = PrefabUtility.InstantiatePrefab(go) as GameObject;
            var player = root.GetComponent<AlembicStreamPlayer>();

            var cubeGO = root.GetComponentInChildren<MeshRenderer>().gameObject;
            player.CurrentTime = 0;
            yield return new WaitForEndOfFrame();
            var t0 = cubeGO.transform.position;
            player.UpdateImmediately(player.Duration - 1e-5f);
            var t1 = cubeGO.transform.position;
            Assert.IsFalse(t0 != t1);
        }

        [SetUp]
        public new void SetUp()
        {
            var curve = AnimationCurve.Linear(0, 0, 10, 10);
            var clip = new AnimationClip { hideFlags = HideFlags.DontSave };
            clip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.hideFlags = HideFlags.DontSave;
            var aTrack = timeline.CreateTrack<AnimationTrack>(null, "CubeAnimation");
            aTrack.CreateClip(clip).displayName = "CubeClip";

            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.AddComponent<Animator>();
            director = cube.AddComponent<PlayableDirector>();
            director.playableAsset = timeline;
            director.SetGenericBinding(aTrack, cube);
        }

        [UnityTest]
        public IEnumerator TestTargetBranchExport()
        {
            director.Play();
            exporter.Recorder.Settings.Scope = ExportScope.TargetBranch;
            exporter.Recorder.Settings.TargetBranch = cube;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            var go = TestAbcImported(exporter.Recorder.Settings.OutputPath);
            yield return TestCubeContents(go);
        }

        [UnityTest]
        public IEnumerator TestOneShotExport()
        {
            director.Play();
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            exporter.OneShot();
            yield return null;
            TestAbcImported(exporter.Recorder.Settings.OutputPath, 0);
        }

        [UnityTest]
        public IEnumerator TestTimeSampling([Values(TimeSamplingType.Acyclic, TimeSamplingType.Uniform)] int sampleType)
        {
            director.Play();
            exporter.Recorder.Settings.ExportOptions.TimeSamplingType = (TimeSamplingType)sampleType;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            var go = TestAbcImported(exporter.Recorder.Settings.OutputPath);
            yield return TestCubeContents(go);
        }

        [UnityTest]
        public IEnumerator TestXForm([Values(TransformType.Matrix, TransformType.TRS)] int xFormType)
        {
            director.Play();
            exporter.Recorder.Settings.ExportOptions.TranformType = (TransformType)xFormType;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            var go = TestAbcImported(exporter.Recorder.Settings.OutputPath);
            yield return TestCubeContents(go);
        }

        [UnityTest]
        public IEnumerator TestSwapHandedness([Values(true, false)] bool swap)
        {
            director.Play();
            exporter.Recorder.Settings.ExportOptions.SwapHandedness = swap;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            var go = TestAbcImported(exporter.Recorder.Settings.OutputPath);
            yield return TestCubeContents(go);
        }

        [UnityTest]
        public IEnumerator TestSwapFaces([Values(true, false)] bool swap)
        {
            director.Play();
            exporter.Recorder.Settings.ExportOptions.SwapFaces = swap;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            var go = TestAbcImported(exporter.Recorder.Settings.OutputPath);
            yield return TestCubeContents(go);
        }

        [UnityTest]
        public IEnumerator TestScaleFactor()
        {
            director.Play();
            exporter.Recorder.Settings.ExportOptions.ScaleFactor = 1;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            var go = TestAbcImported(exporter.Recorder.Settings.OutputPath);
            yield return TestCubeContents(go);
        }

        [UnityTest]
        public IEnumerator TestFrameRate([Values(12, 120)] float frameRate)
        {
            director.Play();
            exporter.Recorder.Settings.ExportOptions.FrameRate = frameRate;
            exporter.MaxCaptureFrame = 30;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            var go = TestAbcImported(exporter.Recorder.Settings.OutputPath);
            yield return TestCubeContents(go);
        }

        // PublicAPI tests
        [UnityTest]
        public IEnumerator TestAlembicStreamPlayerTimeFieldsClampToValidRange()
        {
            director.Play();
            exporter.Recorder.Settings.Scope = ExportScope.TargetBranch;
            exporter.Recorder.Settings.TargetBranch = cube;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            var go = TestAbcImported(exporter.Recorder.Settings.OutputPath);
            var player = go.GetComponentInChildren<AlembicStreamPlayer>();

            player.StartTime = player.StreamDescriptor.MediaStartTime - 1;
            Assert.AreEqual(player.StartTime, player.StreamDescriptor.MediaStartTime);

            player.EndTime = (float)player.StreamDescriptor.MediaEndTime + 1;
            Assert.AreEqual(player.EndTime, (float)player.StreamDescriptor.MediaEndTime);

            player.CurrentTime = (player.StartTime + player.EndTime) / 2;
            Assert.AreEqual(player.CurrentTime, (player.StartTime + player.EndTime) / 2);

            player.CurrentTime = player.EndTime + 1;
            Assert.AreEqual(player.CurrentTime, player.EndTime);
        }

        [UnityTest]
        public IEnumerator TestMultipleUpdatesPerFrame()
        {
            director.Play();
            exporter.MaxCaptureFrame = 30;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            var go = TestAbcImported(exporter.Recorder.Settings.OutputPath);
            var root = PrefabUtility.InstantiatePrefab(go) as GameObject;
            var player = root.GetComponent<AlembicStreamPlayer>();

            var cubeGO = root.GetComponentInChildren<MeshRenderer>().gameObject;
            player.UpdateImmediately(0);
            var t0 = cubeGO.transform.position;
            player.UpdateImmediately(player.Duration);
            var t1 = cubeGO.transform.position;
            Assert.AreNotEqual(t0, t1);
        }

        [UnityTest]
        public IEnumerator TestTimelineTrack()
        {
            director.Play();
            exporter.MaxCaptureFrame = 30;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            var go = TestAbcImported(exporter.Recorder.Settings.OutputPath);
            var root = PrefabUtility.InstantiatePrefab(go) as GameObject;
            var player = root.GetComponent<AlembicStreamPlayer>();
            var timeline = director.playableAsset as TimelineAsset;
            var abcTrack = timeline.CreateTrack<AlembicTrack>(null, "");
            var clip = abcTrack.CreateClip<AlembicShotAsset>();
            var abcAsset = clip.asset as AlembicShotAsset;
            var refAbc = new ExposedReference<AlembicStreamPlayer> { exposedName = Guid.NewGuid().ToString() };
            abcAsset.StreamPlayer = refAbc;
            director.SetReferenceValue(refAbc.exposedName, player);
            director.RebuildGraph();
            var cubeGO = root.GetComponentInChildren<MeshRenderer>().gameObject;
            director.time = 0;
            director.Evaluate();
            yield return null;
            var t0 = cubeGO.transform.position;
            director.time = player.Duration;
            director.Evaluate();
            yield return null;
            var t1 = cubeGO.transform.position;
            Assert.AreNotEqual(t0, t1);
        }

        [Test]
        [Ignore("Unstable AssetDatabase Assertion")] //Assertion failed on expression: 'sadbTxn.Exists(artifactKey.GetGUID())'
        public void TestImportlessAlembicInvalidFile()
        {
            var go = new GameObject("abc");
            var player = go.AddComponent<AlembicStreamPlayer>();
            LogAssert.Expect(LogType.Error, new Regex("File does not exist"));
            var ret = player.LoadFromFile("DoesNotExist");

            Assert.IsFalse(ret);
        }

        [UnityTest]
        public IEnumerator TestImportlessAlembic()
        {
            director.Play();
            exporter.MaxCaptureFrame = 30;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);

            var sceneName = GUID.Generate().ToString();
            var scene = SceneManager.CreateScene(sceneName);
            SceneManager.SetActiveScene(scene);
            var go = new GameObject("abc");
            var player = go.AddComponent<AlembicStreamPlayer>();
            var ret = player.LoadFromFile(exporter.Recorder.Settings.OutputPath);
            Assert.IsTrue(ret);

            var cubeGO = go.GetComponentInChildren<MeshRenderer>().gameObject;
            player.UpdateImmediately(0);
            var t0 = cubeGO.transform.position;
            player.UpdateImmediately(player.Duration);
            var t1 = cubeGO.transform.position;
            Assert.AreNotEqual(t0, t1);

            var asyncOperation = SceneManager.UnloadSceneAsync(sceneName);
            while (!asyncOperation.isDone)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator TestInvalidName()
        {
            cube.name = "a/b";
            director.Play();
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            exporter.OneShot();
            yield return null;
            TestAbcImported(exporter.Recorder.Settings.OutputPath, 0);
        }

        [UnityTest]
        public IEnumerator TestObjectDeletionAndCreation()
        {
            var root = new GameObject();
            root.AddComponent<Spawner>();
            director.Play();
            exporter.MaxCaptureFrame = 40;
            exporter.Recorder.TargetBranch = root;
            exporter.Recorder.Settings.Scope = ExportScope.TargetBranch;

            yield return RecordAlembic();
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);

            var go = new GameObject("abc");
            var player = go.AddComponent<AlembicStreamPlayer>();
            var ret = player.LoadFromFile(exporter.Recorder.Settings.OutputPath);
            player.UpdateImmediately(0);
            Assert.IsTrue(ret);
            var cam = go.GetComponentInChildren<Camera>();   // Scope is respected
            Assert.IsNull(cam);
            var meshNames = go.GetComponentsInChildren<MeshRenderer>(true).Select(x => x.transform.parent.parent.name).ToArray();
            Assert.IsNotEmpty(meshNames.Where(x => x.StartsWith("10")));
            Assert.IsNotEmpty(meshNames.Where(x => x.StartsWith("30")));
        }

        [UnityTest]
        public IEnumerator TestAlembicExportMeshRendererNoMesh_DoesNotCrash()
        {
            var go = new GameObject("mrgo");
            go.AddComponent<MeshRenderer>();
            go.AddComponent<AlembicExporter>();

            director.Play();
            deleteFileList.Add(exporter.Recorder.Settings.OutputPath);
            exporter.OneShot();
            yield return null;
            TestAbcImported(exporter.Recorder.Settings.OutputPath, 0);
            Assert.IsTrue(true);
        }
    }
}
