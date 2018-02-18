using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UTJ.Alembic
{
    [System.ComponentModel.DisplayName("Alembic Recorder Clip")]
    public class AlembicRecorderClip : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField] AlembicRecorderSettings m_settings = new AlembicRecorderSettings();
        [SerializeField] bool m_ignoreFirstFrame = true;

        public AlembicRecorderSettings settings { get { return m_settings; } }
        public ClipCaps clipCaps { get { return ClipCaps.None; } }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var ret = ScriptPlayable<AlembicRecorderBehaviour>.Create(graph);
            var behaviour = ret.GetBehaviour();
            behaviour.settings = m_settings;
            behaviour.ignoreFirstFrame = m_ignoreFirstFrame;
            return ret;
        }

        public virtual void OnDestroy()
        {
        }
    }
}
