#if RECORDER_AVAILABLE
using UnityEditor.Recorder;
namespace UnityEditor.Formats.Alembic.Recorder
{
    class AlembicInput: RecorderInput
    {
     //   UnityEngine.Formats.Alembic.Util.AlembicRecorder recorder;
     protected override void BeginRecording(RecordingSession session)
        {
         //   var abcSettings = (AlembicInputSettings) settings;
         //   recorder = new UnityEngine.Formats.Alembic.Util.AlembicRecorder {Settings = abcSettings.Settings};
         //   recorder.BeginRecording();
        }

     protected override void FrameDone(RecordingSession session)
        {
         //   recorder.ProcessRecording();
        }

     protected override void EndRecording(RecordingSession session)
        {
          // recorder.EndRecording();
          // recorder.Dispose();
          // recorder = null;
        }
    }
}
#endif
