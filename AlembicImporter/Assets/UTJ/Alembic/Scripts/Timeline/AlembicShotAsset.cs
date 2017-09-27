#if UNITY_2017_1_OR_NEWER

using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UTJ.Alembic
{
    public class AlembicShotAsset : PlayableAsset, ITimelineClipAsset
    {
        AlembicStreamPlayer m_Stream;

        [Tooltip("Alambic asset to play")]
        public ExposedReference<AlembicStreamPlayer> streamPlayer;

        [Tooltip("Amount of time to clip off the start of the alembic asset from playback.")]
        [SerializeField] public float startOffset;

        [Tooltip("Amount of time to clip off the end of the alembic asset from playback.")]
        [SerializeField] public float endOffset;

        [Tooltip("Use to compress/dilute time play back.")]
        [SerializeField] public float timeScale = 1f;

		[Tooltip("Controls how playback cycles throught the stream.")]
		[SerializeField] public AlembicPlaybackSettings.CycleType m_Cycle = AlembicPlaybackSettings.CycleType.Hold;

        public ClipCaps clipCaps { get { return ClipCaps.None;  } }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<AlembicShotPlayable>.Create(graph);
            var behaviour = playable.GetBehaviour();
            m_Stream = streamPlayer.Resolve(graph.GetResolver());
            behaviour.streamPlayer = m_Stream;
            behaviour.startTimeOffset = startOffset;
            behaviour.endTimeClipOff = endOffset;
            behaviour.timeScale = timeScale;
            behaviour.cycle = m_Cycle;
            return playable;
        }

        public override double duration
        {
            get
            {   
                return m_Stream == null ?
                    0 : ((m_Stream.m_PlaybackSettings.m_endTime - endOffset) - (m_Stream.m_PlaybackSettings.m_startTime + startOffset)) / timeScale;
            }
        }

    }
}

#endif