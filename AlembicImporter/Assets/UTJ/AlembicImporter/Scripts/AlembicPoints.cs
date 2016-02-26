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

namespace UTJ
{
    [ExecuteInEditMode]
    public class AlembicPoints : AlembicElement
    {
        // members
        AbcAPI.aiPointsData m_abcData;
        Vector3[] m_abcPositions;
        Vector3[] m_abcVelocities;
        ulong[] m_abcIDs;
        AbcAPI.aiPointsSummary m_summary;
    
        // properties
        public AbcAPI.aiPointsData abcData { get { return m_abcData; } }
        public Vector3[] abcPositions { get { return m_abcPositions; } }
        public ulong[] abcIDs { get { return m_abcIDs; } }
        public int abcPeakVertexCount
        {
            get {
                if (m_summary.peakCount == 0)
                {
                    AbcAPI.aiPointsGetSummary(m_abcSchema, ref m_summary);
                }
                return m_summary.peakCount;
            }
        }
    
    
        // No config overrides on AlembicPoints
    
        public override void AbcSampleUpdated(AbcAPI.aiSample sample, bool topologyChanged)
        {
            if(m_abcPositions == null)
            {
                AbcAPI.aiPointsGetSummary(m_abcSchema, ref m_summary);
                m_abcPositions = new Vector3[m_summary.peakCount];
                m_abcIDs = new ulong[m_summary.peakCount];    
                m_abcData.positions = Marshal.UnsafeAddrOfPinnedArrayElement(m_abcPositions, 0);
                m_abcData.ids = Marshal.UnsafeAddrOfPinnedArrayElement(m_abcIDs, 0);
                if (m_summary.hasVelocity)
                {
                    m_abcVelocities = new Vector3[m_summary.peakCount];
                    m_abcData.velocities = Marshal.UnsafeAddrOfPinnedArrayElement(m_abcVelocities, 0);
                }
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
}
