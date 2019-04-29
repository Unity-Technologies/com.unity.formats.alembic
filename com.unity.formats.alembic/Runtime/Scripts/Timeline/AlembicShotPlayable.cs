using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.Playables;

namespace UnityEngine.Formats.Alembic.Timeline
{
    internal class AlembicShotPlayable : PlayableBehaviour
    {
        public AlembicStreamPlayer streamPlayer { get; set; }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            if (streamPlayer == null)
                return;

            var duration = streamPlayer.duration;
            var time = playable.GetTime();
            streamPlayer.CurrentTime = (float)(time == duration ? duration : time % duration);
            streamPlayer.Update();
        }
    }
}
