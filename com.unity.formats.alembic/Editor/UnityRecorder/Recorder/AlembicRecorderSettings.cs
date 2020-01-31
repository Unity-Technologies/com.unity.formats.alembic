#if RECORDER_AVAILABLE
using System.Collections.Generic;
using UnityEditor.Recorder;
using UnityEngine;

namespace UnityEditor.Formats.Alembic.Recorder
{
    [RecorderSettings(typeof(AlembicRecorder), "Alembic Clip", "alembic_recorder")]
    public class AlembicRecorderSettings : RecorderSettings
    {
        [SerializeField] AlembicInputSettings inputSettings = new AlembicInputSettings();
        [SerializeField]
        UnityEngine.Formats.Alembic.Util.AlembicRecorderSettings settings = new UnityEngine.Formats.Alembic.Util.AlembicRecorderSettings();
        public UnityEngine.Formats.Alembic.Util.AlembicRecorderSettings Settings => settings;
        protected override string Extension => "abc";
        public override IEnumerable<RecorderInputSettings> InputsSettings
        {
            get { yield return inputSettings; }
        }
    }
}

#endif
