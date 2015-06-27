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
public abstract class AlembicElement : MonoBehaviour
{
    public AlembicStream m_abcstream;
    public AbcAPI.aiObject m_abcobj;
    public AbcAPI.aiSchema m_abcschema;
    public GCHandle m_hthis;


    public T GetOrAddComponent<T>() where T : Component
    {
        var c = gameObject.GetComponent<T>();
        if (c == null)
        {
            c = gameObject.AddComponent<T>();
        }
        return c;
    }

    public virtual void OnDestroy()
    {
        m_hthis.Free();
    }

    public virtual void AbcSetup(
        AlembicStream abcstream,
        AbcAPI.aiObject abcobj,
        AbcAPI.aiSchema abcschema)
    {
        m_abcstream = abcstream;
        m_abcobj = abcobj;
        m_abcschema = abcschema;
        m_hthis = GCHandle.Alloc(this);
        AbcAPI.aiSchemaSetCallback(abcschema, Callback, GCHandle.ToIntPtr(m_hthis));
    }

    static void Callback(IntPtr __this, AbcAPI.aiSample sample)
    {
        var _this = GCHandle.FromIntPtr(__this).Target as AlembicElement;
        _this.AbcOnUpdateSample(sample);
    }

    // * called by loading thread (not main thread) * 
    public abstract void AbcOnUpdateSample(AbcAPI.aiSample sample);

    public abstract void AbcUpdate();

    public AbcAPI.aiSample getSample()
    {
        return getSample(m_abcstream.time);
    }

    public AbcAPI.aiSample getSample(float time)
    {
        return AbcAPI.aiSchemaGetSample(m_abcschema, time);
    }
}
