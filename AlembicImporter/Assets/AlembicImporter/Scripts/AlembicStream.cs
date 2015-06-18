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
    public float m_time_scale = 1.0f;
    public bool m_preserve_start_time = true;
    public CycleType m_cycle = CycleType.Hold;
    public bool m_reverse_x;
    public bool m_reverse_faces;
    bool m_loaded;
    float m_time_prev;
    float m_time_eps = 0.001f;

    public List<AlembicElement> m_elements = new List<AlembicElement>();

    AlembicImporter.aiContext m_abc;
    Transform m_trans;
    Mesh m_index_mesh;


    public void AddElement(AlembicElement e) { m_elements.Add(e); }

    public void DebugDump() { AlembicImporter.aiDebugDump(m_abc); }

    public void Awake()
    {
        if (m_path_to_abc==null) { return; }
        Debug.Log(m_path_to_abc);
        if (m_data_type == MeshDataType.Texture)
        {
            m_index_mesh = AlembicUtils.CreateIndexOnlyMesh(64998);
        }

        m_trans = GetComponent<Transform>();
        m_abc = AlembicImporter.aiCreateContext();
        m_loaded = AlembicImporter.aiLoad(m_abc, Application.streamingAssetsPath + "/" + m_path_to_abc);

        m_start_time = AlembicImporter.aiGetStartTime(m_abc);
        m_end_time = AlembicImporter.aiGetEndTime(m_abc);
        m_time_offset = -m_start_time;
        m_time_scale = 1.0f;
        m_preserve_start_time = true;

        AlembicImporter.UpdateAbcTree(m_abc, m_trans, m_reverse_x, m_reverse_faces, m_time);
    }

    void OnDestroy()
    {
        m_index_mesh = null;
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
        m_time_prev = 0.0f;
        m_time = 0.0f;
    }

    void Update()
    {
        m_time += Time.deltaTime;
        if (Math.Abs(m_time - m_time_prev) > m_time_eps)
        {
            // update elements
            int len = m_elements.Count;
            for (int i = 0; i < len; ++i)
            {
                m_elements[i].m_time = m_time;
                m_elements[i].AbcUpdate();
            }

            m_time_prev = m_time;
        }
    }
}
