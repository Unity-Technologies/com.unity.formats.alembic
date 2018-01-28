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
            var cloud = abcTreeNode.linkedGameObj.GetComponent<AlembicPointsCloud>();
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
        public override void AbcSampleUpdated(AbcAPI.aiSample sample)
        {
            // get points cloud component
            var cloud = abcTreeNode.linkedGameObj.GetComponent<AlembicPointsCloud>() ??
                        abcTreeNode.linkedGameObj.AddComponent<AlembicPointsCloud>();


            if (cloud.abcPoints.Count == 0)
            {
                AbcAPI.aiPointsGetSummary(m_abcSchema, ref m_summary);
                cloud.m_abcPoints.Resize(m_summary.peakCount);
                cloud.m_abcIDs.Resize(m_summary.peakCount);
                cloud.m_peakPointCount = m_summary.peakCount;
                m_abcData.positions = cloud.m_abcPoints;
                m_abcData.ids = cloud.m_abcIDs;
                if (m_summary.hasVelocity)
                {
                    cloud.m_abcVelocities.Resize(m_summary.peakCount);
                    m_abcData.velocities = cloud.m_abcVelocities;
                }
            }

            m_abcData.positions = cloud.m_abcPoints;
            m_abcData.ids = cloud.m_abcIDs;
            if (m_summary.hasVelocity)
                m_abcData.velocities = cloud.m_abcVelocities;

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
