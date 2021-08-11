using System;
using System.Collections.Generic;
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
        const string EventName = "alembic_exporter_start";
        const int MAXEventsPerHour = 1000;
        const int MAXNumberOfElements = 1000;

        internal static void SendAnalytics(AlembicRecorderSettings settings)
        {
#if UNITY_EDITOR
            if (!EditorAnalytics.enabled)
                return;

            EditorAnalytics.RegisterEventWithLimit(EventName, MAXEventsPerHour, MAXNumberOfElements, VendorKey);

            var data = CreateEvent(settings);
            EditorAnalytics.SendEventWithLimit(EventName, data);
#endif
        }

        static AlembicExporterAnalyticsEvent CreateEvent(AlembicRecorderSettings settings)
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

        static IEnumerable<T> GetTargets<T>(this AlembicRecorderSettings settings) where T : Component
        {
            if (settings.Scope == ExportScope.TargetBranch)
            {
                return settings.TargetBranch != null ? settings.TargetBranch.GetComponentsInChildren<T>() : Enumerable.Empty<T>();
            }

            return Object.FindObjectsOfType<T>();
        }

        [Serializable]
        struct AlembicExporterAnalyticsEvent
        {
            public bool capture_mesh, skinned_mesh, camera, static_mesh_renderers;
        }
    }
}
