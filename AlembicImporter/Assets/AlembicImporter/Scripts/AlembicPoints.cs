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
    // members
    AbcAPI.aiPointsData m_abcData;
    Vector3[] m_abcPositions;
    Int64[] m_abcIDs;
    int m_abcPeakVertexCount;

    // properties
    public AbcAPI.aiPointsData abcData { get { return m_abcData; } }
    public Vector3[] abcPositions { get { return m_abcPositions; } }
    public Int64[] abcIDs { get { return m_abcIDs; } }
    public int abcPeakVertexCount
    {
        get {
            if (m_abcPeakVertexCount == 0)
            {
                m_abcPeakVertexCount = AbcAPI.aiPointsGetPeakVertexCount(m_abcSchema);
            }
            return m_abcPeakVertexCount;
        }
    }


    // No config overrides on AlembicPoints

    public override void AbcSampleUpdated(AbcAPI.aiSample sample, bool topologyChanged)
    {
        if(m_abcPositions == null)
        {
            m_abcPeakVertexCount = AbcAPI.aiPointsGetPeakVertexCount(m_abcSchema);
            m_abcPositions = new Vector3[m_abcPeakVertexCount];
            m_abcIDs = new Int64[m_abcPeakVertexCount];

            m_abcData.positions = Marshal.UnsafeAddrOfPinnedArrayElement(m_abcPositions, 0);
            m_abcData.ids = Marshal.UnsafeAddrOfPinnedArrayElement(m_abcIDs, 0);
        }

        AbcAPI.aiPointsCopyData(sample, ref m_abcData);
        AbcDirty();
    }

    public override void AbcUpdate()
    {
        if (AbcIsDirty())
        {
            // nothing to do in this component.
            AbcClean();
        }
    }


    void Reset()
    {
        // add renderer
        var c = gameObject.GetComponent<AlembicPointsRenderer>();
        if (c == null)
        {
            c = gameObject.AddComponent<AlembicPointsRenderer>();
        }
    }
}
