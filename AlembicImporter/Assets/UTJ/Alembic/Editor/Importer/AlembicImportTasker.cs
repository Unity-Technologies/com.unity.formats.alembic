using System;
using UnityEngine;

namespace UTJ.Alembic
{
    class AlembicImportTasker
    {
        internal static GameObject Import(AlembicImportMode importMode, AlembicImportSettings importSettings, AlembicDiagnosticSettings diagSettings, Action<AlembicStream, GameObject, AlembicStreamDescriptor> customAction)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(importSettings.m_pathToAbc.leaf);
            var go = new GameObject(fileName);
            go.transform.localScale *= importSettings.m_scaleFactor;

            using (var abcStream = new AlembicStream(go, importSettings, new AlembicPlaybackSettings(), diagSettings))
            {
                abcStream.AbcLoad(true);

                importSettings.m_minTime = abcStream.AbcStartTime;
                importSettings.m_maxTime = abcStream.AbcEndTime;
                
                importSettings.m_startTime = importSettings.m_startTime ==-1.0f ? abcStream.AbcStartTime : importSettings.m_startTime;
                importSettings.m_endTime = importSettings.m_endTime ==-1.0f ? abcStream.AbcEndTime : importSettings.m_endTime;

                AlembicStreamDescriptor streamDescr = ScriptableObject.CreateInstance<AlembicStreamDescriptor>();
                streamDescr.name = "AlembicStream: " + go.name;
                streamDescr.m_ImportSettings = importSettings;
                streamDescr.m_importMode = importMode;
                
                if (importMode == AlembicImportMode.AutomaticStreamingSetup)
                {
                    var dynStream = go.AddComponent<AlembicStreamPlayer>();
                    dynStream.m_PlaybackSettings = new AlembicPlaybackSettings();
                    dynStream.m_PlaybackSettings.m_startTime = importSettings.m_startTime;
                    dynStream.m_PlaybackSettings.m_endTime = importSettings.m_endTime;
                    dynStream.m_StreamDescriptor = streamDescr;
                    dynStream.enabled = true;
                }

                customAction.Invoke(abcStream, go, streamDescr);
            }

            return go;
        }
    }
}
