using UnityEngine;

namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    public class AlembicStreamPlayer : MonoBehaviour
    {
        public AlembicStream m_stream;
        public AlembicStreamDescriptor m_streamDescriptor;
        [SerializeField] public float m_currentTime;
        [SerializeField] public int m_startFrame;
        [SerializeField] public int m_endFrame;
        [SerializeField] public float m_vertexMotionScale = 1.0f;
        float m_lastUpdateTime;
        bool m_forceUpdate = false;
        bool m_updateStarted = false;

        public float duration
        {
            get
            {
               return (m_endFrame- m_startFrame) * m_streamDescriptor.frameLength;
            }
        }

        void OnValidate()
        {
            if (m_streamDescriptor == null) return;
            if (m_startFrame < m_streamDescriptor.minFrame) m_startFrame = m_streamDescriptor.minFrame;
            if (m_startFrame > m_streamDescriptor.maxFrame) m_startFrame = m_streamDescriptor.maxFrame;
            if (m_endFrame < m_startFrame) m_endFrame = m_startFrame; 
            if (m_endFrame > m_streamDescriptor.maxFrame) m_endFrame = m_streamDescriptor.maxFrame;    
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
                if (m_stream.AbcUpdateBegin(m_currentTime + m_startFrame * m_streamDescriptor.frameLength + m_streamDescriptor.abcStartTime))
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
            var d = duration;
            if (d == .0f || m_currentTime < .0f)
                m_currentTime = .0f;
            else if (m_currentTime > d)
                m_currentTime = d;
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
