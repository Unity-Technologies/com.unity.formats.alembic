#if RECORDER_AVAILABLE
using UnityEditor.Recorder;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEditor.Formats.Alembic.Recorder
{
    class AlembicRecorder : GenericRecorder<AlembicRecorderSettings>
    {
        UnityEngine.Formats.Alembic.Util.AlembicRecorder recorder;
        protected override bool BeginRecording(RecordingSession session)
        {
            var abcSettings =  Settings;
            recorder = new UnityEngine.Formats.Alembic.Util.AlembicRecorder {Settings = abcSettings.Settings};
            recorder.Settings.FixDeltaTime = true;
            recorder.Settings.ExportOptions.FrameRate = abcSettings.FrameRate;
            recorder.Settings.ExportOptions.TimeSamplingType = TimeSamplingType.Uniform;

            abcSettings.FileNameGenerator.CreateDirectory(session);
            var absolutePath = FileNameGenerator.SanitizePath(abcSettings.FileNameGenerator.BuildAbsolutePath(session));
            var clipName = absolutePath.Replace(FileNameGenerator.SanitizePath(UnityEngine.Application.dataPath), "Assets");
            recorder.Settings.OutputPath = clipName;
            recorder.BeginRecording();
            return base.BeginRecording(session);
        }

        protected override void RecordFrame(RecordingSession ctx)
        {
            recorder.ProcessRecording();
        }

        protected override void EndRecording(RecordingSession session)
        {
            recorder.EndRecording();
            base.EndRecording(session);
        }
    }
}

#endif
