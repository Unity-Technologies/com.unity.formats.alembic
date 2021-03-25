namespace UnityEngine.Formats.Alembic.Importer
{
    interface IStreamDescriptor
    {
        string PathToAbc
        {
            get;
            set;
        }
        AlembicStreamSettings Settings
        {
            get;
            set;
        }

        float MediaStartTime
        {
            get;
            set;
        }

        float MediaEndTime
        {
            get;
            set;
        }

        float MediaDuration { get; }

        IStreamDescriptor Clone();
    }
}
