using UnityEngine;
using UnityEngine.Formats.Alembic.Util;
using UnityEngine.Timeline;

namespace UnityEngine.Formats.Alembic.Timeline
{
    /// <summary>
    /// Timeline integration class for the Alembic Recorder
    /// </summary>
    [System.Serializable]
    [TrackClipType(typeof(AlembicRecorderClip))]
    [TrackColor(0.33f, 0.0f, 0.08f)]
    public class AlembicRecorderTrack : TrackAsset
    {
    }
}
