using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif



namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    [AddComponentMenu("UTJ/Alembic/Exporter")]
    public class AlembicExporter : MonoBehaviour
    {
        #region fields
        [SerializeField] AlembicRecorder m_recorder = new AlembicRecorder();
        [SerializeField] bool m_captureOnStart = true;
        [SerializeField] bool m_ignoreFirstFrame = true;
        [SerializeField] int m_maxCaptureFrame = 0;
        int m_prevFrame = 0;
        bool m_firstFrame;
        #endregion


        #region properties
        public AlembicRecorder recorder { get { return m_recorder; } }
        public bool captureOnStart { get { return m_captureOnStart; } set { m_captureOnStart = value; } }
        public bool ignoreFirstFrame { get { return m_ignoreFirstFrame; } set { m_ignoreFirstFrame = value; } }
        public int maxCaptureFrame { get { return m_maxCaptureFrame; } set { m_maxCaptureFrame = value; } }
        #endregion


        #region private methods

        void InitializeOutputPath()
        {
            var settings = m_recorder.settings;
            if (settings.outputPath == null || settings.outputPath == "")
            {
                settings.outputPath = "Output/" + gameObject.name + ".abc";
            }
        }

        IEnumerator ProcessRecording()
        {
            yield return new WaitForEndOfFrame();

            if (!m_recorder.recording || Time.frameCount == m_prevFrame) { yield break; }
            m_prevFrame = Time.frameCount;
            if (m_captureOnStart && m_ignoreFirstFrame && m_firstFrame)
            {
                m_firstFrame = false;
                yield break;
            }

            m_recorder.ProcessRecording();

            if (m_maxCaptureFrame > 0 && m_recorder.frameCount >= m_maxCaptureFrame)
                EndRecording();
        }
        #endregion


        #region public methods
        public void BeginRecording()
        {
            m_firstFrame = true;
            m_prevFrame = -1;

            m_recorder.targetBranch = gameObject;
            m_recorder.BeginRecording();
        }

        public void EndRecording()
        {
            m_recorder.EndRecording();
        }

        public void OneShot()
        {
            BeginRecording();
            m_recorder.ProcessRecording();
            EndRecording();
        }
        #endregion


        #region messages
#if UNITY_EDITOR
        void Reset()
        {
            AlembicRecorder.ForceDisableBatching();
            InitializeOutputPath();
        }
#endif

        void OnEnable()
        {
            InitializeOutputPath();
        }

        void Start()
        {
            if (m_captureOnStart
#if UNITY_EDITOR
                && EditorApplication.isPlaying
#endif
                )
            {
                BeginRecording();
            }
        }

        void Update()
        {
            if(m_recorder.recording)
            {
                StartCoroutine(ProcessRecording());
            }
        }

        void OnDisable()
        {
            EndRecording();
        }
        #endregion
    }
}
