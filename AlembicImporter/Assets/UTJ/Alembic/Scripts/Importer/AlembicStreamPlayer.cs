using UnityEngine;

namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    public class AlembicStreamPlayer : MonoBehaviour
    {
        private AlembicStream _stream = null;

        [HideInInspector]
        public AlembicStream Stream
        {
            get { return _stream; }
        }

        public AlembicPlaybackSettings m_PlaybackSettings;
        [HideInInspector] public AlembicDiagnosticSettings m_Diagnotics;
        [ReadOnly] public AlembicStreamDescriptor m_StreamDescriptor;
        [SerializeField] private float m_time;

        public float CurrentTime
        {
            get { return m_time; }
            set
            {
                m_time = value;
            }
        }

        void LateUpdate()
        {
            if (Stream != null)
                if (!Stream.AbcUpdate(m_time))
                    Stream.AbcRecoverContext(gameObject);
        }

        void OnEnable()
        {
            if (_stream == null)
            {
                if (m_PlaybackSettings == null)
                {
                    m_PlaybackSettings = new AlembicPlaybackSettings();
                }

                if (m_StreamDescriptor == null)
                    return;

                _stream = new AlembicStream(gameObject, m_StreamDescriptor.m_ImportSettings, m_PlaybackSettings, m_Diagnotics);
                Stream.AbcLoad(false);
            }
        }

        public void OnDestroy()
        {
            if (Stream != null)
                Stream.Dispose();
        }

        public void OnApplicationQuit()
        {
            AbcAPI.aiCleanup();
        }

        public void ForceRefresh()
        {
            if (Stream != null)
                Stream.ForcedRefresh();
        }
    }
}
