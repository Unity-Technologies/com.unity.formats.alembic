using UnityEngine;

namespace UTJ.Alembic
{
    [System.Serializable]
    public class AlembicStreamSettings
    {
        [SerializeField] public AbcAPI.aiNormalsMode normalsMode = AbcAPI.aiNormalsMode.ComputeIfMissing;
        [SerializeField] public AbcAPI.aiTangentsMode tangentsMode = AbcAPI.aiTangentsMode.Smooth;
        [SerializeField] public AbcAPI.aiAspectRatioMode aspectRatioMode = AbcAPI.aiAspectRatioMode.CurrentResolution;
        [SerializeField] public bool swapHandedness = true;
        [SerializeField] public bool swapFaceWinding = false;
        [SerializeField] public bool turnQuadEdges = false;
        [SerializeField] public bool shareVertices = true;
        [SerializeField] public bool treatVertexExtraDataAsStatics = false;
        [SerializeField] public bool cacheSamples = false;
        [SerializeField] public bool use32BitsIndexBuffer = false;
    }
}