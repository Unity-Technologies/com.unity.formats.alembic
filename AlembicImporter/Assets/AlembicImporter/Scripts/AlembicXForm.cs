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
public class AlembicXForm : AlembicElement
{
    Transform m_trans;
    AbcAPI.aiXFormData m_abcdata;

    public override void AbcSetup(
        AlembicStream abcstream,
        AbcAPI.aiObject abcobj,
        AbcAPI.aiSchema abcschema)
    {
        base.AbcSetup(abcstream, abcobj, abcschema);
        m_trans = GetComponent<Transform>();
    }

    public override void AbcOnUpdateSample(AbcAPI.aiSample sample)
    {
        AbcAPI.aiXFormGetData(sample, ref m_abcdata);
    }

    public override void AbcUpdate()
    {
        var trans = m_trans;

        if (m_abcdata.inherits != 0)
        {
            trans.localPosition = m_abcdata.translation;
            trans.localRotation = m_abcdata.rotation;
            trans.localScale = m_abcdata.scale;
        }
        else
        {
            trans.position = m_abcdata.translation;
            trans.rotation = m_abcdata.rotation;
            trans.localScale = m_abcdata.scale;
        }
    }
}
