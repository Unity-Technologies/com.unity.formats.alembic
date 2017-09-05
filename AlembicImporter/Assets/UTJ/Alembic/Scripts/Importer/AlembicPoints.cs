using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{
    public class AlembicPoints : AlembicElement
    {
        // members
        AbcAPI.aiPointsData m_abcData;
        AbcAPI.aiPointsSummary m_summary;

        // properties
        public AbcAPI.aiPointsData abcData { get { return m_abcData; } }
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

        public override void AbcUpdateConfig()
        {
            var cloud = AlembicTreeNode.linkedGameObj.GetComponent<AlembicPointsCloud>();
            if(cloud != null)
            {
                AbcAPI.aiPointsSetSort(m_abcSchema, cloud.m_sort);
                if(cloud.m_sortFrom != null)
                {
                    AbcAPI.aiPointsSetSortBasePosition(m_abcSchema, cloud.m_sortFrom.position);
                }
            }
        }

        // No config overrides on AlembicPoints
        public override void AbcSampleUpdated(AbcAPI.aiSample sample, bool topologyChanged)
        {
            // get points cloud component
            var cloud = AlembicTreeNode.linkedGameObj.GetComponent<AlembicPointsCloud>() ??
                        AlembicTreeNode.linkedGameObj.AddComponent<AlembicPointsCloud>();


            if (cloud.abcPositions == null)
            {
                AbcAPI.aiPointsGetSummary(m_abcSchema, ref m_summary);
                cloud.m_abcPositions = new Vector3[m_summary.peakCount];
                cloud.m_abcIDs = new ulong[m_summary.peakCount];
                cloud.m_peakVertexCount = m_summary.peakCount;
                m_abcData.positions = Marshal.UnsafeAddrOfPinnedArrayElement(cloud.m_abcPositions, 0);
                m_abcData.ids = Marshal.UnsafeAddrOfPinnedArrayElement(cloud.m_abcIDs, 0);
                if (m_summary.hasVelocity)
                {
                    cloud.m_abcVelocities = new Vector3[m_summary.peakCount];
                    m_abcData.velocities = Marshal.UnsafeAddrOfPinnedArrayElement(cloud.m_abcVelocities, 0);
                }
            }

            m_abcData.positions = Marshal.UnsafeAddrOfPinnedArrayElement(cloud.m_abcPositions, 0);
            m_abcData.ids = Marshal.UnsafeAddrOfPinnedArrayElement(cloud.m_abcIDs, 0);
            if (m_summary.hasVelocity)
                m_abcData.velocities = Marshal.UnsafeAddrOfPinnedArrayElement(cloud.m_abcVelocities, 0);

            AbcAPI.aiPointsCopyData(sample, ref m_abcData);
            cloud.m_boundsCenter = m_abcData.boundsCenter;
            cloud.m_boundsExtents = m_abcData.boundsExtents;
            cloud.m_count = m_abcData.count;

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

    }
}
