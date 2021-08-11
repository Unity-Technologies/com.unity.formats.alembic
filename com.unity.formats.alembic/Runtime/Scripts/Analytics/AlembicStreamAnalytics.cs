using System;
using UnityEditor;

namespace UnityEngine.Formats.Alembic.Importer
{
    static class AlembicStreamAnalytics
    {
        const string VendorKey = "unity.alembic";
        const string EventName = "alembic_change_stream";
        const int MAXEventsPerHour = 1000;
        const int MAXNumberOfElements = 1000;

        internal static void SendAnalytics()
        {
#if UNITY_EDITOR
            if (!EditorAnalytics.enabled)
                return;

            EditorAnalytics.RegisterEventWithLimit(EventName, MAXEventsPerHour, MAXNumberOfElements, VendorKey);
            EditorAnalytics.SendEventWithLimit(EventName, new AlembicChangeStreamEvent());
#endif
        }

        [Serializable]
        struct AlembicChangeStreamEvent
        {
        }
    }
}
