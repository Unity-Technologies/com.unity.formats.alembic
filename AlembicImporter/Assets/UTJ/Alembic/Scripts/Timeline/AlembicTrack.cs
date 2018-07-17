using UnityEngine;
using UnityEngine.Timeline;

namespace UTJ.Alembic
{
    [System.Serializable]
    [TrackClipType(typeof(AlembicShotAsset))]
    [TrackColor(0.53f, 0.0f, 0.08f)]
#if !UNITY_2018_2_OR_NEWER
    [TrackMediaType(TimelineAsset.MediaType.Script)]
#endif
    public class AlembicTrack : TrackAsset
    {
    }
}
