#if RECORDER_AVAILABLE
using System;
using System.Collections.Generic;
using UnityEditor.Recorder;
using UnityEngine;

namespace UnityEditor.Formats.Alembic.Recorder
{
    /// <inheritdoc/>
    [RecorderSettings(typeof(AlembicRecorder), "Alembic Clip", "alembic_recorder")]
    public class AlembicRecorderSettings : RecorderSettings
    {
        [SerializeField] AlembicInputSettings inputSettings = new AlembicInputSettings();
        [SerializeField]
        UnityEngine.Formats.Alembic.Util.AlembicRecorderSettings settings = new UnityEngine.Formats.Alembic.Util.AlembicRecorderSettings();
        [SerializeField] string m_BindingId = String.Empty;
        public UnityEngine.Formats.Alembic.Util.AlembicRecorderSettings Settings => settings;
        protected override string Extension => "abc";
        public override IEnumerable<RecorderInputSettings> InputsSettings
        {
            get { yield return inputSettings; }
        }

        GameObject GetTargetBranch()
        {
            return BindingManager.Get(m_BindingId) as GameObject;
        }

        void SetTargetBranch(GameObject go)
        {
            BindingManager.Set(m_BindingId, go);
        }

        public override void OnAfterDuplicate()
        {
            DuplicateExposedReference();
        }

        void OnEnable()
        {
            if (string.IsNullOrEmpty(m_BindingId))
            {
                m_BindingId = GenerateBindingId();
            }

            // The Recorder saves it's data in an asset and we need to serialize the GameObject references in a different way.
            settings.getTargetBranch = GetTargetBranch;
            settings.setTargetBranch = SetTargetBranch;
        }

        void OnDisable()
        {
            settings.getTargetBranch = null;
            settings.setTargetBranch = null;
        }

        void DuplicateExposedReference()
        {
            if (string.IsNullOrEmpty(m_BindingId))
                return;

            var src = m_BindingId;
            var dst = GenerateBindingId();

            m_BindingId = dst;

            BindingManager.Duplicate(src, dst);
        }

        static string GenerateBindingId()
        {
            return GUID.Generate().ToString();
        }
    }
}

#endif
