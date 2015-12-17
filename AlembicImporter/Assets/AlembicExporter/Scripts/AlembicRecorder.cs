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
class AlembicRecorder : MonoBehaviour
{
    public string m_path;
    public aeAPI.aeConfig m_conf;

    aeAPI.aeContext m_ctx;
    bool m_recording;


    public bool BeginRecording()
    {
        if(m_recording) { return true; }

        m_ctx = aeAPI.aeCreateContext(ref m_conf);
        if(m_ctx.ptr == IntPtr.Zero) {
            Debug.Log("aeCreateContext() failed");
            return false;
        }
        if(!aeAPI.aeOpenArchive(m_ctx, m_path))
        {
            Debug.Log("aeOpenArchive() failed");
            aeAPI.aeDestroyContext(m_ctx);
            m_ctx = new aeAPI.aeContext();
            return false;
        }

        m_recording = true;
        return true;
    }

    public void EndRecording()
    {
        if (!m_recording) { return; }
        aeAPI.aeDestroyContext(m_ctx);
        m_ctx = new aeAPI.aeContext();
    }


    IEnumerator UpdateRecording()
    {
        yield return new WaitForEndOfFrame();

        var top = aeAPI.aeGetTopObject(m_ctx);
        // todo
    }


    void Update()
    {
        if(m_recording)
        {
            StartCoroutine(UpdateRecording());
        }
    }
}
