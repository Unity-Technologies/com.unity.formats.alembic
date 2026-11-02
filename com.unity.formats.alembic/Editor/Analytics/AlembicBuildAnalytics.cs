//#define DEBUG_ANALYTICS
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Formats.Alembic.UnitTests.Editor")]

namespace UnityEditor.Formats.Alembic.Importer
{
    static class AlembicBuildAnalytics
    {
        const string VendorKey = "unity.alembic";
        const string EventName = "alembic_build";
        const int MAXEventsPerHour = 1000;
        const int MAXNumberOfElements = 1000;

        internal static void SendAnalytics(BuildTarget target)
        {
            if (!EditorAnalytics.enabled)
                return;

            EditorAnalytics.SendAnalytic(new BuildEventAnalytic(CreateEvent(target)));
#if DEBUG_ANALYTICS
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            Debug.Log($"{EventName}->{json}");
#endif
        }

        internal static AlembicBuildAnalyticsEvent CreateEvent(BuildTarget target)
        {
            var evt = new AlembicBuildAnalyticsEvent
            {
                target_platform = target.ToString()
            };

            return evt;
        }

        [UnityEngine.Analytics.AnalyticInfo(eventName: EventName, vendorKey: VendorKey, maxEventsPerHour: MAXEventsPerHour, maxNumberOfElements: MAXNumberOfElements)]
        class BuildEventAnalytic : UnityEngine.Analytics.IAnalytic
        {
            private AlembicBuildAnalyticsEvent? data = null;

            public BuildEventAnalytic(AlembicBuildAnalyticsEvent data)
            {
                this.data = data;
            }

            public bool TryGatherData(out UnityEngine.Analytics.IAnalytic.IData data, [NotNullWhen(false)] out Exception error)
            {
                error = null;
                data = this.data;
                return data != null;
            }
        }

        [Serializable]
        internal struct AlembicBuildAnalyticsEvent : UnityEngine.Analytics.IAnalytic.IData
        {
            public string target_platform;
        }
    }
}
