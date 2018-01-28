using UnityEngine;

namespace UTJ.Alembic
{
    [System.Serializable]
    public class AlembicStreamSettings
    {
        [SerializeField] public AbcAPI.aiNormalsMode normals = AbcAPI.aiNormalsMode.ComputeIfMissing;
        [SerializeField] public AbcAPI.aiTangentsMode tangents = AbcAPI.aiTangentsMode.Compute;
        [SerializeField] public AbcAPI.aiAspectRatioMode cameraAspectRatio = AbcAPI.aiAspectRatioMode.CurrentResolution;
        [SerializeField] public bool swapHandedness = true;
        [SerializeField] public bool swapFaceWinding = false;
        [SerializeField] public bool turnQuadEdges = false;
        [SerializeField] public bool cacheSamples = false;
        [SerializeField] public bool use32BitsIndexBuffer = false;

        public AlembicStreamSettings()
        {
#if UNITY_2017_3_OR_NEWER
            use32BitsIndexBuffer = true;
#endif
        }
    }
}