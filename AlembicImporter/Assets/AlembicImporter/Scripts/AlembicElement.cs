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


    public virtual void AbcSetup(AlembicStream abcstream)
    {
        m_abcstream = abcstream;
    }

    public virtual void AbcUpdate()
    {
    }
}
