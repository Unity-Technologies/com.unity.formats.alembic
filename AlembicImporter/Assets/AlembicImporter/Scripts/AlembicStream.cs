using System;
using System.Collections;
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

    public string m_path_to_abc;
    public float m_time;
    public float m_start_time = 0.0f;
    public float m_end_time = 0.0f;
    public float m_time_offset = 0.0f;
    public float m_time_scale = 1.0f;
    public bool m_preserve_start_time = true;
    public CycleType m_cycle = CycleType.Hold;
    public bool m_reverse_x;
    public bool m_reverse_faces;
    public bool m_force_refresh;
    public bool m_ignore_missing_nodes = true;
    
    bool m_loaded;
    float m_adjusted_time_prev;
    bool m_reverse_x_prev;
    bool m_reverse_faces_prev;
    float m_time_eps = 0.001f;
    AlembicImporter.aiContext m_abc;
    bool m_ignore_missing_nodes_prev;


    void OnEnable()
    {
#if UNITY_STANDALONE_WIN
        AlembicImporter.AddLibraryPath();
#endif
        m_abc = AlembicImporter.aiCreateContext();
        m_loaded = AlembicImporter.aiLoad(m_abc, m_path_to_abc);
    }

    void OnDisable()
    {
        AlembicImporter.aiDestroyContext(m_abc);
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
        m_time = 0.0f;

        m_adjusted_time_prev = AdjustTime(0.0f);
        m_reverse_x_prev = m_reverse_x;
        m_reverse_faces_prev = m_reverse_faces;
        m_ignore_missing_nodes_prev = m_ignore_missing_nodes;
        m_force_refresh = false;
    }

    void UpdateAbc(float time)
    {
        if (!m_loaded)
        {
            m_loaded = AlembicImporter.aiLoad(m_abc, Application.streamingAssetsPath + "/" + m_path_to_abc);
        }

        if (m_loaded)
        {
            m_time = time;

            float adjusted_time = AdjustTime(m_time);

            if (m_force_refresh || 
                m_reverse_x != m_reverse_x_prev ||
                m_reverse_faces != m_reverse_faces_prev ||
                m_ignore_missing_nodes != m_ignore_missing_nodes_prev ||
                Math.Abs(adjusted_time - m_adjusted_time_prev) > m_time_eps)
            {
                AlembicImporter.UpdateAbcTree(m_abc, GetComponent<Transform>(), m_reverse_x, m_reverse_faces, adjusted_time, m_ignore_missing_nodes);
                
                m_adjusted_time_prev = adjusted_time;
                m_reverse_x_prev = m_reverse_x;
                m_reverse_faces_prev = m_reverse_faces;
                m_force_refresh = false;
            }
        }
    }

    void Update()
    {
        UpdateAbc(m_time + Time.deltaTime);
    }
}
