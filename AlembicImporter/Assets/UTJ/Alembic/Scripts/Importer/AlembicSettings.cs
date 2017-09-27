using UnityEngine;

namespace UTJ.Alembic
{

    public enum AlembicImportMode
    {
        NoSupportForStreaming,
        AutomaticStreamingSetup
    }

    [System.Serializable]
    public class AlembicDiagnosticSettings
    {
        [SerializeField] public bool m_verbose;
        [SerializeField] public bool m_logToFile;
        [SerializeField] public string m_logPath;
        
    }

    [System.Serializable]
    public class AlembicImportSettings
    {
        [SerializeField][ReadOnly] public DataPath m_pathToAbc = new DataPath();
        [SerializeField] public bool m_swapHandedness = true;
        [SerializeField] public bool m_swapFaceWinding = false;
        [SerializeField] public bool m_TurnQuadEdges = false;
        [SerializeField] public bool m_shareVertices = true;
        [SerializeField] public bool m_treatVertexExtraDataAsStatics = false;
        [SerializeField] public float m_scaleFactor = 0.01f;
        [SerializeField] public float m_minTime = 0.0f;
        [SerializeField] public float m_maxTime = 0.0f;
        [SerializeField] public float m_startTime = -1.0f;
        [SerializeField] public float m_endTime = -1.0f;
        [SerializeField] public AbcAPI.aiNormalsMode m_normalsMode = AbcAPI.aiNormalsMode.ComputeIfMissing;
        [SerializeField] public AbcAPI.aiTangentsMode m_tangentsMode = AbcAPI.aiTangentsMode.Smooth;
        [SerializeField] public AbcAPI.aiAspectRatioMode m_aspectRatioMode = AbcAPI.aiAspectRatioMode.CurrentResolution;
        [SerializeField] public bool m_cacheSamples = false;
    }

    [System.Serializable]
    public class AlembicPlaybackSettings
    {
        public enum CycleType
        {
            Hold,
            Reverse,
            Loop,
            Bounce,
        };

        [SerializeField] public float m_startTime = 0.0f;
        [SerializeField] public float m_endTime = 0.0f;
        [SerializeField] public float m_vertexMotionScale = 1.0f;
        [SerializeField] public CycleType m_cycle = CycleType.Loop;
        [SerializeField] public bool m_InterpolateSamples = true;
    }
}

