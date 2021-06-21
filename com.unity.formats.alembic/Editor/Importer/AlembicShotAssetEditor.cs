using System;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.Formats.Alembic.Timeline;
using UnityEngine.Timeline;

namespace UnityEditor.Formats.Alembic.Importer
{
    [CustomTimelineEditor(typeof(AlembicShotAsset))]
    class AlembicShotAssetEditor : ClipEditor
    {
        public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            if (TimelineEditor.inspectedDirector == null || TimelineEditor.inspectedAsset == null || clonedFrom != null)
            {
                return;
            }

            var asset = clip.asset as AlembicShotAsset;
            var asp = TimelineEditor.inspectedDirector.GetReferenceValue(asset.StreamPlayer.exposedName,
                out var valid) as AlembicStreamPlayer;
            if (asp == null || !valid)
            {
                return;
            }

            var duration =
                Math.Max(asp.Duration, 1 / TimelineEditor.inspectedAsset.editorSettings.frameRate); // at least 1 frame
            clip.duration = duration;
        }
    }
}
