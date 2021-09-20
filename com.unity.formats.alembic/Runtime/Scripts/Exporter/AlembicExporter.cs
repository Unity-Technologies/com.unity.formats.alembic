using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Formats.Alembic.Util;

namespace UnityEngine.Formats.Alembic.Exporter
{
    /// <summary>
    /// Component that records the Unity Scene state and exports it as an Alembic file. This class records only in Play Mode.
    /// </summary>
    [ExecuteInEditMode]
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
        /// <summary>
        /// Reference to the Alembic recorder (lower level class that implements most of the functionality).
        /// </summary>
        public AlembicRecorder Recorder { get { return m_recorder; } }
        /// <summary>
        /// Enable to start capturing immediately after entering the Play Mode.
        /// </summary>
        public bool CaptureOnStart { get { return m_captureOnStart; } set { m_captureOnStart = value; } }
        /// <summary>
        /// Enable to skip capturing the first frame (only available when CaptureOnStart is enabled).
        /// </summary>
        public bool IgnoreFirstFrame { get { return m_ignoreFirstFrame; } set { m_ignoreFirstFrame = value; } }
        /// <summary>
        /// Get or set the number of frames to capture. If set to 0, the capture runs indefinitely.
        /// </summary>
        public int MaxCaptureFrame { get { return m_maxCaptureFrame; } set { m_maxCaptureFrame = value; } }
        #endregion


        #region private methods

        void InitializeOutputPath()
        {
            var settings = m_recorder.Settings;
            if (string.IsNullOrEmpty(settings.OutputPath))
            {
                settings.OutputPath = "Output/" + gameObject.name + ".abc";
            }
        }

        IEnumerator ProcessRecording()
        {
            yield return new WaitForEndOfFrame();

            if (!m_recorder.Recording || Time.frameCount == m_prevFrame) { yield break; }
            m_prevFrame = Time.frameCount;
            if (m_captureOnStart && m_ignoreFirstFrame && m_firstFrame)
            {
                m_firstFrame = false;
                yield break;
            }

            m_recorder.ProcessRecording();

            if (m_maxCaptureFrame > 0 && m_recorder.FrameCount >= m_maxCaptureFrame)
                EndRecording();
        }

        #endregion


        #region public methods
        /// <summary>
        /// Starts a recording session. Use this method if CaptureOnStart is disabled.
        /// </summary>
        public void BeginRecording()
        {
            m_firstFrame = true;
            m_prevFrame = -1;
            m_recorder.BeginRecording();
            AlembicExporterAnalytics.SendAnalytics(m_recorder.Settings);
        }

        /// <summary>
        /// Ends the recording session.
        /// </summary>
        public void EndRecording()
        {
            m_recorder.EndRecording();
        }

        /// <summary>
        /// Exports only the current frame.
        /// </summary>
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
            if (m_recorder.Recording)
            {
                StartCoroutine(ProcessRecording());
            }
        }

        void OnDisable()
        {
            EndRecording();
        }

        void OnDestroy()
        {
            if (Recorder != null) Recorder.Dispose();
        }

        #endregion
    }
}
