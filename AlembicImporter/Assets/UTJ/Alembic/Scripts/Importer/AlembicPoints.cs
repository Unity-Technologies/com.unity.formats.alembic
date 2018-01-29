using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{
    public class AlembicPoints : AlembicElement
    {
        // members
        aiPoints m_abcSchema;
        aiPointsData m_abcData;
        aiPointsSummary m_summary;

        // properties
        public aiPointsData abcData { get { return m_abcData; } }


        public override void AbcSetup(aiObject abcObj, aiSchema abcSchema)
        {
            base.AbcSetup(abcObj, abcSchema);
            m_abcSchema = (aiPoints)abcSchema;
            m_abcSchema.GetSummary(ref m_summary);
        }

        public override void AbcPrepareSample()
        {
            var cloud = abcTreeNode.linkedGameObj.GetComponent<AlembicPointsCloud>();
            if(cloud != null)
            {
                m_abcSchema.sort = cloud.m_sort;
                if(cloud.m_sort && cloud.m_sortFrom != null)
                {
                    m_abcSchema.sortBasePosition = cloud.m_sortFrom.position;
                }
            }
        }

        public override void AbcSyncDataEnd()
        {
            if (!m_abcSchema.schema.isDataUpdated)
                return;

            var sample = m_abcSchema.sample;
            // get points cloud component
            var cloud = abcTreeNode.linkedGameObj.GetComponent<AlembicPointsCloud>() ??
                        abcTreeNode.linkedGameObj.AddComponent<AlembicPointsCloud>();

            if (cloud.abcPoints.Count == 0)
            {
                m_abcSchema.GetSummary(ref m_summary);
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

            sample.CopyData(ref m_abcData);
            cloud.m_boundsCenter = m_abcData.boundsCenter;
            cloud.m_boundsExtents = m_abcData.boundsExtents;
            cloud.m_count = m_abcData.count;
        }
    }
}
