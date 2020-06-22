using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    internal class AlembicCurves : AlembicElement
    {
        // members
        aiCurves m_abcSchema;
        PinnedList<aiCurvesData> m_abcData = new PinnedList<aiCurvesData>(1);
        aiCurvesSummary m_summary;
        aiCurvesSampleSummary m_sampleSummary;

        internal override aiSchema abcSchema { get { return m_abcSchema; } }
        public override bool visibility
        {
            get { return m_abcData[0].visibility; }
        }

        internal override void AbcSetup(aiObject abcObj, aiSchema abcSchema)
        {
            base.AbcSetup(abcObj, abcSchema);
            m_abcSchema = (aiCurves)abcSchema;
            m_abcSchema.GetSummary(ref m_summary);
        }

        public override void AbcPrepareSample()
        {
            base.AbcPrepareSample();
        }

        public override void AbcSyncDataBegin()
        {
            if (!m_abcSchema.schema.isDataUpdated)
                return;

            var sample = m_abcSchema.sample;
            sample.GetSummary(ref m_sampleSummary);

            // get points cloud component
            var cloud = abcTreeNode.gameObject.GetComponent<AlembicCurveCollection>();
            if (cloud == null)
            {
                cloud = abcTreeNode.gameObject.AddComponent<AlembicCurveCollection>();
                //  abcTreeNode.gameObject.AddComponent<AlembicPointsRenderer>(); // Need rendering
            }
            var data = default(aiCurvesData);

            cloud.positionsList.ResizeDiscard(m_sampleSummary.count);
            data.positions = cloud.positionsList;

            m_abcData[0] = data;

            // kick async copy
            sample.FillData(m_abcData);
        }
    }
}
