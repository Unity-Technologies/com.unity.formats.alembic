using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.Playables;

namespace UnityEngine.Formats.Alembic.Timeline
{
    internal class AlembicShotPlayable : PlayableBehaviour
    {
        public AlembicStreamPlayer streamPlayer { get; set; }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (streamPlayer == null)
                return;

            var duration = streamPlayer.Duration;
            var time = playable.GetTime();
            streamPlayer.CurrentTime = (float)(time >= duration ? duration : time % duration);
            if (info.evaluationType == FrameData.EvaluationType.Playback)
            {
                streamPlayer.Update();
            }
            else
            {
                streamPlayer.UpdateImmediately(streamPlayer.CurrentTime);
            }
        }
    }
}
