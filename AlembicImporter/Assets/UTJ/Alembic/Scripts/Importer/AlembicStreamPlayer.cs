using UnityEngine;

namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    public class AlembicStreamPlayer : MonoBehaviour
    {
        private AlembicStream _stream = null;
        public bool m_ManualUpdateMode = false;

        [HideInInspector]
        public AlembicStream Stream
        {
            get { return _stream; }
        }

        public AlembicPlaybackSettings m_PlaybackSettings;
        [HideInInspector] public AlembicDiagnosticSettings m_Diagnotics;
        [ReadOnly] public AlembicStreamDescriptor m_StreamDescriptor;

        void OnEnable()
        {
            // Should not be needed...
            bool newPlayerSettings = false;
            if (m_PlaybackSettings == null)
            {
                m_PlaybackSettings = new AlembicPlaybackSettings();
                newPlayerSettings = true;
            }

            if (_stream == null)
            {
                if (m_StreamDescriptor == null)
                    return;

                _stream = new AlembicStream(gameObject, m_StreamDescriptor.m_ImportSettings, m_PlaybackSettings, m_Diagnotics);
                Stream.AbcLoad(false);

                // Safety net...
                if (newPlayerSettings)
                {
                    m_PlaybackSettings.m_startTime = Stream.AbcStartTime;
                    m_PlaybackSettings.m_endTime = Stream.AbcEndTime;
                }
            }

            // Re-importing the asset will create a new import settings asset and the stream will be holding on to an old one...
            if (_stream!=null && m_StreamDescriptor!= null && Stream.ImportSettings != m_StreamDescriptor.m_ImportSettings)
                Stream.ImportSettings = m_StreamDescriptor.m_ImportSettings;

        }

        public void ManualUpdate()
        {
            if (Stream != null && m_ManualUpdateMode == true)
               if (!Stream.ProcessUpdateEvent())
                    Stream.AbcRecoverContext(gameObject);
        }

        public void Update()
        {
            if (Stream != null && m_ManualUpdateMode == false)
                if (!Stream.ProcessUpdateEvent())
                    Stream.AbcRecoverContext(gameObject);
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
