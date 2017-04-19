#if UNITY_2017_1_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UTJ.Alembic
{
    [System.Serializable]
    [TrackClipType(typeof(AlembicShotAsset))]
    [TrackMediaType(TimelineAsset.MediaType.Script)]
    [TrackColor(0.53f, 0.0f, 0.08f)]
    public class AlembicTrack : TrackAsset
    {
    }
}

#endif
