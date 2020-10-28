#if RECORDER_AVAILABLE
using UnityEditor.Formats.Alembic.Exporter;
using UnityEditor.Recorder;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UnityEditor.Formats.Alembic.Recorder
{
    [CustomEditor(typeof(AlembicRecorderSettings))]
    class AlembicRecorderSettingsEditor : RecorderEditor
    {
        SavedBool m_foldCaptureComponents;
        SavedBool m_foldMeshComponents;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_foldCaptureComponents = new SavedBool($"{target.GetType()}.m_foldCaptureComponents", false);
            m_foldMeshComponents = new SavedBool($"{target.GetType()}.m_foldMeshComponents", false);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var t = target as AlembicRecorderSettings;
            var dirty = AlembicExporterEditor.DrawSettings(serializedObject,
                t.Settings,
                "settings.", ref m_foldCaptureComponents, ref m_foldMeshComponents, true);

            serializedObject.ApplyModifiedProperties();
            if (dirty)
            {
                EditorUtility.SetDirty(target);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }
    }
}
#endif
