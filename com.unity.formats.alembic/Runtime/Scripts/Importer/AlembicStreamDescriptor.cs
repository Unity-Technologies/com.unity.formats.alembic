namespace UnityEngine.Formats.Alembic.Importer
{
    class AlembicStreamDescriptor : ScriptableObject, IStreamDescriptor
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
            set { pathToAbc = value; }
        }

        [SerializeField]
        AlembicStreamSettings settings = new AlembicStreamSettings();
        public AlembicStreamSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        [SerializeField] float abcStartTime = float.MinValue;
        public float MediaStartTime
        {
            get => abcStartTime;
            set => abcStartTime = value;
        }

        [SerializeField]
        float abcEndTime = float.MaxValue;
        public float MediaEndTime
        {
            get => abcEndTime;
            set => abcEndTime = value;
        }
        public float MediaDuration => abcEndTime - abcStartTime;
        public IStreamDescriptor Clone()
        {
            return Instantiate(this);
        }
    }
}
