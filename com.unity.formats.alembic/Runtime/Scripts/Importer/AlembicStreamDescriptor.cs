namespace UnityEngine.Formats.Alembic.Importer
{
    class AlembicStreamDescriptor : ScriptableObject
    {
        [SerializeField]
        string pathToAbc;
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
            internal set { pathToAbc = value; }
        }

        [SerializeField]
        AlembicStreamSettings settings = new AlembicStreamSettings();
        public AlembicStreamSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        [SerializeField] float abcStartTime = float.MinValue;
        public float mediaStartTime
        {
            get => abcStartTime;
            internal set => abcStartTime = value;
        }

        [SerializeField]
        float abcEndTime = float.MaxValue;
        public float mediaEndTime
        {
            get => abcEndTime;
            internal set => abcEndTime = value;
        }
        public float mediaDuration => abcEndTime - abcStartTime;
    }
}
