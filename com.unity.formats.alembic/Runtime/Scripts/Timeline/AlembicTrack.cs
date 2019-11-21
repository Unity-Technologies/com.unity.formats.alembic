using UnityEngine;
using UnityEngine.Timeline;

namespace UnityEngine.Formats.Alembic.Timeline
{
    /// <summary>
    /// Timeline integration class for the Alembic Recorder.
    /// </summary>
    [System.Serializable]
    [TrackClipType(typeof(AlembicShotAsset))]
    [TrackColor(0.53f, 0.0f, 0.08f)]
    public class AlembicTrack : TrackAsset
    {
    }
}
