using System;
using System.IO;

namespace UnityEngine.Formats.Alembic.Importer
{
    [Serializable]
    class EmbeddedAlembicStreamDescriptor : IStreamDescriptor
    {
        [SerializeField] string pathToAbc = string.Empty;
        [SerializeField] AlembicStreamSettings settings = new AlembicStreamSettings();
        [SerializeField] float mediaStartTime;
        [SerializeField] float mediaEndTime;

        public string PathToAbc
        {
            get
            {
                {
#if UNITY_EDITOR
                    return pathToAbc;
#else
                    if (!System.IO.Path.IsPathRooted(pathToAbc) && !string.IsNullOrEmpty(pathToAbc))
                        return System.IO.Path.Combine(Application.streamingAssetsPath, pathToAbc);
                    return pathToAbc;
#endif
                }
            }
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
            var copier = ScriptableObject.CreateInstance<AlembicStreamSettings.AlembicStreamSettingsCopier>();
            copier.abcSettings = Settings;
            return new EmbeddedAlembicStreamDescriptor
            {
                pathToAbc = PathToAbc,
                settings = Object.Instantiate(copier).abcSettings,
                mediaStartTime = MediaStartTime,
                mediaEndTime = MediaEndTime,
            };
        }
    }
}
