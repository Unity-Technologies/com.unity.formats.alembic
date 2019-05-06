using System;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEngine.Formats.Alembic.Timeline
{
    [System.ComponentModel.DisplayName("Alembic Shot")]
    internal class AlembicShotAsset : PlayableAsset, ITimelineClipAsset, IPropertyPreview
    {
        AlembicStreamPlayer m_stream;

        [Tooltip("Alembic asset to play")]
        [SerializeField]
        private ExposedReference<AlembicStreamPlayer> streamPlayer;
        public ExposedReference<AlembicStreamPlayer> StreamPlayer
        {
            get { return streamPlayer; }
            set { streamPlayer = value; }
        }

        [Tooltip("Amount of time to clip off the end of the alembic asset from playback.")]
        [SerializeField]
        private float endOffset;
        public float EndOffset
        {
            get { return endOffset; }
            set { endOffset = value; }
        }

        public ClipCaps clipCaps { get { return ClipCaps.Extrapolation | ClipCaps.Looping | ClipCaps.SpeedMultiplier | ClipCaps.ClipIn;  } }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<AlembicShotPlayable>.Create(graph);
            var behaviour = playable.GetBehaviour();
            m_stream = StreamPlayer.Resolve(graph.GetResolver());
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

        public void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            var streamComponent = streamPlayer.Resolve(director);
            if (streamComponent != null)
            {
                driver.AddFromName<AlembicStreamPlayer>(streamComponent.gameObject, "currentTime");
            }
        }
    }
}
