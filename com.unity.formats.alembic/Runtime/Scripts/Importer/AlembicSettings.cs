using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    [System.Serializable]
    public class AlembicStreamSettings
    {
        [SerializeField]
        private NormalsMode normals = NormalsMode.CalculateIfMissing;
        public NormalsMode Normals
        {
            get { return normals; }
            set { normals = value; }
        }

        [SerializeField]
        private TangentsMode tangents = TangentsMode.Calculate;
        public TangentsMode Tangents
        {
            get { return tangents; }
            set { tangents = value; }
        }

        [SerializeField]
        private AspectRatioMode cameraAspectRatio = AspectRatioMode.CameraAperture;
        public AspectRatioMode CameraAspectRatio
        {
            get { return cameraAspectRatio; }
            set { cameraAspectRatio = value; }
        }

        [SerializeField]
        private bool importVisibility = true;
        public bool ImportVisibility
        {
            get { return importVisibility; }
            set { importVisibility = value; }
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
        private bool flipFaces = false;
        public bool FlipFaces
        {
            get { return flipFaces; }
            set { flipFaces = value; }
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
        internal bool ImportPointPolygon // Broken
        {
            get { return importPointPolygon; }
            set { importPointPolygon = value; }
        }

        [SerializeField]
        private bool importLinePolygon = true;
        internal bool ImportLinePolygon // Broken
        {
            get { return importLinePolygon; }
            set { importLinePolygon = value; }
        }

        [SerializeField]
        private bool importTrianglePolygon = true;
        internal bool ImportTrianglePolygon // Broken
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
        private bool importCameras = true;
        public bool ImportCameras
        {
            get { return importCameras; }
            set { importCameras = value; }
        }

        [SerializeField]
        private bool importMeshes = true;
        public bool ImportMeshes
        {
            get { return importMeshes; }
            set { importMeshes = value; }
        }

        [SerializeField]
        private bool importPoints = false;
        public bool ImportPoints
        {
            get { return importPoints; }
            set { importPoints = value; }
        }
    }
}
