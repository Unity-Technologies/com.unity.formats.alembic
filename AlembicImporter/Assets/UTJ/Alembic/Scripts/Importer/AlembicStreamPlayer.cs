using UnityEngine;

namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    public class AlembicStreamPlayer : MonoBehaviour
    {
        public AlembicStream Stream;
        public AlembicStreamDescriptor streamDescriptor;
        [SerializeField] public float currentTime;
        [SerializeField] public int startFrame;
        [SerializeField] public int endFrame;
        [SerializeField] public float vertexMotionScale = 1.0f;
        [SerializeField] public bool interpolateSamples = true;
        float m_LastUpdateTime;
        bool m_ForceUpdate = false;

        public float Duration
        {
            get
            {
               return (endFrame- startFrame) * streamDescriptor.FrameLength;
            }
        }

        void OnValidate()
        {
            if (streamDescriptor == null) return;
            if (startFrame < streamDescriptor.minFrame) startFrame = streamDescriptor.minFrame;
            if (startFrame > streamDescriptor.maxFrame) startFrame = streamDescriptor.maxFrame;
            if (endFrame < startFrame) endFrame = startFrame; 
            if (endFrame > streamDescriptor.maxFrame) endFrame = streamDescriptor.maxFrame;    
            ClampTime();
            m_ForceUpdate = true;
        } 

        void LateUpdate()
        {
            if (Stream != null && streamDescriptor != null)
            {
                ClampTime();
                if (m_LastUpdateTime != currentTime || m_ForceUpdate)
                {
                    if (Stream.AbcUpdate(currentTime + startFrame * streamDescriptor.FrameLength + streamDescriptor.abcStartTime, vertexMotionScale, interpolateSamples))
                    {
                        m_LastUpdateTime = currentTime;
                        m_ForceUpdate = false;    
                    }
                    else
                    {
                        Stream.Dispose();
                        LoadStream();
                    }
                }
            }
        }

        private void ClampTime()
        {
            float duration = Duration;
            if (duration == .0f || currentTime < .0f)
                currentTime = .0f;
            else if (currentTime > duration)
                currentTime = duration;
        }

        public void LoadStream()
        {
            if (streamDescriptor == null) return;
            Stream = new AlembicStream(gameObject, streamDescriptor);
            Stream.AbcLoad();
            m_ForceUpdate = true;
        }

        void OnEnable()
        {
            if (Stream == null)
            {
                LoadStream();
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
    }
}
