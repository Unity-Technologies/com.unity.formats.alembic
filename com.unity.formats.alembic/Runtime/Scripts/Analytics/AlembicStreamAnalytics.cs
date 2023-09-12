//#define DEBUG_ANALYTICS
using System;
using System.Diagnostics.CodeAnalysis;
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
#if UNITY_EDITOR && ENABLE_CLOUD_SERVICES_ANALYTICS
            if (!EditorAnalytics.enabled)
                return;
#if UNITY_2023_2_OR_NEWER
            var data = new ChangeStreamEventAnalytic(new AlembicChangeStreamEvent());
            EditorAnalytics.SendAnalytic(data);
#else
            EditorAnalytics.RegisterEventWithLimit(EventName, MAXEventsPerHour, MAXNumberOfElements, VendorKey);
            var data = new AlembicChangeStreamEvent();
            EditorAnalytics.SendEventWithLimit(EventName, data);
#endif
#if DEBUG_ANALYTICS
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            Debug.Log($"{EventName}->{json}");
#endif
#endif
        }
#if UNITY_2023_2_OR_NEWER
        [Analytics.AnalyticInfo(eventName: EventName, vendorKey: VendorKey, maxEventsPerHour: MAXEventsPerHour, maxNumberOfElements: MAXNumberOfElements)]
        class ChangeStreamEventAnalytic : Analytics.IAnalytic
        {
            private AlembicChangeStreamEvent? data = null;

            public ChangeStreamEventAnalytic(AlembicChangeStreamEvent data)
            {
                this.data = data;
            }

            public bool TryGatherData(out Analytics.IAnalytic.IData data, [NotNullWhen(false)] out Exception error)
            {
                error = null;
                data = this.data;
                return data != null;
            }
        }

        [Serializable]
        struct AlembicChangeStreamEvent : Analytics.IAnalytic.IData
        {
        }
#else
        [Serializable]
        struct AlembicChangeStreamEvent
        {
        }
#endif
    }
}
