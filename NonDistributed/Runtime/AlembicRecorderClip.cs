using UnityEngine.Formats.Alembic.Util;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEngine.Formats.Alembic.Timeline
{
    [System.ComponentModel.DisplayName("Alembic Recorder Clip")]
    internal class AlembicRecorderClip : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField] AlembicRecorderSettings m_settings = new AlembicRecorderSettings();
        [SerializeField] bool m_ignoreFirstFrame = true;
        [SerializeField] string m_targetBranchTag = "";

        public AlembicRecorderSettings settings
        {
            get { return m_settings; }
        }

        public ClipCaps clipCaps
        {
            get { return ClipCaps.None; }
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var gos = new GameObject[] {};
            if (!string.IsNullOrEmpty(m_targetBranchTag))
            {
                gos = GameObject.FindGameObjectsWithTag(m_targetBranchTag);
            }

            var ret = ScriptPlayable<AlembicRecorderBehaviour>.Create(graph);
            var behaviour = ret.GetBehaviour();
            behaviour.settings = m_settings;
            behaviour.ignoreFirstFrame = m_ignoreFirstFrame;
            m_settings.TargetBranch = gos.Length == 1 ? gos[0] : null;

            return ret;
        }
    }
}
