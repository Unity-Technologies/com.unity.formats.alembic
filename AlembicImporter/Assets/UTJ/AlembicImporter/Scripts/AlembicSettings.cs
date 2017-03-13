using UnityEngine;

namespace UTJ.Alembic
{

	public enum AlembicImportMode
	{
		NoSupportForStreaming,
		ManualStreamingSetup,
		AutomaticStreamingSetup
	}

	[System.Serializable]
	public class AlembicDiagnosticSettings
	{
		[SerializeField] public bool m_verbose;
		[SerializeField] public bool m_logToFile;
		[SerializeField] public string m_logPath;
		
	}

	[System.Serializable]
	public class AlembicImportSettings
	{
		[SerializeField][HideInInspector] public string m_pathToAbc;

		[Tooltip("Should 'handedness' be swapped?")]
		[SerializeField] public bool m_swapHandedness = true;
		[SerializeField] public bool m_swapFaceWinding = false;
		[SerializeField] public bool m_submeshPerUVTile = true;
		[SerializeField] public bool m_importMeshes = true;
		[SerializeField] public AbcAPI.aiNormalsMode m_normalsMode = AbcAPI.aiNormalsMode.ComputeIfMissing;
		[SerializeField] public AbcAPI.aiTangentsMode m_tangentsMode = AbcAPI.aiTangentsMode.None;
		[SerializeField] public AbcAPI.aiAspectRatioMode m_aspectRatioMode = AbcAPI.aiAspectRatioMode.CurrentResolution;

		[Header("Advanced")]
		[SerializeField] public bool m_useThreads = false;
		[SerializeField] public int m_sampleCacheSize = 0;

	}

	[System.Serializable]
	public class AlembicPlaybackSettings
	{
		public enum CycleType
		{
			Hold,
			Loop,
			Reverse,
			Bounce
		};
        
		[HideInInspector][SerializeField] public float m_duration = 0.0f;

		[Tooltip("Specifies the lower time bound to use in the stream")]
		[SerializeField] public float m_startTime = 0.0f;

		[Tooltip("Specifies the upper time bound to use in the stream")]
		[SerializeField] public float m_endTime = 0.0f;

		[Tooltip("Offset, inside the specified time bounds, of where to playback starts.")]
		[SerializeField] public float m_timeOffset = 0.0f;

		[Tooltip("Use to compress/dilute time play back")]
		[SerializeField] public float m_timeScale = 1.0f;

		[Tooltip("Should 'start time' be exempted from 'time scale'")]
		public bool m_preserveStartTime = true;

		[Tooltip("Controls how playback cycles throught the stream.")]
		[SerializeField] public CycleType m_cycle = CycleType.Hold;

	    [SerializeField] public float m_Time = 0f;
	    [SerializeField] public bool m_OverrideTime = false;
	}
}

