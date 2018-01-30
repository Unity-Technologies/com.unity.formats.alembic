using UnityEngine;

namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    public class AlembicStreamPlayer : MonoBehaviour
    {
        public AlembicStream m_stream;
        public AlembicStreamDescriptor m_streamDescriptor;
        [SerializeField] public double m_currentTime;
        [SerializeField] public double m_startTime;
        [SerializeField] public double m_endTime;
        [SerializeField] public float m_vertexMotionScale = 1.0f;
        [SerializeField] public bool m_asyncLoad = true;
        double m_lastUpdateTime;
        bool m_forceUpdate = false;
        bool m_updateStarted = false;

        public double duration { get { return m_endTime - m_startTime; } }

        void OnValidate()
        {
            if (m_streamDescriptor == null) return;
            m_startTime = Mathf.Clamp((float)m_startTime, (float)m_streamDescriptor.abcStartTime, (float)m_streamDescriptor.abcEndTime);
            m_endTime = Mathf.Clamp((float)m_endTime, (float)m_startTime, (float)m_streamDescriptor.abcEndTime);
            ClampTime();
            m_forceUpdate = true;
        }

        void Update()
        {
            if (m_stream == null || m_streamDescriptor == null)
                return;

            ClampTime();
            if (m_lastUpdateTime != m_currentTime || m_forceUpdate)
            {
                m_stream.vertexMotionScale = m_vertexMotionScale;
                m_stream.asyncLoad = m_asyncLoad;
                if (m_stream.AbcUpdateBegin(m_startTime + m_currentTime))
                {
                    m_lastUpdateTime = m_currentTime;
                    m_forceUpdate = false;
                    m_updateStarted = true;
                }
                else
                {
                    m_stream.Dispose();
                    m_stream = null;
                    LoadStream();
                }
            }
        }

        void LateUpdate()
        {
            if (!m_updateStarted)
                return;
            m_updateStarted = false;
            m_stream.AbcUpdateEnd();
        }

        private void ClampTime()
        {
            m_currentTime = Mathf.Clamp((float)m_currentTime, 0.0f, (float)duration);
        }

        public void LoadStream()
        {
            if (m_streamDescriptor == null)
                return;
            m_stream = new AlembicStream(gameObject, m_streamDescriptor);
            m_stream.AbcLoad();
            m_forceUpdate = true;
        }

        void OnEnable()
        {
            if (m_stream == null)
                LoadStream();
        }

        public void OnDestroy()
        {
            if (m_stream != null)
                m_stream.Dispose();
        }

        public void OnApplicationQuit()
        {
            AbcAPI.aiCleanup();
        }
    }
}
