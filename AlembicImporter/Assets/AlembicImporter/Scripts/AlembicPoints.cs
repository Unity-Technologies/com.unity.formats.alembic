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
public class AlembicPoints : AlembicElement
{
    AbcAPI.aiPointsSampleData m_abcData;

    // No config overrides on AlembicPoints

    public override void AbcSampleUpdated(AbcAPI.aiSample sample, bool topologyChanged)
    {
        AbcAPI.aiPointsGetData(sample, ref m_abcData);
        AbcDirty();
    }

    public override void AbcUpdate()
    {
        // implement this in subclass

        //if (AbcIsDirty())
        //{
        //    // do something
        //    AbcClean();
        //}
    }
}
