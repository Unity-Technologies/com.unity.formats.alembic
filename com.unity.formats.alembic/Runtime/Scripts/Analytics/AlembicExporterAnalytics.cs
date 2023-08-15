//#define DEBUG_ANALYTICS
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Formats.Alembic.Util;

namespace UnityEngine.Formats.Alembic.Exporter
{
    static class AlembicExporterAnalytics
    {
        const string VendorKey = "unity.alembic";
        const string EventName = "alembic_exporter";
        const int MAXEventsPerHour = 1000;
        const int MAXNumberOfElements = 1000;

        internal static void SendAnalytics(AlembicRecorderSettings settings)
        {
#if UNITY_EDITOR && ENABLE_CLOUD_SERVICES_ANALYTICS
            if (!EditorAnalytics.enabled)
                return;
#if UNITY_2023_2_OR_NEWER
            EditorAnalytics.SendAnalytic(new ExporterEventAnalytic(CreateEvent(settings)));
#else
            EditorAnalytics.RegisterEventWithLimit(EventName, MAXEventsPerHour, MAXNumberOfElements, VendorKey);

            var data = CreateEvent(settings);
            EditorAnalytics.SendEventWithLimit(EventName, data);
#endif
#if DEBUG_ANALYTICS
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            Debug.Log($"{EventName}->{json}");
#endif
#endif
        }

        static IEnumerable<T> GetTargets<T>(this AlembicRecorderSettings settings) where T : Component
        {
            if (settings.Scope == ExportScope.TargetBranch)
            {
                return settings.TargetBranch != null ? settings.TargetBranch.GetComponentsInChildren<T>() : Enumerable.Empty<T>();
            }
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsSortMode.InstanceID);
#else
            return Object.FindObjectsOfType<T>();
#endif
        }

        internal static AlembicExporterAnalyticsEvent CreateEvent(AlembicRecorderSettings settings)
        {
            var evt = new AlembicExporterAnalyticsEvent
            {
                capture_mesh = settings.CaptureMeshRenderer && settings.GetTargets<MeshFilter>().Any(),
                skinned_mesh = settings.CaptureSkinnedMeshRenderer && settings.GetTargets<SkinnedMeshRenderer>().Any(),
                camera = settings.CaptureCamera && settings.GetTargets<Camera>().Any(),
                static_mesh_renderers = settings.AssumeNonSkinnedMeshesAreConstant
            };


            return evt;
        }

#if UNITY_2023_2_OR_NEWER
        [Analytics.AnalyticInfo(eventName: EventName, vendorKey: VendorKey, maxEventsPerHour: MAXEventsPerHour, maxNumberOfElements: MAXNumberOfElements)]
        class ExporterEventAnalytic : Analytics.IAnalytic
        {
            private AlembicExporterAnalyticsEvent? data = null;

            public ExporterEventAnalytic(AlembicExporterAnalyticsEvent data)
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
        internal struct AlembicExporterAnalyticsEvent : Analytics.IAnalytic.IData
        {
            public bool capture_mesh, skinned_mesh, camera, static_mesh_renderers;
        }
#else


        [Serializable]
        internal struct AlembicExporterAnalyticsEvent
        {
            public bool capture_mesh, skinned_mesh, camera, static_mesh_renderers;
        }
#endif
    }
}
