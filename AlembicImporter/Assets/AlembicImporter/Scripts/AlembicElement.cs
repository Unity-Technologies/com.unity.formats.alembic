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
public class AlembicElement : MonoBehaviour
{
    public AlembicStream m_abcstream;
    public AlembicImporter.aiObject m_abcobj;

    public T GetOrAddComponent<T>() where T : Component
    {
        var c = gameObject.GetComponent<T>();
        if (c == null)
        {
            c = gameObject.AddComponent<T>();
        }
        return c;
    }

    public virtual void AbcSetup(AlembicStream abcstream, AlembicImporter.aiObject abcobj)
    {
        m_abcstream = abcstream;
        m_abcobj = abcobj;
    }

    public virtual void AbcUpdate()
    {
    }
}
