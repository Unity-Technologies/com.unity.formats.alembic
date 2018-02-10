using System;
using UnityEngine.Playables;

namespace UTJ.Alembic
{
    public class AlembicRecorderPlayableBehaviour : PlayableBehaviour
    {
        PlayState m_playState = PlayState.Paused;
        WaitForEndOfFrameComponent m_endOfFrameComp;
        bool m_firstOneSkipped;

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
            m_playState = PlayState.Playing;
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (m_playState == PlayState.Playing)
            {
            }

            m_playState = PlayState.Paused;
        }

        public void OnFrameEnd()
        {
            // todo
        }
    }
}
