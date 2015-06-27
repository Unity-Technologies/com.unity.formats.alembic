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
public class AlembicLight : AlembicElement
{
    public override void AbcSetup(
        AlembicStream abcstream,
        AbcAPI.aiObject abcobj,
        AbcAPI.aiSchema abcschema)
    {
        base.AbcSetup(abcstream, abcobj, abcschema);
    }


    public override void AbcOnUpdateSample(AbcAPI.aiSample sample)
    {
    }

    public override void AbcUpdate()
    {
    }
}
