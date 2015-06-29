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
    public enum MeshDataType { Mesh, Texture }
    public enum CycleType { Hold, Loop, Reverse, Bounce }

    public MeshDataType m_data_type;
    public string m_path_to_abc;
    public float m_time;
    public float m_start_time;
    public float m_end_time;
    public float m_time_offset;
    public float m_time_interval = 1.0f / 30.0f;
    public float m_time_scale = 1.0f;
    public bool m_preserve_start_time = true;
    public CycleType m_cycle = CycleType.Hold;
    public bool m_revert_x = true;
    public bool m_revert_faces = false;
    bool m_load_succeeded;
    float m_time_prev;
    float m_time_next;
    float m_time_eps = 0.001f;

    public List<AlembicElement> m_elements = new List<AlembicElement>();

    AbcAPI.aiContext m_abc;
    Transform m_trans;


    public float time { get { return m_time; } }
    public float time_prev { get { return m_time_prev; } }
    public float time_next { get { return m_time_next; } }

    public void AddElement(AlembicElement e) { m_elements.Add(e); }

    public void DebugDump() { AbcAPI.aiDebugDump(m_abc); }



    public void AbcLoad()
    {
        if (m_path_to_abc == null) { return; }
        Debug.Log(m_path_to_abc);

        m_trans = GetComponent<Transform>();
        m_abc = AbcAPI.aiCreateContext();
        m_load_succeeded = AbcAPI.aiLoad(m_abc, Application.streamingAssetsPath + "/" + m_path_to_abc);

        if (m_load_succeeded)
        {
            m_start_time = AbcAPI.aiGetStartTime(m_abc);
            m_end_time = AbcAPI.aiGetEndTime(m_abc);
            m_time_offset = -m_start_time;
            m_time_scale = 1.0f;
            m_preserve_start_time = true;

            AbcAPI.aiImportConfig ic;
            ic.triangulate = 1;
            ic.revert_x = (byte)(m_revert_x ? 1 : 0);
            ic.revert_faces = (byte)(m_revert_faces ? 1 : 0);
            AbcAPI.aiSetImportConfig(m_abc, ref ic);
            AbcAPI.UpdateAbcTree(m_abc, m_trans, m_revert_x, m_revert_faces, m_time);
        }
    }

    void AbcUpdateElements()
    {
        int len = m_elements.Count;
        for (int i = 0; i < len; ++i)
        {
            m_elements[i].AbcUpdate();
        }
    }


    void OnApplicationQuit()
    {
        AbcAPI.aiCleanup();
    }

    public void Awake()
    {
        AbcLoad();
    }

    void OnDestroy()
    {
        AbcAPI.aiDestroyContext(m_abc);
        m_abc = default(AbcAPI.aiContext);
    }

    float AdjustTime(float in_time)
    {
        float extra_offset = 0.0f;

        // compute extra time offset to counter-balance effect of m_time_scale on m_start_time
        if (m_preserve_start_time)
        {
            extra_offset = m_start_time * (m_time_scale - 1.0f);
        }

        float play_time = m_end_time - m_start_time;

        // apply speed and offset
        float out_time = m_time_scale * (in_time - m_time_offset) - extra_offset;

        if (m_cycle == CycleType.Hold)
        {
            if (out_time < (m_start_time - m_time_eps))
            {
                out_time = m_start_time;
            }
            else if (out_time > (m_end_time + m_time_eps))
            {
                out_time = m_end_time;
            }
        }
        else
        {
            float normalized_time = (out_time - m_start_time) / play_time;
            float play_repeat = (float)Math.Floor(normalized_time);
            float fraction = Math.Abs(normalized_time - play_repeat);
            
            if (m_cycle == CycleType.Reverse)
            {
                if (out_time > (m_start_time + m_time_eps) && out_time < (m_end_time - m_time_eps))
                {
                    // inside alembic sample range
                    out_time = m_end_time - fraction * play_time;
                }
                else if (out_time < (m_start_time + m_time_eps))
                {
                    out_time = m_end_time;
                }
                else
                {
                    out_time = m_start_time;
                }
            }
            else
            {
                if (out_time < (m_start_time - m_time_eps) || out_time > (m_end_time + m_time_eps))
                {
                    // outside alembic sample range
                    if (m_cycle == CycleType.Loop || ((int)play_repeat % 2) == 0)
                    {
                        out_time = m_start_time + fraction * play_time;
                    }
                    else
                    {
                        out_time = m_end_time - fraction * play_time;
                    }
                }
            }
        }

        return out_time;
    }

    void Start()
    {
        m_time_prev = m_time_next = m_time;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        m_time_next = m_time;
    }
#endif

    void Update()
    {
        if (!m_load_succeeded) { return; }

        bool needs_update_sample = false;
        m_time += Time.deltaTime;
        if (Math.Abs(m_time - m_time_prev) > m_time_interval)
        {
            needs_update_sample = true;
            m_time_prev = m_time_next;
            m_time_next = m_time_prev + m_time_interval;
        }

        if (needs_update_sample)
        {
            // end preload
            AbcAPI.aiUpdateSamplesEnd(m_abc);

            AbcUpdateElements();

            AbcAPI.aiSetTimeRangeToKeepSamples(m_abc, m_time_prev, 0.1f);

            // begin preload
            AbcAPI.aiUpdateSamplesBegin(m_abc, m_time_next);
        }
    }
}
