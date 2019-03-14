using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Formats.Alembic.Exporter;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    public class TimelineDataTests : BaseFixture
    {
        PlayableDirector director;

        static IEnumerator TestCubeContents(GameObject go)
        {
            var root = PrefabUtility.InstantiatePrefab(go) as GameObject;
            var player = root.GetComponent<AlembicStreamPlayer>();

            var cube = root.transform.GetChild(1).gameObject; // First is Camera, Second is Cube
            player.CurrentTime = 0;
            yield return new WaitForEndOfFrame();
            var t0 = cube.transform.localPosition;
            player.CurrentTime = (float)player.duration;
            yield return new WaitForEndOfFrame();
            var t1  = cube.transform.localPosition;
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
            
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.AddComponent<Animator>();
            director = cube.AddComponent<PlayableDirector>();
            director.playableAsset = timeline;
            director.SetGenericBinding(aTrack,cube);
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
