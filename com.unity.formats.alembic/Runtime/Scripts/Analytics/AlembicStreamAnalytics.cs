#define DEBUG_ANALYTICS
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
            var data = new AlembicChangeStreamEvent();
            EditorAnalytics.SendEventWithLimit(EventName, data);
#if DEBUG_ANALYTICS
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            Debug.Log(json);
#endif
#endif
        }

        [Serializable]
        struct AlembicChangeStreamEvent
        {
        }
    }
}
