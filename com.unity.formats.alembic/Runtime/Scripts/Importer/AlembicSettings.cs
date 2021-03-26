using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    /// <summary>
    /// This class contains stream reading options for Alembic file import.
    /// </summary>
    [System.Serializable]
    public class AlembicStreamSettings
    {
        [SerializeField]
        NormalsMode normals = NormalsMode.CalculateIfMissing;
        /// <summary>
        /// Get or set the Normals computation mode on import.
        /// </summary>
        public NormalsMode Normals
        {
            get { return normals; }
            set { normals = value; }
        }

        [SerializeField]
        TangentsMode tangents = TangentsMode.Calculate;
        /// <summary>
        /// Get or set the Tangents computation mode on import.
        /// </summary>
        public TangentsMode Tangents
        {
            get { return tangents; }
            set { tangents = value; }
        }

        [SerializeField]
        AspectRatioMode cameraAspectRatio = AspectRatioMode.CameraAperture;
        /// <summary>
        /// Get or set the Camera aspect ratio on import.
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
        /// The world scale factor to convert between the Alembic file and Unity.
        /// </summary>
        public float ScaleFactor
        {
            get { return scaleFactor; }
            set { scaleFactor = value; }
        }

        [SerializeField]
        bool swapHandedness = true;
        /// <summary>
        /// Enable to switch the X-axis direction between Left and Right handed coordinate systems.
        /// </summary>
        public bool SwapHandedness
        {
            get { return swapHandedness; }
            set { swapHandedness = value; }
        }

        [SerializeField]
        bool flipFaces = false;
        /// <summary>
        /// Enable to invert the orientation of the polygons.
        /// </summary>
        public bool FlipFaces
        {
            get { return flipFaces; }
            set { flipFaces = value; }
        }

        [SerializeField]
        bool interpolateSamples = true;
        /// <summary>
        /// Enable to linearly interpolate between Alembic samples for which the topology does not change.
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
        /// Enable or disable the import of Transform (Xform) data.
        /// </summary>
        public bool ImportXform
        {
            get { return importXform; }
            set { importXform = value; }
        }

        [SerializeField]
        bool importCameras = true;
        /// <summary>
        /// Enable or disable the import of Camera data.
        /// </summary>
        public bool ImportCameras
        {
            get { return importCameras; }
            set { importCameras = value; }
        }

        [SerializeField]
        bool importMeshes = true;
        /// <summary>
        /// Enable or disable the import of Mesh data.
        /// </summary>
        public bool ImportMeshes
        {
            get { return importMeshes; }
            set { importMeshes = value; }
        }

        [SerializeField]
        bool importPoints = false;
        /// <summary>
        /// Enable or disable the import of Point (particle cloud) data.
        /// </summary>
        public bool ImportPoints
        {
            get { return importPoints; }
            set { importPoints = value; }
        }

        [SerializeField]
        bool importCurves = true;
        /// <summary>
        /// Enable or disable the import of Curve data.
        /// </summary>
        public bool ImportCurves
        {
            get { return importCurves; }
            set { importCurves = value; }
        }

        [SerializeField] bool createCurveRenderers;
        /// <summary>
        /// If you enabled the importing of Alembic curves, this method automatically creates the AlembicCurveRendering component.
        /// </summary>
        public bool CreateCurveRenderers
        {
            get => createCurveRenderers;
            set => createCurveRenderers = value;
        }

        internal class AlembicStreamSettingsCopier : ScriptableObject
        {
            public AlembicStreamSettings abcSettings;
        }
    }
}
