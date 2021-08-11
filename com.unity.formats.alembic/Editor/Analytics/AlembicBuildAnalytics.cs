using System;

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

            EditorAnalytics.RegisterEventWithLimit(EventName, MAXEventsPerHour, MAXNumberOfElements, VendorKey);

            var data = CreateEvent(target);
            EditorAnalytics.SendEventWithLimit(EventName, data);
        }

        static AlembicBuildAnalyticsEvent CreateEvent(BuildTarget target)
        {
            var evt = new AlembicBuildAnalyticsEvent
            {
                target_platform = target.ToString()
            };


            return evt;
        }

        [Serializable]
        struct AlembicBuildAnalyticsEvent
        {
            public string target_platform;
        }
    }
}
