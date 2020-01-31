#if RECORDER_AVAILABLE
using UnityEditor.Formats.Alembic.Exporter;
using UnityEditor.Recorder;

namespace UnityEditor.Formats.Alembic.Recorder
{
    [CustomEditor(typeof(AlembicRecorderSettings))]
    public class AlembicRecorderSettingsEditor : RecorderEditor
    {
        bool m_foldCaptureComponents;
        bool m_foldMeshComponents;
        public override void OnInspectorGUI()
        {
            var t = target as AlembicRecorderSettings;
            AlembicExporterEditor.DrawSettings(serializedObject,
                t.Settings,
                "settings.", ref m_foldCaptureComponents, ref m_foldMeshComponents, true);
        }
    }
}
#endif
