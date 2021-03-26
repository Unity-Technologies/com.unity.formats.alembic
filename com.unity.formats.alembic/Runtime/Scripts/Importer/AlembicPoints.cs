using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    internal class AlembicPoints : AlembicElement
    {
        // members
        aiPoints m_abcSchema;
        PinnedList<aiPointsData> m_abcData = new PinnedList<aiPointsData>(1);
        aiPointsSummary m_summary;
        aiPointsSampleSummary m_sampleSummary;

        internal override aiSchema abcSchema { get { return m_abcSchema; } }
        public override bool visibility { get { return m_abcData[0].visibility; } }

        internal override void AbcSetup(aiObject abcObj, aiSchema abcSchema)
        {
            base.AbcSetup(abcObj, abcSchema);
            m_abcSchema = (aiPoints)abcSchema;
            m_abcSchema.GetSummary(ref m_summary);
        }

        public override void AbcPrepareSample()
        {
            if (disposed)
            {
                return;
            }

            var cloud = abcTreeNode.gameObject.GetComponent<AlembicPointsCloud>();
            if (cloud != null)
            {
                m_abcSchema.sort = cloud.m_sort;
                if (cloud.m_sort && cloud.m_sortFrom != null)
                {
                    m_abcSchema.sortBasePosition = cloud.m_sortFrom.position;
                }
            }
        }

        public override void AbcSyncDataBegin()
        {
            if (disposed || !m_abcSchema.schema.isDataUpdated)
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
            cloud.pointsList.ResizeDiscard(m_sampleSummary.count);
            data.points = cloud.pointsList;
            if (m_summary.hasVelocities)
            {
                cloud.velocitiesList.ResizeDiscard(m_sampleSummary.count);
                data.velocities = cloud.velocitiesList;
            }
            if (m_summary.hasIDs)
            {
                cloud.idsList.ResizeDiscard(m_sampleSummary.count);
                data.ids = cloud.idsList;
            }
            m_abcData[0] = data;

            // kick async copy
            sample.FillData(m_abcData);
        }

        public override void AbcSyncDataEnd()
        {
            if (disposed || !m_abcSchema.schema.isDataUpdated)
                return;

            var data = m_abcData[0];

            if (abcTreeNode.stream.streamDescriptor.Settings.ImportVisibility)
                abcTreeNode.gameObject.SetActive(data.visibility);

            var cloud = abcTreeNode.gameObject.GetComponent<AlembicPointsCloud>();
            cloud.BoundsCenter = data.boundsCenter;
            cloud.BoundsExtents = data.boundsExtents;
        }
    }
}
