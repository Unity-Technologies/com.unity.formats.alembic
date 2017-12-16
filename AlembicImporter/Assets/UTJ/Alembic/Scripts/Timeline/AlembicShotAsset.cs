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

        [Tooltip("Alembic asset to play")]
        public ExposedReference<AlembicStreamPlayer> streamPlayer;

        [Tooltip("Amount of time to clip off the end of the alembic asset from playback.")]
        [SerializeField] public float endOffset;
		
        public ClipCaps clipCaps { get { return ClipCaps.Extrapolation | ClipCaps.Looping | ClipCaps.SpeedMultiplier | ClipCaps.ClipIn;  } }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<AlembicShotPlayable>.Create(graph);
            var behaviour = playable.GetBehaviour();
            m_Stream = streamPlayer.Resolve(graph.GetResolver());
            behaviour.streamPlayer = m_Stream;
            return playable;
        }

        public override double duration
        {
            get
            {   
                return m_Stream == null ?
                    0 : m_Stream.streamDescriptor.Duration;
            }
        }

    }
}

#endif