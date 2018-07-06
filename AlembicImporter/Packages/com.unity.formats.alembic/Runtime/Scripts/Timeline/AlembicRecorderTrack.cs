using UnityEngine;
using UnityEngine.Formats.Alembic.Util;
using UnityEngine.Timeline;

namespace UnityEngine.Formats.Alembic.Timeline
{
    [System.Serializable]
    [TrackClipType(typeof(AlembicRecorderClip))]
    [TrackColor(0.33f, 0.0f, 0.08f)]
    internal class AlembicRecorderTrack : TrackAsset
    {
    }
}
