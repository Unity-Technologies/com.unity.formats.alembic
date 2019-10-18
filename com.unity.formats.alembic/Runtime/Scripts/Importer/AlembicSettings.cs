using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    /// <summary>
    /// This class contains stream reading options.
    /// </summary>
    [System.Serializable]
    public class AlembicStreamSettings
    {
        [SerializeField]
        NormalsMode normals = NormalsMode.CalculateIfMissing;
        /// <summary>
        /// Normal computation options.
        /// </summary>
        public NormalsMode Normals
        {
            get { return normals; }
            set { normals = value; }
        }

        [SerializeField]
        TangentsMode tangents = TangentsMode.Calculate;
        /// <summary>
        /// Tangent computation options.
        /// </summary>
        public TangentsMode Tangents
        {
            get { return tangents; }
            set { tangents = value; }
        }

        [SerializeField]
        AspectRatioMode cameraAspectRatio = AspectRatioMode.CameraAperture;
        /// <summary>
        /// Camera aspect ratio import options.
        /// </summary>
        internal AspectRatioMode CameraAspectRatio // Broken/Unimplemented , not connected to any code path.
        {
            get { return cameraAspectRatio; }
            set { cameraAspectRatio = value; }
        }

        [SerializeField]
        bool importVisibility = true;
        /// <summary>
        /// Enables or disables the control of the active state of objects.
        /// </summary>
        public bool ImportVisibility
        {
            get { return importVisibility; }
            set { importVisibility = value; }
        }

        [SerializeField]
        float scaleFactor = 0.01f;
        /// <summary>
        /// The world scale factor conversion between the Alembic file and unity.
        /// </summary>
        public float ScaleFactor
        {
            get { return scaleFactor; }
            set { scaleFactor = value; }
        }

        [SerializeField]
        bool swapHandedness = true;
        /// <summary>
        /// Switch the X-axis direction to convert between Left and Right handed coordinate systems.
        /// </summary>
        public bool SwapHandedness
        {
            get { return swapHandedness; }
            set { swapHandedness = value; }
        }

        [SerializeField]
        bool flipFaces = false;
        /// <summary>
        /// Invert the orientations of the polygons.
        /// </summary>
        public bool FlipFaces
        {
            get { return flipFaces; }
            set { flipFaces = value; }
        }

        [SerializeField]
        bool interpolateSamples = true;
        /// <summary>
        /// Linearly interpolate between Alembic samples for which the topology does not change.
        /// </summary>
        public bool InterpolateSamples
        {
            get { return interpolateSamples; }
            set { interpolateSamples = value; }
        }

        [SerializeField]
        bool importPointPolygon = true;
        internal bool ImportPointPolygon // Broken/Unimplemented , not connected to any code path.
        {
            get { return importPointPolygon; }
            set { importPointPolygon = value; }
        }

        [SerializeField]
        bool importLinePolygon = true;
        internal bool ImportLinePolygon // Broken/Unimplemented , not connected to any code path.
        {
            get { return importLinePolygon; }
            set { importLinePolygon = value; }
        }

        [SerializeField]
        bool importTrianglePolygon = true;
        internal bool ImportTrianglePolygon // Broken/Unimplemented , not connected to any code path.
        {
            get { return importTrianglePolygon; }
            set { importTrianglePolygon = value; }
        }

        [SerializeField]
        bool importXform = true;
        /// <summary>
        /// Enables or disable the import of transforms.
        /// </summary>
        public bool ImportXform
        {
            get { return importXform; }
            set { importXform = value; }
        }

        [SerializeField]
        bool importCameras = true;
        /// <summary>
        /// Enables or disable the import of cameras.
        /// </summary>
        public bool ImportCameras
        {
            get { return importCameras; }
            set { importCameras = value; }
        }

        [SerializeField]
        bool importMeshes = true;
        /// <summary>
        /// Enables or disable the import of meshes.
        /// </summary>
        public bool ImportMeshes
        {
            get { return importMeshes; }
            set { importMeshes = value; }
        }

        [SerializeField]
        bool importPoints = false;
        /// <summary>
        /// Enables or disable the import of points.
        /// </summary>
        public bool ImportPoints
        {
            get { return importPoints; }
            set { importPoints = value; }
        }
    }
}
