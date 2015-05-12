using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


public class AlembicStream : MonoBehaviour
{
    public string m_path_to_abc;
    public float m_time;
    public float m_timescale = 1.0f;
    IntPtr m_abc;


    void OnEnable()
    {
        m_abc = AlembicImporter.aiCreateContext();
        AlembicImporter.aiLoad(m_abc, m_path_to_abc);
    }

    void OnDisable()
    {
        AlembicImporter.aiDestroyContext(m_abc);
    }

    void Update()
    {
        m_time += Time.deltaTime * m_timescale;
        AlembicImporter.UpdateAbcTree(m_abc, GetComponent<Transform>(), m_time);
    }
}
