namespace UnityEngine.Formats.Alembic.Importer
{
    /// <summary>
    /// A class that that stores information about an Alembic stream.
    /// </summary>
    public class AlembicStreamDescriptor : ScriptableObject
    {
        [SerializeField]
        string pathToAbc;
        /// <summary>
        /// The path to the Alembic asset. When in a standalone build, the returned path is prepended by the streamingAssets path.
        /// </summary>
        public string PathToAbc
        {
            // For standalone builds, the path should be relative to the StreamingAssets
            get
            {
#if UNITY_EDITOR
                return pathToAbc;
#else
                return System.IO.Path.Combine(Application.streamingAssetsPath, pathToAbc);
#endif
            }
            set { pathToAbc = value; }
        }

        [SerializeField]
        AlembicStreamSettings settings = new AlembicStreamSettings();
        /// <summary>
        /// The stream import options.
        /// </summary>
        public AlembicStreamSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        [SerializeField] float abcStartTime = float.MinValue;
        /// <summary>
        /// The start timestamp of the Alembic file.
        /// </summary>
        public float mediaStartTime
        {
            get => abcStartTime;
            internal set => abcStartTime = value;
        }

        [SerializeField]
        float abcEndTime = float.MaxValue;

        /// <summary>
        /// The end timestamp of the Alembic file.
        /// </summary>
        public float mediaEndTime
        {
            get => abcEndTime;
            internal set => abcEndTime = value;
        }

        /// <summary>
        /// The duration of the Alembic file.
        /// </summary>
        public float mediaDuration => abcEndTime - abcStartTime;
    }
}
