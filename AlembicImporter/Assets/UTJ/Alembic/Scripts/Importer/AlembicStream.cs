using System;
using System.Collections.Generic;
using UnityEngine;

namespace UTJ.Alembic
{
    public class AlembicStream : IDisposable
    {
        private AlembicTreeNode _alembicTreeRoot;

        public bool streamInterupted = false;

        public AlembicImportSettings ImportSettings { get; set; }

        public AlembicPlaybackSettings m_playbackSettings;
        public AlembicDiagnosticSettings m_diagSettings;

        private float m_time;
        private AbcAPI.aiConfig m_config;
        private bool m_loaded;
        private AlembicImportSettings m_LastImportSettings = new AlembicImportSettings();
        private float m_lastAbcTime;
        private float m_lastAspectRatio = -1.0f;

        private AbcAPI.aiContext m_abc;
        public bool m_forceRefresh;


        public AlembicTreeNode AlembicTreeRoot { get { return _alembicTreeRoot; } }

        static List<AlembicStream> s_Streams = new List<AlembicStream>();
        public static void DisconnectStreamsWithPath(string path)
        {
            var fullPath = Application.streamingAssetsPath + path;
            AbcAPI.clearContextsWithPath(fullPath);
            s_Streams.ForEach(s => {
                if (s.ImportSettings.m_pathToAbc.GetFullPath() == fullPath)
                {
                    s.streamInterupted = true;
                    s.m_abc = default(AbcAPI.aiContext);
                    s.m_loaded = false;   
                }
            });
        } 

        public static void RemapStreamsWithPath(string oldPath , string newPath)
        {
            var fullOldPath = Application.streamingAssetsPath + oldPath;
            var fullNewPath = Application.streamingAssetsPath + newPath;
            s_Streams.ForEach(s =>
            {
                if (s.ImportSettings.m_pathToAbc.GetFullPath() == fullOldPath)
                {
                    s.streamInterupted = true;
                    s.ImportSettings.m_pathToAbc = new DataPath(fullNewPath);
                }
            } );
        } 

        public static void ReconnectStreamsWithPath(string path)
        {
            var fullPath = Application.streamingAssetsPath + path;
            s_Streams.ForEach(s =>
            {
                if (s.ImportSettings.m_pathToAbc.GetFullPath() == fullPath)
                    s.streamInterupted = false;
            } );
        } 
        
        // --- For internal use ---

        public bool AbcIsValid()
        {
            return (m_abc.ptr != (IntPtr)0);
        }

        private void AbcSyncConfig()
        {
            m_config.swapHandedness = ImportSettings.m_swapHandedness;
            m_config.shareVertices = ImportSettings.m_shareVertices;
            m_config.swapFaceWinding = ImportSettings.m_swapFaceWinding;
            m_config.normalsMode = ImportSettings.m_normalsMode;
            m_config.tangentsMode = ImportSettings.m_tangentsMode;
            m_config.cacheTangentsSplits = true;
            m_config.aspectRatio = AbcAPI.GetAspectRatio(ImportSettings.m_aspectRatioMode);
            m_config.forceUpdate = false; 
            m_config.cacheSamples = ImportSettings.m_cacheSamples;
            m_config.treatVertexExtraDataAsStatics = ImportSettings.m_treatVertexExtraDataAsStatics;
            m_config.interpolateSamples = m_playbackSettings.m_InterpolateSamples;
            m_config.turnQuadEdges = ImportSettings.m_TurnQuadEdges;
            m_config.vertexMotionScale = m_playbackSettings.m_vertexMotionScale;

            if (AbcIsValid())
            {
                AbcAPI.aiSetConfig(m_abc, ref m_config);
            }
        }

        private float AbcTime(float inTime)
        {
            float duration = m_playbackSettings.m_endTime - m_playbackSettings.m_startTime;
            if (duration == 0.0f)
            {
                return 0;
            }

            float outTime = inTime;
            
            if (m_playbackSettings.m_cycle == AlembicPlaybackSettings.CycleType.Hold)
            {
                if (outTime < 0)
                    outTime = 0;
                else if (outTime > duration)
                    outTime = duration;
            }
            else if (m_playbackSettings.m_cycle == AlembicPlaybackSettings.CycleType.Reverse)
            {
                if (outTime < 0)
                    outTime = duration;
                else if (outTime >= duration)
                    outTime = 0;
                else
                    outTime = duration - outTime % duration;
            }
            else if (m_playbackSettings.m_cycle == AlembicPlaybackSettings.CycleType.Loop)
            {
                outTime = outTime == duration ? duration : outTime % duration;
            }
            else if (m_playbackSettings.m_cycle == AlembicPlaybackSettings.CycleType.Bounce)
            {
                bool isReversed = ((int)(outTime / duration)) % 2 == 1;
                outTime = isReversed ? duration - outTime % duration : outTime % duration;
            }

            return outTime + m_playbackSettings.m_startTime;
        }

        private bool AbcUpdateRequired(float abcTime, float aspectRatio)
        {
            if (m_forceRefresh ||
                ImportSettings.m_swapHandedness != m_LastImportSettings.m_swapHandedness ||
                ImportSettings.m_swapFaceWinding != m_LastImportSettings.m_swapFaceWinding ||
                ImportSettings.m_normalsMode != m_LastImportSettings.m_normalsMode ||
                ImportSettings.m_tangentsMode != m_LastImportSettings.m_tangentsMode ||
                Math.Abs(abcTime - m_lastAbcTime) > 0 ||
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
            m_LastImportSettings.m_normalsMode = ImportSettings.m_normalsMode;
            m_LastImportSettings.m_tangentsMode = ImportSettings.m_tangentsMode;
            m_LastImportSettings.m_pathToAbc = ImportSettings.m_pathToAbc;
        }

        public void AbcUpdateConfigElements(AlembicTreeNode node = null)
        {
            if (node == null)
                node = _alembicTreeRoot;
            var o = node.alembicObjects.GetEnumerator();
            while (o.MoveNext())
            {
                o.Current.Value.AbcUpdateConfig();
            }

            var c = node.children.GetEnumerator();
            while (c.MoveNext())
            {
                AbcUpdateConfigElements(c.Current);
            }
        }

        public void AbcUpdateElements( AlembicTreeNode node = null )
        {
            if (node == null)
                node = _alembicTreeRoot;
            var o = node.alembicObjects.GetEnumerator();
            while (o.MoveNext())
            {
                o.Current.Value.AbcUpdate();
            }

            var c = node.children.GetEnumerator();
            while (c.MoveNext())
            {
                AbcUpdateElements(c.Current);
            }
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


        public bool AbcRecoverContext(GameObject go)
        {
            if (m_diagSettings.m_verbose)
            {
                Debug.Log("AlembicStream.AbcRecoverContext: Try to recover alembic context");
            }
            if (go != _alembicTreeRoot.linkedGameObj)
            {
                _alembicTreeRoot = new AlembicTreeNode() { stream = this, linkedGameObj = go };
            }
            m_abc = AbcAPI.aiCreateContext(_alembicTreeRoot.linkedGameObj.GetInstanceID());
            AbcSyncConfig();
            m_loaded = AbcAPI.aiLoad(m_abc, ImportSettings.m_pathToAbc.GetFullPath());
            if (AbcIsValid())
            {
                m_forceRefresh = true;
                _alembicTreeRoot.ResetTree();

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

        public bool AbcUpdate(float time)
        {
            if (ImportSettings == null)
                return true;

            if (streamInterupted)
            {
                return true;
            }
            if (!AbcIsValid() || (!m_loaded && ImportSettings != null && ImportSettings.m_pathToAbc != null))
            {
                return false;
                // We have lost the alembic context, try to recover it
            }
            else 
            {
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
                   
                    AbcAPI.aiUpdateSamples(m_abc, abcTime);
                    AbcUpdateElements();
                    
                    AbcSetLastUpdateState(abcTime, aspectRatio);
                }
                return true;
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
            AbcSyncConfig();
            m_loaded = AbcAPI.aiLoad(m_abc,ImportSettings.m_pathToAbc.GetFullPath());

            if (m_loaded)
            {
                m_forceRefresh = true;
                AbcAPI.UpdateAbcTree(m_abc, _alembicTreeRoot, AbcTime(m_time), createMissingNodes);
                AlembicStream.s_Streams.Add(this);
            }
            else
            {
                Debug.LogError("failed to load alembic: " + ImportSettings.m_pathToAbc.GetFullPath());
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

        public void Dispose()
        {
            AlembicStream.s_Streams.Remove(this);
            if (_alembicTreeRoot != null)
            {
                _alembicTreeRoot.Dispose();
                _alembicTreeRoot = null;
            }

            if (AbcIsValid())
            {
                AbcAPI.aiDestroyContext(m_abc);
                m_abc = default(AbcAPI.aiContext);
            }
        }

        // return false if context needs to be recovered

        public void ForcedRefresh()
        {
            m_forceRefresh = true;
            AbcUpdateConfigElements();
            AbcUpdateElements();
        }
    }
}
