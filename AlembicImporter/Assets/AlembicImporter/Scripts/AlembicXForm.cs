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

    public override void AbcSetup(AlembicStream abcstream, AlembicImporter.aiObject abcobj)
    {
        base.AbcSetup(abcstream, abcobj);
        m_trans = GetComponent<Transform>();
    }

    public override void AbcUpdate()
    {
        var trans = m_trans;
        var abc = m_abcobj;

        Vector3 pos = AlembicImporter.aiXFormGetPosition(abc);
        Quaternion rot = Quaternion.AngleAxis(AlembicImporter.aiXFormGetAngle(abc), AlembicImporter.aiXFormGetAxis(abc));
        Vector3 scale = AlembicImporter.aiXFormGetScale(abc);
        if (AlembicImporter.aiXFormGetInherits(abc))
        {
            trans.localPosition = pos;
            trans.localRotation = rot;
            trans.localScale = scale;
        }
        else
        {
            trans.position = pos;
            trans.rotation = rot;
            trans.localScale = scale;
        }
    }
}
