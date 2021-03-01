using System;

namespace UnityEngine.Formats.Alembic.Importer
{
    [Serializable]
    class SceneStreamDescriptor : IStreamDescriptor
    {
        [SerializeField] string pathToAbc;
        [SerializeField] AlembicStreamSettings settings = new AlembicStreamSettings();
        [SerializeField] float mediaStartTime;
        [SerializeField] float mediaEndTime;

        public string PathToAbc
        {
            get => pathToAbc;
            set => pathToAbc = value;
        }

        public AlembicStreamSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        public float MediaStartTime
        {
            get => mediaStartTime;
            set => mediaStartTime = value;
        }

        public float MediaEndTime
        {
            get => mediaEndTime;
            set => mediaEndTime = value;
        }

        public float MediaDuration => MediaEndTime - MediaStartTime;

        public IStreamDescriptor Clone()
        {
            var copier = new AlembicStreamSettings.AlembicStreamSettingsCopier {abcSettings = Settings};
            return new SceneStreamDescriptor
            {
                pathToAbc = PathToAbc,
                settings = Object.Instantiate(copier).abcSettings,
                mediaStartTime = MediaStartTime,
                mediaEndTime = MediaEndTime,
            };
        }
    }
}
