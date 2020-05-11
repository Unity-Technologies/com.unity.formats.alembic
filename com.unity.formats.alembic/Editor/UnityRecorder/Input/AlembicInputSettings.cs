#if RECORDER_AVAILABLE
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor.Recorder;
using UnityEngine;

namespace UnityEditor.Formats.Alembic.Recorder
{
    [Serializable]
    [DisplayName("Alembic")]
    class AlembicInputSettings : RecorderInputSettings
    {
        protected override Type InputType => typeof(AlembicInput);

        protected override bool ValidityCheck(List<string> errors)
        {
            return true;
        }
    }
}
#endif
