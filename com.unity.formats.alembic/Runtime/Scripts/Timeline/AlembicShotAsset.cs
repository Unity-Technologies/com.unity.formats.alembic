using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEngine.Formats.Alembic.Timeline
{
    /// <summary>
    /// Clip representing the playback range of an Alembic asset.
    /// </summary>
    [System.ComponentModel.DisplayName("Alembic Shot")]
    public class AlembicShotAsset : PlayableAsset, ITimelineClipAsset, IPropertyPreview
    {
        AlembicStreamPlayer m_stream;

        [Tooltip("Alembic asset to play")]
        [SerializeField]
        private ExposedReference<AlembicStreamPlayer> streamPlayer;

        ClipCaps ITimelineClipAsset.clipCaps { get { return ClipCaps.Extrapolation | ClipCaps.Looping | ClipCaps.SpeedMultiplier | ClipCaps.ClipIn;  } }

        /// <summary>
        /// The AlembicStreamPlayer to play.
        /// </summary>
        public ExposedReference<AlembicStreamPlayer> StreamPlayer
        {
            get { return streamPlayer; }
            set { streamPlayer = value; }
        }

        /// <summary>
        /// For more information see: https://docs.unity3d.com/ScriptReference/Playables.PlayableAsset.CreatePlayable.html
        /// </summary>
        /// <param name="graph">The Playable Graph.</param>
        /// <param name="owner">The GameObject containing the PlayableDirector.</param>
        /// <returns>The playable that drives the AlembicStreamPlayer.</returns>
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<AlembicShotPlayable>.Create(graph);
            var behaviour = playable.GetBehaviour();
            m_stream = StreamPlayer.Resolve(graph.GetResolver());
            behaviour.streamPlayer = m_stream;
            return playable;
        }

        /// <summary>
        /// Returns the duration selected through the AlembicStreamPlayer.
        /// </summary>
        public override double duration
        {
            get
            {
                return m_stream == null ? 0 : m_stream.Duration;
            }
        }

        /// <inheritdoc/>
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
