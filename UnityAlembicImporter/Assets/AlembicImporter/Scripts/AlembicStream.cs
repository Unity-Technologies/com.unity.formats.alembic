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
    public string m_path_to_abc;
    public float m_time;
    public float m_timescale = 1.0f;
    public bool m_reverse_x;
    public bool m_reverse_faces;
    bool m_loaded;
    float m_time_prev;
    IntPtr m_abc;


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

    void Update()
    {
        if (!m_loaded)
        {
            m_loaded = AlembicImporter.aiLoad(m_abc, Application.streamingAssetsPath + "/" + m_path_to_abc);
        }
        if(m_loaded)
        {
            m_time += Time.deltaTime * m_timescale;
            if (m_time_prev != m_time)
            {
                m_time_prev = m_time;
                AlembicImporter.UpdateAbcTree(m_abc, GetComponent<Transform>(), m_reverse_x, m_reverse_faces, m_time);
            }
        }
    }
}
