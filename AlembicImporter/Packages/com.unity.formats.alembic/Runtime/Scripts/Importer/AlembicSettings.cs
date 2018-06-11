using UnityEngine;

namespace UTJ.Alembic
{
    [System.Serializable]
    public class AlembicStreamSettings
    {
        [SerializeField] public aiNormalsMode normals = aiNormalsMode.ComputeIfMissing;
        [SerializeField] public aiTangentsMode tangents = aiTangentsMode.Compute;
        [SerializeField] public aiAspectRatioMode cameraAspectRatio = aiAspectRatioMode.CameraAperture;
        [SerializeField] public float scaleFactor = 0.01f;
        [SerializeField] public bool swapHandedness = true;
        [SerializeField] public bool swapFaceWinding = false;
        [SerializeField] public bool turnQuadEdges = false;
        [SerializeField] public bool interpolateSamples = true;

        [SerializeField] public bool importPointPolygon = true;
        [SerializeField] public bool importLinePolygon = true;
        [SerializeField] public bool importTrianglePolygon = true;

        [SerializeField] public bool importXform = true;
        [SerializeField] public bool importCamera = true;
        [SerializeField] public bool importPolyMesh = true;
        [SerializeField] public bool importPoints = true;
    }
}