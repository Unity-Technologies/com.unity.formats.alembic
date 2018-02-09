using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UTJ.Alembic
{
    [System.ComponentModel.DisplayName("Alembic Recorder Clip")]
    public class AlembicRecorderClip : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps { get { return ClipCaps.None; } }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<AlembicRecorderPlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            // todo
            return playable;
        }

        public virtual void OnDestroy()
        {
        }
    }
}
