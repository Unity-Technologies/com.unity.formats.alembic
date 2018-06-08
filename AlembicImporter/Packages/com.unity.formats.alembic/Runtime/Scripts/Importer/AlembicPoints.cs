using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{
    public class AlembicPoints : AlembicElement
    {
        // members
        aiPoints m_abcSchema;
        PinnedList<aiPointsData> m_abcData = new PinnedList<aiPointsData>(1);
        aiPointsSummary m_summary;
        aiPointsSampleSummary m_sampleSummary;

        public override aiSchema abcSchema { get { return m_abcSchema; } }
        public override bool visibility { get { return m_abcData[0].visibility; } }

        public override void AbcSetup(aiObject abcObj, aiSchema abcSchema)
        {
            base.AbcSetup(abcObj, abcSchema);
            m_abcSchema = (aiPoints)abcSchema;
            m_abcSchema.GetSummary(ref m_summary);
        }

        public override void AbcPrepareSample()
        {
            var cloud = abcTreeNode.gameObject.GetComponent<AlembicPointsCloud>();
            if(cloud != null)
            {
                m_abcSchema.sort = cloud.m_sort;
                if(cloud.m_sort && cloud.m_sortFrom != null)
                {
                    m_abcSchema.sortBasePosition = cloud.m_sortFrom.position;
                }
            }
        }

        public override void AbcSyncDataBegin()
        {
            if (!m_abcSchema.schema.isDataUpdated)
                return;

            var sample = m_abcSchema.sample;
            sample.GetSummary(ref m_sampleSummary);

            // get points cloud component
            var cloud = abcTreeNode.gameObject.GetComponent<AlembicPointsCloud>();
            if (cloud == null)
            {
                cloud = abcTreeNode.gameObject.AddComponent<AlembicPointsCloud>();
                abcTreeNode.gameObject.AddComponent<AlembicPointsRenderer>();
            }

            // setup buffers
            var data = default(aiPointsData);
            cloud.m_points.ResizeDiscard(m_sampleSummary.count);
            data.points = cloud.m_points;
            if (m_summary.hasVelocities)
            {
                cloud.m_velocities.ResizeDiscard(m_sampleSummary.count);
                data.velocities = cloud.m_velocities;
            }
            if (m_summary.hasIDs)
            {
                cloud.m_ids.ResizeDiscard(m_sampleSummary.count);
                data.ids = cloud.m_ids;
            }
            m_abcData[0] = data;

            // kick async copy
            sample.FillData(m_abcData);
        }

        public override void AbcSyncDataEnd()
        {
            if (!m_abcSchema.schema.isDataUpdated)
                return;

            var sample = m_abcSchema.sample;
            // wait async copy complete
            sample.Sync();

            var data = m_abcData[0];

            if (!abcTreeNode.stream.ignoreVisibility)
                abcTreeNode.gameObject.SetActive(data.visibility);

            var cloud = abcTreeNode.gameObject.GetComponent<AlembicPointsCloud>();
            cloud.m_boundsCenter = data.boundsCenter;
            cloud.m_boundsExtents = data.boundsExtents;
        }
    }
}
