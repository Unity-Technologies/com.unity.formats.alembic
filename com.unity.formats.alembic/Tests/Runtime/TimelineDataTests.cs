using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.Formats.Alembic.Util;
using UnityEngine.TestTools;

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
            player.CurrentTime = (float)player.duration;
            yield return new WaitForEndOfFrame();
            var t1  = cubeGO.transform.position;
            Assert.AreNotEqual(t0,t1);
        }
        
        [SetUp]
        public new void SetUp()
        {
            var curve = AnimationCurve.Linear(0, 0, 10, 10);
            var clip = new AnimationClip {hideFlags = HideFlags.DontSave};
            clip.SetCurve("",typeof(Transform),"localPosition.x",curve);
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.hideFlags = HideFlags.DontSave;
            var aTrack = timeline.CreateTrack<AnimationTrack>(null, "CubeAnimation");
            aTrack.CreateClip(clip).displayName = "CubeClip";
            
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.AddComponent<Animator>();
            director = cube.AddComponent<PlayableDirector>();
            director.playableAsset = timeline;
            director.SetGenericBinding(aTrack,cube);
        }
        
        [UnityTest]
        public IEnumerator  TestTargetBranchExport()
        {
            director.Play();
            exporter.recorder.settings.Scope = ExportScope.TargetBranch;
            exporter.recorder.settings.TargetBranch = cube;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.recorder.settings.OutputPath);
            var go = TestAbcImported (exporter.recorder.settings.OutputPath);
            yield return TestCubeContents(go);
        }

        [UnityTest]
        public IEnumerator  TestOneShotExport()
        {
            director.Play();
            deleteFileList.Add(exporter.recorder.settings.OutputPath);
            exporter.OneShot ();
            yield return null;
            TestAbcImported (exporter.recorder.settings.OutputPath, 0);
        }

        [UnityTest]
        public IEnumerator TestTimeSampling([Values(aeTimeSamplingType.Acyclic, aeTimeSamplingType.Uniform)]int sampleType)
        {
            director.Play();
            exporter.recorder.settings.conf.TimeSamplingType = (aeTimeSamplingType)sampleType;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.recorder.settings.OutputPath);
            var go = TestAbcImported (exporter.recorder.settings.OutputPath);
            yield return TestCubeContents(go);
        }
        
        [UnityTest]
        public IEnumerator TestXForm([Values(aeXformType.Matrix,aeXformType.TRS)]int xFormType)
        {
            director.Play();
            exporter.recorder.settings.conf.XformType = (aeXformType) xFormType;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.recorder.settings.OutputPath);
            var go = TestAbcImported (exporter.recorder.settings.OutputPath);
            yield return TestCubeContents(go);
        }
        
        [UnityTest]
        public IEnumerator TestArchiveType([Values(aeArchiveType.Ogawa, aeArchiveType.HDF5)]int archiveType)
        {
            director.Play();
            exporter.recorder.settings.conf.ArchiveType = (aeArchiveType) archiveType;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.recorder.settings.OutputPath);
            var go = TestAbcImported (exporter.recorder.settings.OutputPath);
            yield return TestCubeContents(go);
        }
        
        [UnityTest]
        public IEnumerator TestSwapHandedness([Values(true, false)]bool swap)
        {
            director.Play();
            exporter.recorder.settings.conf.SwapHandedness = swap;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.recorder.settings.OutputPath);
            var go = TestAbcImported (exporter.recorder.settings.OutputPath);
            yield return TestCubeContents(go);
        }
        
        
        // Crash when doing swapFaces ( FTV-124 )
        [UnityTest]
        public IEnumerator TestSwapFaces([Values(/*true,*/ false)]bool swap)
        {
            director.Play();
            exporter.recorder.settings.conf.SwapFaces = swap;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.recorder.settings.OutputPath);
            var go = TestAbcImported (exporter.recorder.settings.OutputPath);
            yield return TestCubeContents(go);
        }
        
        [UnityTest]
        public IEnumerator TestScaleFactor()
        {
            director.Play();
            exporter.recorder.settings.conf.ScaleFactor = 1;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.recorder.settings.OutputPath);
            var go = TestAbcImported (exporter.recorder.settings.OutputPath);
            yield return TestCubeContents(go);
        }
        
        [UnityTest]
        public IEnumerator TestFrameRate([Values(12, 120)]float frameRate)
        {
            director.Play();
            exporter.recorder.settings.conf.FrameRate = frameRate;
            exporter.maxCaptureFrame = 30;
            yield return RecordAlembic();
            deleteFileList.Add(exporter.recorder.settings.OutputPath);
            var go = TestAbcImported (exporter.recorder.settings.OutputPath);
            yield return TestCubeContents(go);
        }
    }
}
