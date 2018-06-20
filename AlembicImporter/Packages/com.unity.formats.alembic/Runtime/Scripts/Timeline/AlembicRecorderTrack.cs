using UnityEngine;
using UnityEngine.Formats.Alembic.Util;
using UnityEngine.Timeline;

#if ENABLE_ALEMBIC_TIMELINE_RECORDER
namespace UnityEngine.Formats.Alembic.Timeline
{
    [System.Serializable]
    [TrackClipType(typeof(AlembicRecorderClip))]
    [TrackColor(0.33f, 0.0f, 0.08f)]
    internal class AlembicRecorderTrack : TrackAsset
    {
    }
}
#endif // ENABLE_ALEMBIC_TIMELINE_RECORDER