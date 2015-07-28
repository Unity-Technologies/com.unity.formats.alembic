using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class AlembicStream : MonoBehaviour
{
    public enum CycleType { Hold, Loop, Reverse, Bounce };

    public string m_pathToAbc;
    public float m_time;
    public float m_startTime = 0.0f;
    public float m_endTime = 0.0f;
    public float m_timeOffset = 0.0f;
    public float m_timeScale = 1.0f;
    public bool m_preserveStartTime = true;
    public CycleType m_cycle = CycleType.Hold;
    public bool m_swapHandedness;
    public bool m_swapFaceWinding;
    public AbcAPI.aiNormalsMode m_normalsMode = AbcAPI.aiNormalsMode.ComputeIfMissing;
    public AbcAPI.aiTangentsMode m_tangentsMode = AbcAPI.aiTangentsMode.None;
    public AbcAPI.aiAspectRatioMode m_aspectRatioMode = AbcAPI.aiAspectRatioMode.CurrentResolution;
    public bool m_forceRefresh;
    public bool m_verbose = false;
    public bool m_logToFile = false;
    public string m_logPath = "";
    [HideInInspector] public HashSet<AlembicElement> m_elements = new HashSet<AlembicElement>();
    [HideInInspector] public AbcAPI.aiConfig m_config;

    bool m_loaded;
    float m_lastAbcTime;
    bool m_lastSwapHandedness;
    bool m_lastSwapFaceWinding;
    AbcAPI.aiNormalsMode m_lastNormalsMode;
    AbcAPI.aiTangentsMode m_lastTangentsMode;
    bool m_lastIgnoreMissingNodes;
    float m_lastAspectRatio = -1.0f;
    bool m_lastLogToFile = false;
    string m_lastLogPath = "";
    float m_timeEps = 0.001f;
    AbcAPI.aiContext m_abc;
    Transform m_trans;
    // keep a list of aiObjects to update?

    // --- For internal use ---

    bool AbcIsValid()
    {
        return (m_abc.ptr != (IntPtr)0);
    }

    void AbcSyncConfig()
    {
        m_config.swapHandedness = m_swapHandedness;
        m_config.swapFaceWinding = m_swapFaceWinding;
        m_config.normalsMode = m_normalsMode;
        m_config.tangentsMode = m_tangentsMode;
        m_config.cacheTangentsSplits = true;
        m_config.aspectRatio = AbcAPI.GetAspectRatio(m_aspectRatioMode);
        m_config.forceUpdate = false; // m_forceRefresh; ?

        if (AbcIsValid())
        {
            AbcAPI.aiSetConfig(m_abc, ref m_config);
        }
    }

    float AbcTime(float inTime)
    {
        float extraOffset = 0.0f;

        // compute extra time offset to counter-balance effect of m_timeScale on m_startTime
        if (m_preserveStartTime)
        {
            extraOffset = m_startTime * (m_timeScale - 1.0f);
        }

        float playTime = m_endTime - m_startTime;

        // apply speed and offset
        float outTime = m_timeScale * (inTime - m_timeOffset) - extraOffset;

        if (m_cycle == CycleType.Hold)
        {
            if (outTime < (m_startTime - m_timeEps))
            {
                outTime = m_startTime;
            }
            else if (outTime > (m_endTime + m_timeEps))
            {
                outTime = m_endTime;
            }
        }
        else
        {
            float normalizedTime = (outTime - m_startTime) / playTime;
            float playRepeat = (float)Math.Floor(normalizedTime);
            float fraction = Math.Abs(normalizedTime - playRepeat);
            
            if (m_cycle == CycleType.Reverse)
            {
                if (outTime > (m_startTime + m_timeEps) && outTime < (m_endTime - m_timeEps))
                {
                    // inside alembic sample range
                    outTime = m_endTime - fraction * playTime;
                }
                else if (outTime < (m_startTime + m_timeEps))
                {
                    outTime = m_endTime;
                }
                else
                {
                    outTime = m_startTime;
                }
            }
            else
            {
                if (outTime < (m_startTime - m_timeEps) || outTime > (m_endTime + m_timeEps))
                {
                    // outside alembic sample range
                    if (m_cycle == CycleType.Loop || ((int)playRepeat % 2) == 0)
                    {
                        outTime = m_startTime + fraction * playTime;
                    }
                    else
                    {
                        outTime = m_endTime - fraction * playTime;
                    }
                }
            }
        }

        return outTime;
    }

    bool AbcUpdateRequired(float abcTime, float aspectRatio)
    {
        if (m_forceRefresh || 
            m_swapHandedness != m_lastSwapHandedness ||
            m_swapFaceWinding != m_lastSwapFaceWinding ||
            m_normalsMode != m_lastNormalsMode ||
            m_tangentsMode != m_lastTangentsMode ||
            Math.Abs(abcTime - m_lastAbcTime) > m_timeEps ||
            aspectRatio != m_lastAspectRatio)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void AbcSetLastUpdateState(float abcTime, float aspectRatio)
    {
        m_lastAbcTime = abcTime;
        m_lastSwapHandedness = m_swapHandedness;
        m_lastSwapFaceWinding = m_swapFaceWinding;
        m_lastNormalsMode = m_normalsMode;
        m_lastTangentsMode = m_tangentsMode;
        m_lastAspectRatio = aspectRatio;
        m_forceRefresh = false;
    }

    void AbcUpdateElements()
    {
        if (m_verbose)
        {
            Debug.Log("AlembicStream.AbcUpdateElement: " + m_elements.Count + " element(s).");
        }

        foreach (AlembicElement e in m_elements)
        {
            if (e != null)
            {
                e.AbcUpdate();
            }
        }
    }

    void AbcDetachElements()
    {
        if (m_verbose)
        {
            Debug.Log("AlembicStream.AbcDetachElement: " + m_elements.Count + " element(s).");
        }

        foreach (AlembicElement e in m_elements)
        {
            if (e != null)
            {
                e.m_abcStream = null;
            }
        }
    }

    void AbcUpdate(float time)
    {
        if (m_lastLogToFile != m_logToFile ||
            m_lastLogPath != m_logPath)
        {
            AbcAPI.aiEnableFileLog(m_logToFile, m_logPath);

            m_lastLogToFile = m_logToFile;
            m_lastLogPath = m_logPath;
        }
        
        if (m_loaded)
        {
            m_time = time;

            float abcTime = AbcTime(m_time);
            float aspectRatio = AbcAPI.GetAspectRatio(m_aspectRatioMode);

            if (AbcUpdateRequired(abcTime, aspectRatio))
            {
                if (m_verbose)
                {
                    Debug.Log("AlembicSctream.AbcUpdate: t=" + m_time + " (t'=" + abcTime + ")");
                }
                
                AbcSyncConfig();

                // This should ensure only keep one sample,
                // unless you take 1,000,000 samples / sec (>30,000 samples per frame at 30 fps)
                AbcAPI.aiSetTimeRangeToKeepSamples(m_abc, m_time, 0.000001f);

                AbcAPI.aiUpdateSamples(m_abc, m_time, false); // single threaded for first tests

                AbcUpdateElements();
                
                AbcSetLastUpdateState(abcTime, aspectRatio);
            }
        }
    }

    // --- public api ---
    
    public void AbcAddElement(AlembicElement e)
    {
        if (e != null)
        {
            if (m_verbose)
            {
                Debug.Log("AlembicStream.AbcAddElement: \"" + e.gameObject.name + "\"");
            }
            m_elements.Add(e);
        }
    }

    public void AbcRemoveElement(AlembicElement e)
    {
        if (e != null)
        {
            if (m_verbose)
            {
                Debug.Log("AlembicStream.AbcRemoveElement: \"" + e.gameObject.name + "\"");
            }
            AbcAPI.aiDestroyObject(m_abc, e.m_abcObj);
            m_elements.Remove(e);
        }
    }

    public void AbcLoad(bool createMissingNodes=false)
    {
        if (m_pathToAbc == null)
        {
            return;
        }

        m_trans = GetComponent<Transform>();
        m_abc = AbcAPI.aiCreateContext(gameObject.GetInstanceID());
        m_loaded = AbcAPI.aiLoad(m_abc, Application.streamingAssetsPath + "/" + m_pathToAbc);

        if (m_loaded)
        {
            m_startTime = AbcAPI.aiGetStartTime(m_abc);
            m_endTime = AbcAPI.aiGetEndTime(m_abc);
            m_timeOffset = -m_startTime;
            m_timeScale = 1.0f;
            m_preserveStartTime = true;

            AbcSyncConfig();

            AbcAPI.UpdateAbcTree(m_abc, m_trans, m_time, createMissingNodes);
        }
    }

    // --- method overrides ---

    void OnApplicationQuit()
    {
        AbcAPI.aiCleanup();
    }

    public void Awake()
    {
        AbcLoad();
        AbcAPI.aiEnableFileLog(m_logToFile, m_logPath);
    }

    void OnDestroy()
    {
        if (!AbcIsValid())
        {
            Debug.Log("AlembicStream: Nothing to destroy");
            return;
        }

        if (!Application.isPlaying)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                AbcDetachElements();
                AbcAPI.aiDestroyContext(m_abc);
                m_abc = default(AbcAPI.aiContext);
            }
#else
            AbcDetachElements();
            AbcAPI.aiDestroyContext(m_abc);
            m_abc = default(AbcAPI.aiContext);
#endif
        }
    }

    void Start()
    {
        m_time = 0.0f;

        AbcSetLastUpdateState(AbcTime(0.0f), AbcAPI.GetAspectRatio(m_aspectRatioMode));
        m_forceRefresh = true;
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            AbcUpdate(Time.time);
        }
        else
        {
            AbcUpdate(m_time);
        }
    }
}
