#if UNITY_2017_1_OR_NEWER

using UnityEngine.Playables;

namespace UTJ.Alembic
{
    public class AlembicShotPlayable : PlayableBehaviour
    {
        public AlembicStreamPlayer streamPlayer { get; set; }

        public float startTimeOffset;
        public float endTimeClipOff;
        public float timeScale;
        public AlembicPlaybackSettings.CycleType cycle = AlembicPlaybackSettings.CycleType.Hold;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            base.ProcessFrame(playable, info, playerData);

            if (streamPlayer == null)
                return;

            streamPlayer.m_PlaybackSettings.m_cycle = cycle;
            var time = playable.GetTime()* timeScale + startTimeOffset - endTimeClipOff;
            if (time < 0) time = 0;
            streamPlayer.CurrentTime = (float)time;
        }
    }
}

#endif
