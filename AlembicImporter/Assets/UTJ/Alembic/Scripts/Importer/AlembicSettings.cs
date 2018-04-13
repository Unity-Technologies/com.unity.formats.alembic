using UnityEngine;

namespace UTJ.Alembic
{
    [System.Serializable]
    public class AlembicStreamSettings
    {
        [SerializeField] public aiNormalsMode normals = aiNormalsMode.CalculateIfMissing;
        [SerializeField] public aiTangentsMode tangents = aiTangentsMode.Calculate;
        [SerializeField] public aiAspectRatioMode cameraAspectRatio = aiAspectRatioMode.CameraAperture;
        [SerializeField] public float scaleFactor = 0.01f;
        [SerializeField] public bool swapHandedness = true;
        [SerializeField] public bool flipFaces = false;
        [SerializeField] public bool turnQuadEdges = false;
        [SerializeField] public bool interpolateSamples = true;

        [SerializeField] public bool importPointPolygon = true;
        [SerializeField] public bool importLinePolygon = true;
        [SerializeField] public bool importTrianglePolygon = true;

        [SerializeField] public bool importXform = true;
        [SerializeField] public bool importCameras = true;
        [SerializeField] public bool importMeshes = true;
        [SerializeField] public bool importPoints = true;

        [SerializeField] public bool importVisibility = true;
    }
}