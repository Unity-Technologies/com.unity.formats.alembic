using System;
using UnityEngine;

namespace UTJ.Alembic
{
    public class AlembicStream : AlembicDisposable
    {
        private AlembicTreeNode _alembicTreeRoot;


        public AlembicImportSettings ImportSettings { get; set; }


        public AlembicPlaybackSettings m_playbackSettings;
        public AlembicDiagnosticSettings m_diagSettings;

        private float m_time;
        private AbcAPI.aiConfig m_config;
        private bool m_loaded;
        private AlembicImportSettings m_LastImportSettings = new AlembicImportSettings();
        private float m_lastAbcTime;
        private float m_lastAspectRatio = -1.0f;

        private float m_timeEps = 0.001f;
        private AbcAPI.aiContext m_abc;
        private bool m_updateBegan = false;
        public bool m_forceRefresh;


        public AlembicTreeNode AlembicTreeRoot { get { return _alembicTreeRoot; } }



        // --- For internal use ---

        private bool AbcIsValid()
        {
            return (m_abc.ptr != (IntPtr)0);
        }
    
        private void AbcSyncConfig()
        {
            m_config.swapHandedness = ImportSettings.m_swapHandedness;
            m_config.swapFaceWinding = ImportSettings.m_swapFaceWinding;
            m_config.normalsMode = ImportSettings.m_normalsMode;
            m_config.tangentsMode = ImportSettings.m_tangentsMode;
            m_config.cacheTangentsSplits = true;
            m_config.aspectRatio = AbcAPI.GetAspectRatio(ImportSettings.m_aspectRatioMode);
            m_config.forceUpdate = false; 
            m_config.useThreads = ImportSettings.m_useThreads;
            m_config.cacheSamples = ImportSettings.m_sampleCacheSize;
            m_config.submeshPerUVTile = ImportSettings.m_submeshPerUVTile;
    
            if (AbcIsValid())
            {
                AbcAPI.aiSetConfig(m_abc, ref m_config);
            }
        }
    
        private float AbcTime(float inTime)
        {
            float extraOffset = 0.0f;
    
            // compute extra time offset to counter-balance effect of m_timeScale on m_startTime
            if (m_playbackSettings.m_preserveStartTime)
            {
                extraOffset = m_playbackSettings.m_startTime * (m_playbackSettings.m_timeScale - 1.0f);
            }
    
            float playTime = m_playbackSettings.m_endTime - m_playbackSettings.m_startTime;
    
            // apply speed and offset
            float outTime = m_playbackSettings.m_timeScale * (inTime - m_playbackSettings.m_timeOffset) - extraOffset;
    
            if (m_playbackSettings.m_cycle == AlembicPlaybackSettings.CycleType.Hold)
            {
                if (outTime < (m_playbackSettings.m_startTime - m_timeEps))
                {
                    outTime = m_playbackSettings.m_startTime;
                }
                else if (outTime > (m_playbackSettings.m_endTime + m_timeEps))
                {
                    outTime = m_playbackSettings.m_endTime;
                }
            }
            else
            {
                float normalizedTime = (outTime - m_playbackSettings.m_startTime) / playTime;
                float playRepeat = (float)Math.Floor(normalizedTime);
                float fraction = Math.Abs(normalizedTime - playRepeat);
                
                if (m_playbackSettings.m_cycle == AlembicPlaybackSettings.CycleType.Reverse)
                {
                    if (outTime > (m_playbackSettings.m_startTime + m_timeEps) && outTime < (m_playbackSettings.m_endTime - m_timeEps))
                    {
                        // inside alembic sample range
                        outTime = m_playbackSettings.m_endTime - fraction * playTime;
                    }
                    else if (outTime < (m_playbackSettings.m_startTime + m_timeEps))
                    {
                        outTime = m_playbackSettings.m_endTime;
                    }
                    else
                    {
                        outTime = m_playbackSettings.m_startTime;
                    }
                }
                else
                {
                    if (outTime < (m_playbackSettings.m_startTime - m_timeEps) || outTime > (m_playbackSettings.m_endTime + m_timeEps))
                    {
                        // outside alembic sample range
                        if (m_playbackSettings.m_cycle == AlembicPlaybackSettings.CycleType.Loop || ((int)playRepeat % 2) == 0)
                        {
                            outTime = m_playbackSettings.m_startTime + fraction * playTime;
                        }
                        else
                        {
                            outTime = m_playbackSettings.m_endTime - fraction * playTime;
                        }
                    }
                }
            }
    
            return outTime;
        }
    
        private bool AbcUpdateRequired(float abcTime, float aspectRatio)
        {
            if (m_forceRefresh ||
                ImportSettings.m_swapHandedness != m_LastImportSettings.m_swapHandedness ||
                ImportSettings.m_swapFaceWinding != m_LastImportSettings.m_swapFaceWinding ||
                ImportSettings.m_submeshPerUVTile != m_LastImportSettings.m_submeshPerUVTile ||
                ImportSettings.m_normalsMode != m_LastImportSettings.m_normalsMode ||
                ImportSettings.m_tangentsMode != m_LastImportSettings.m_tangentsMode ||
                Math.Abs(abcTime - m_lastAbcTime) > m_timeEps ||
                aspectRatio != m_lastAspectRatio ||
                ImportSettings.m_pathToAbc != m_LastImportSettings.m_pathToAbc)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    
        private void AbcSetLastUpdateState(float abcTime, float aspectRatio)
        {
            m_lastAbcTime = abcTime;
            m_lastAspectRatio = aspectRatio;
            m_forceRefresh = false;

            m_LastImportSettings.m_swapHandedness = ImportSettings.m_swapHandedness;
            m_LastImportSettings.m_swapFaceWinding = ImportSettings.m_swapFaceWinding;
            m_LastImportSettings.m_submeshPerUVTile = ImportSettings.m_submeshPerUVTile;
            m_LastImportSettings.m_normalsMode = ImportSettings.m_normalsMode;
            m_LastImportSettings.m_tangentsMode = ImportSettings.m_tangentsMode;
        }

        public void AbcUpdateConfigElements(AlembicTreeNode node = null)
        {
            if (node == null)
                node = _alembicTreeRoot;

            foreach (var o in node.alembicObjects)
                o.Value.AbcUpdateConfig();

            foreach (var c in node.children)
                AbcUpdateConfigElements(c);
        }

        public void AbcUpdateElements( AlembicTreeNode node = null )
        {
            if (node == null)
                node = _alembicTreeRoot;

            foreach (var o in node.alembicObjects)
                o.Value.AbcUpdate();

            foreach (var c in node.children)
                AbcUpdateElements( c );
        }


        public float AbcStartTime
        {
            get
            {
                if (AbcIsValid())
                    return AbcAPI.aiGetStartTime(m_abc);
                else
                    return 0;
            }
        }

        public float AbcEndTime
        {
            get
            {
                if (AbcIsValid())
                    return AbcAPI.aiGetEndTime(m_abc);
                else
                    return 0;
            }
        }


        private bool AbcRecoverContext()
        {
            if (!AbcIsValid())
            {
                if (m_diagSettings.m_verbose)
                {
                    Debug.Log("AlembicStream.AbcRecoverContext: Try to recover alembic context");
                }
                
                m_abc = AbcAPI.aiCreateContext(_alembicTreeRoot.linkedGameObj.GetInstanceID());
    
                if (AbcIsValid())
                {
                    m_forceRefresh = true;
                    _alembicTreeRoot.ResetTree();

                    AbcSyncConfig();
    
                    AbcAPI.UpdateAbcTree(m_abc, _alembicTreeRoot, AbcTime(m_time), false);
    
                    if (m_diagSettings.m_verbose)
                    {
                        Debug.Log("AlembicStream.AbcRecoverContext: Succeeded.");
                    }
    
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
    
        private void AbcUpdateBegin(float time)
        {
            if (ImportSettings == null)
                return;

            if (!m_loaded && ImportSettings != null && ImportSettings.m_pathToAbc != null)
            {
                // We have lost the alembic context, try to recover it
                m_loaded = AbcRecoverContext();
            }
    
            if (m_loaded)
            {
                if (!AbcIsValid())
                {
                    // We have lost the alembic context, try to recover
                    m_loaded = AbcRecoverContext();
                    if (!m_loaded)
                    {
                        Debug.LogWarning("AlembicStream.AbcUpdate: Lost alembic context");
                        
                        return;
                    }
                }
    
                m_time = time;
    
                float abcTime = AbcTime(m_time);
                float aspectRatio = AbcAPI.GetAspectRatio(ImportSettings.m_aspectRatioMode);
    
                if (AbcUpdateRequired(abcTime, aspectRatio))
                {
                    if (m_diagSettings.m_verbose)
                    {
                        Debug.Log("AlembicStream.AbcUpdate: t=" + m_time + " (t'=" + abcTime + ")");
                    }
                    
                    AbcSyncConfig();
                    AbcUpdateConfigElements();
    
                    if(ImportSettings.m_useThreads)
                    {
                        AbcAPI.aiUpdateSamplesBegin(m_abc, abcTime);
                        m_updateBegan = true;
                    }
                    else
                    {
                        AbcAPI.aiUpdateSamples(m_abc, abcTime);
                        AbcUpdateElements();
                    }
                    
                    AbcSetLastUpdateState(abcTime, aspectRatio);
                }
            }
        }
   
    
        // --- public api ---
   
        public void AbcLoad(bool createMissingNodes=false)
        {
            if ( m_playbackSettings == null || m_diagSettings == null || ImportSettings == null || ImportSettings.m_pathToAbc == null)
            {
                return;
            }

            m_time = 0.0f;
            m_forceRefresh = true;

            m_abc = AbcAPI.aiCreateContext(_alembicTreeRoot.linkedGameObj.GetInstanceID());
            m_loaded = AbcAPI.aiLoad(m_abc, ImportSettings.m_pathToAbc);

            if (m_loaded)
            {
                m_forceRefresh = true;
                AbcSyncConfig();
                AbcAPI.UpdateAbcTree(m_abc, _alembicTreeRoot, AbcTime(m_time), createMissingNodes);
            }
            else
            {
                Debug.LogError("failed to load alembic: " + ImportSettings.m_pathToAbc);
            }
            AbcSetLastUpdateState(AbcTime(0.0f), AbcAPI.GetAspectRatio(ImportSettings.m_aspectRatioMode));
        }

        public AlembicStream(GameObject rootGo, AlembicImportSettings importSettings, AlembicPlaybackSettings playSettings, AlembicDiagnosticSettings diagSettings)
        {
            _alembicTreeRoot = new AlembicTreeNode() { stream = this, linkedGameObj = rootGo };
            ImportSettings = importSettings;
            m_playbackSettings = playSettings;
            m_diagSettings = diagSettings ?? new AlembicDiagnosticSettings();

            AbcAPI.aiEnableFileLog(m_diagSettings.m_logToFile, m_diagSettings.m_logPath);
        }

        // --- method overrides ---

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_alembicTreeRoot != null)
                {
                    _alembicTreeRoot.Dispose();
                    _alembicTreeRoot = null;
                }
            }

            if (AbcIsValid())
            {
                if (m_updateBegan )
                    AbcAPI.aiUpdateSamplesEnd(m_abc);
                AbcAPI.aiDestroyContext(m_abc);
                m_abc = default(AbcAPI.aiContext);
            }

            base.Dispose(disposing);
        }

        public void ProcessUpdateEvent()
        {
            if (Application.isPlaying && !m_playbackSettings.m_OverrideTime)
            {
                AbcUpdateBegin(Time.time);
            }
            else
            {
                AbcUpdateBegin(m_playbackSettings.m_Time);
            }
        }
    
        public void ProcessLateUpdateEvent()
        {
            if (m_updateBegan && _alembicTreeRoot != null)
            {
                AbcAPI.aiUpdateSamplesEnd(m_abc);
                AbcUpdateElements();
            }
        }

        public void ForcedRefresh()
        {
            m_forceRefresh = true;
            AbcUpdateConfigElements();
            AbcUpdateElements();
        }
    }
}
