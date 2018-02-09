using System;
using UnityEngine.Playables;

namespace UTJ.Alembic
{
    public class AlembicRecorderPlayableBehaviour : PlayableBehaviour
    {
        PlayState m_PlayState = PlayState.Paused;
        WaitForEndOfFrameComponent endOfFrameComp;
        bool m_FirstOneSkipped;

        public override void OnGraphStart(Playable playable)
        {
        }

        public override void OnGraphStop(Playable playable)
        {
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            m_PlayState = PlayState.Playing;
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (m_PlayState == PlayState.Playing)
            {
            }

            m_PlayState = PlayState.Paused;
        }

        public void OnFrameEnd()
        {
            // todo
        }
    }
}
