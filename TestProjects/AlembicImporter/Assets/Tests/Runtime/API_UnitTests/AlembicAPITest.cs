using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine.Formats.Alembic.Util;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    public class AlembicAPITest
    {
        [UnityTest]
        public IEnumerator TestAPIBasics()
        {
            using (AlembicRecorder recorder = new AlembicRecorder())
            {
                var conf = recorder.settings.Conf;
                conf.FrameRate = 24;

                conf.FrameRate = 24;
                conf.ScaleFactor = 1;
                conf.SwapHandedness = true;
                recorder.settings.CaptureCamera = false;
                recorder.settings.AssumeNonSkinnedMeshesAreConstant = false;
                recorder.settings.CaptureMeshRenderer = true;
                recorder.settings.MeshSubmeshes = true;
                //Time.maximumDeltaTime = (1.0f / m_recorder.settings.conf.frameRate);
                conf.ArchiveType = UnityEngine.Formats.Alembic.Sdk.aeArchiveType.Ogawa;
                recorder.settings.Conf = conf;

                // assert that all the settings are correct


                var result = recorder.BeginRecording();
                Assert.That(result, Is.True);
                recorder.ProcessRecording();
                recorder.EndRecording();
            }
            yield return null;
        }
    }
}
