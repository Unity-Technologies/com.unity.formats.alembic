using UnityEngine;

namespace UTJ.Alembic
{
    [System.Serializable]
    public class AlembicStreamSettings
    {
        [SerializeField]
        private aiNormalsMode normals = aiNormalsMode.ComputeIfMissing;
        public aiNormalsMode Normals
        {
            get { return normals; }
            set { normals = value; }
        }

        [SerializeField]
        private aiTangentsMode tangents = aiTangentsMode.Compute;
        public aiTangentsMode Tangents
        {
            get { return tangents; }
            set { tangents = value; }
        }

        [SerializeField]
        private aiAspectRatioMode cameraAspectRatio = aiAspectRatioMode.CameraAperture;
        public aiAspectRatioMode CameraAspectRatio
        {
            get { return cameraAspectRatio; }
            set { cameraAspectRatio = value; }
        }

        [SerializeField]
        private float scaleFactor = 0.01f;
        public float ScaleFactor
        {
            get { return scaleFactor; }
            set { scaleFactor = value; }
        }

        [SerializeField]
        private bool swapHandedness = true;
        public bool SwapHandedness
        {
            get { return swapHandedness; }
            set { swapHandedness = value; }
        }

        [SerializeField]
        private bool swapFaceWinding = false;
        public bool SwapFaceWinding
        {
            get { return swapFaceWinding; }
            set { swapFaceWinding = value; }
        }

        [SerializeField]
        private bool turnQuadEdges = false;
        public bool TurnQuadEdges
        {
            get { return turnQuadEdges; }
            set { turnQuadEdges = value; }
        }

        [SerializeField]
        private bool interpolateSamples = true;
        public bool InterpolateSamples
        {
            get { return interpolateSamples; }
            set { interpolateSamples = value; }
        }

        [SerializeField]
        private bool importPointPolygon = true;
        public bool ImportPointPolygon
        {
            get { return importPointPolygon; }
            set { importPointPolygon = value; }
        }

        [SerializeField]
        private bool importLinePolygon = true;
        public bool ImportLinePolygon
        {
            get { return importLinePolygon; }
            set { importLinePolygon = value; }
        }

        [SerializeField]
        private bool importTrianglePolygon = true;
        public bool ImportTrianglePolygon
        {
            get { return importTrianglePolygon; }
            set { importTrianglePolygon = value; }
        }

        [SerializeField]
        private bool importXform = true;
        public bool ImportXform
        {
            get { return importXform; }
            set { importXform = value; }
        }

        [SerializeField]
        private bool importCamera = true;
        public bool ImportCamera
        {
            get { return importCamera; }
            set { importCamera = value; }
        }

        [SerializeField]
        private bool importPolyMesh = true;
        public bool ImportPolyMesh
        {
            get { return importPolyMesh; }
            set { importPolyMesh = value; }
        }

        [SerializeField]
        private bool importPoints = true;
        public bool ImportPoints
        {
            get { return importPoints; }
            set { importPoints = value; }
        }
    }
}