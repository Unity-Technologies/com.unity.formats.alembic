using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{
    public class AlembicPoints : AlembicElement
    {
        // members
        AbcAPI.aiPointsData m_AbcData;
        AbcAPI.aiPointsSummary m_Summary;

        // properties
        public AbcAPI.aiPointsData abcData { get { return m_AbcData; } }
        public int abcPeakVertexCount
        {
            get {
                if (m_Summary.peakCount == 0)
                {
                    AbcAPI.aiPointsGetSummary(m_AbcSchema, ref m_Summary);
                }
                return m_Summary.peakCount;
            }
        }

        public override void AbcUpdateConfig()
        {
            var cloud = AlembicTreeNode.linkedGameObj.GetComponent<AlembicPointsCloud>();
            if(cloud != null)
            {
                AbcAPI.aiPointsSetSort(m_AbcSchema, cloud.m_sort);
                if(cloud.m_sortFrom != null)
                {
                    AbcAPI.aiPointsSetSortBasePosition(m_AbcSchema, cloud.m_sortFrom.position);
                }
            }
        }

        // No config overrides on AlembicPoints
        public override void AbcSampleUpdated(AbcAPI.aiSample sample, bool topologyChanged)
        {
            // get points cloud component
            var cloud = AlembicTreeNode.linkedGameObj.GetComponent<AlembicPointsCloud>() ??
                        AlembicTreeNode.linkedGameObj.AddComponent<AlembicPointsCloud>();


            if (cloud.abcPositions.Count == 0)
            {
                AbcAPI.aiPointsGetSummary(m_AbcSchema, ref m_Summary);
                cloud.m_abcPositions.Resize(m_Summary.peakCount);
                cloud.m_abcIDs.Resize(m_Summary.peakCount);
                cloud.m_peakVertexCount = m_Summary.peakCount;
                m_AbcData.positions = cloud.m_abcPositions;
                m_AbcData.ids = cloud.m_abcIDs;
                if (m_Summary.hasVelocity)
                {
                    cloud.m_abcVelocities.Resize(m_Summary.peakCount);
                    m_AbcData.velocities = cloud.m_abcVelocities;
                }
            }

            m_AbcData.positions = cloud.m_abcPositions;
            m_AbcData.ids = cloud.m_abcIDs;
            if (m_Summary.hasVelocity)
                m_AbcData.velocities = cloud.m_abcVelocities;

            AbcAPI.aiPointsCopyData(sample, ref m_AbcData);
            cloud.m_boundsCenter = m_AbcData.boundsCenter;
            cloud.m_boundsExtents = m_AbcData.boundsExtents;
            cloud.m_count = m_AbcData.count;

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
