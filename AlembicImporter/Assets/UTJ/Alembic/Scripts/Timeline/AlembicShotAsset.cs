using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UTJ.Alembic
{
    [System.ComponentModel.DisplayName("Alembic Shot")]
    public class AlembicShotAsset : PlayableAsset, ITimelineClipAsset
    {
        AlembicStreamPlayer m_stream;

        [Tooltip("Alembic asset to play")]
        public ExposedReference<AlembicStreamPlayer> streamPlayer;

        [Tooltip("Amount of time to clip off the end of the alembic asset from playback.")]
        [SerializeField] public float endOffset;

        public ClipCaps clipCaps { get { return ClipCaps.Extrapolation | ClipCaps.Looping | ClipCaps.SpeedMultiplier | ClipCaps.ClipIn;  } }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<AlembicShotPlayable>.Create(graph);
            var behaviour = playable.GetBehaviour();
            m_stream = streamPlayer.Resolve(graph.GetResolver());
            behaviour.streamPlayer = m_stream;
            return playable;
        }

        public override double duration
        {
            get
            {   
                return m_stream == null ? 0 : m_stream.duration;
            }
        }

    }
}
