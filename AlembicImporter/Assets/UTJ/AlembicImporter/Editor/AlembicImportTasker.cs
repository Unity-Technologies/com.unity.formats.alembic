using System;
using UnityEngine;

namespace UTJ.Alembic
{
	class AlembicImportTasker
	{
		internal static GameObject Import(AlembicImportMode importMode, AlembicImportSettings importSettings, AlembicDiagnosticSettings diagSettings, Action<AlembicStream, GameObject, AlembicStreamDescriptor> customAction)
		{
			var fileName = System.IO.Path.GetFileNameWithoutExtension(importSettings.m_pathToAbc);
			var go = new GameObject(fileName);

			using (var abcStream = new AlembicStream(go, importSettings, new AlembicPlaybackSettings(), diagSettings))
			{
				abcStream.AbcLoad(true);

				AlembicStreamDescriptor streamDescr = null;
				if (importMode > AlembicImportMode.NoSupportForStreaming)
				{
					streamDescr = ScriptableObject.CreateInstance<AlembicStreamDescriptor>();
					streamDescr.name = "AlembicStream: " + go.name;
					streamDescr.m_ImportSettings = importSettings;
					streamDescr.m_importMode = importMode;
				}

				if (importMode == AlembicImportMode.AutomaticStreamingSetup)
				{
					var dynStream = go.AddComponent<AlembicStreamPlayer>();
					dynStream.m_PlaybackSettings = new AlembicPlaybackSettings()
					{
						m_startTime = abcStream.AbcStartTime,
						m_endTime = abcStream.AbcEndTime,
                        m_duration = abcStream.AbcEndTime
					};
					dynStream.m_StreamDescriptor = streamDescr;
					dynStream.enabled = true;
				}

				customAction.Invoke(abcStream, go, streamDescr);
			}

			return go;
		}
	}
}
