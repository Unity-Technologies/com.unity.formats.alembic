namespace UnityEngine.Formats.Alembic.Importer
{
    public class AlembicStreamDescriptor : ScriptableObject
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
            get { return settings; }
            set { settings = value; }
        }

        [SerializeField]
        internal double abcStartTime = double.MinValue;

        [SerializeField]
        internal double abcEndTime = double.MaxValue;

        public double mediaDuration { get { return abcEndTime - abcStartTime; } }
    }
}
