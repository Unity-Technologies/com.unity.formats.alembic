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
    bool m_pendingUpdate;

    public override void AbcSetup(AlembicStream abcStream,
                                  AbcAPI.aiObject abcObj,
                                  AbcAPI.aiSchema abcSchema)
    {
        base.AbcSetup(abcStream, abcObj, abcSchema);

        Light light = GetOrAddComponent<Light>();

        // Disable component for now
        light.enabled = false;

        m_pendingUpdate = false;
    }

    // No config override

    public override void AbcSampleUpdated(AbcAPI.aiSample sample, bool topologyChanged)
    {
        // ToDo
    }

    public override void AbcUpdate()
    {
        // ToDo
        if (!m_pendingUpdate)
        {
            return;
        }

        m_pendingUpdate = false;
    }
}
