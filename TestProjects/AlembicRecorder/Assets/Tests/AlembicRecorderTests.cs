using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor.Recorder.Timeline;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.Formats.Alembic.Util;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Timeline;

namespace Tests
{
    public class AlembicRecorderTests
    {
        const string sceneName = "Scene";
        readonly List<string> deleteFileList = new List<string>();
        [SetUp]
        public void SetUp()
        {
            var scene = SceneManager.CreateScene(sceneName);
            SceneManager.SetActiveScene(scene);
            var cam = new GameObject("Cam");
            cam.AddComponent<Camera>();
            cam.transform.localPosition = new Vector3(0, 1, -10);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var asyncOperation = SceneManager.UnloadSceneAsync(sceneName);
            while (!asyncOperation.isDone)
            {
                yield return null;
            }

            foreach (var file in deleteFileList)
            {
                File.Delete(file);
            }

            deleteFileList.Clear();
        }

        [UnityTest]
        public IEnumerator AlembicRecorderRecords()
        {
            var curve = AnimationCurve.Linear(0, 0, 10, 10);
            var clip = new AnimationClip {hideFlags = HideFlags.DontSave};
            clip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.hideFlags = HideFlags.DontSave;
            var aTrack = timeline.CreateTrack<AnimationTrack>(null, "CubeAnimation");
            aTrack.CreateClip(clip).displayName = "CubeClip";

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.AddComponent<Animator>();
            var director = cube.AddComponent<PlayableDirector>();
            director.playableAsset = timeline;
            director.SetGenericBinding(aTrack, cube);

            var rClip = timeline.CreateTrack<RecorderTrack>().CreateDefaultClip().asset as RecorderClip;
            var abcSettings = ScriptableObject
                .CreateInstance<UnityEditor.Formats.Alembic.Recorder.AlembicRecorderSettings>();
            rClip.settings = abcSettings;
            abcSettings.Settings.Scope = ExportScope.TargetBranch;
            abcSettings.FrameRate = 30;
            var outputFile = Path.Combine(Application.dataPath, "..", "SampleRecordings", "test");
            abcSettings.OutputFile = outputFile;
            abcSettings.Settings.TargetBranch = cube;

            director.Play();
            deleteFileList.Add(outputFile+".abc");
            while (Time.time < 10)
            {
                yield return null;
            }

            var go = new GameObject("abc");
            var player = go.AddComponent<AlembicStreamPlayer>();
            var ret = player.LoadFromFile(outputFile+".abc");
            Assert.IsTrue(ret);
            player.UpdateImmediately(5);
            var pos = go.GetComponentInChildren<MeshFilter>().transform.position;
            Assert.IsTrue(Vector3.Distance(pos, new Vector3(4.96666f,0,0)) < 1e-3);
        }
    }
}
