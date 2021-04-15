using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    class AlembicCurvesElement : AlembicElement
    {
        // members
        aiCurves m_abcSchema;
        PinnedList<aiCurvesData> m_abcData = new PinnedList<aiCurvesData>(1);
        aiCurvesSummary m_summary;
        aiCurvesSampleSummary m_sampleSummary;
        internal bool CreateRenderingComponent { get; set; }

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

        public override void AbcSyncDataBegin()
        {
            if (disposed || !m_abcSchema.schema.isDataUpdated)
                return;

            var sample = m_abcSchema.sample;
            sample.GetSummary(ref m_sampleSummary);

            // get points cloud component
            var curves = abcTreeNode.gameObject.GetOrAddComponent<AlembicCurves>();
            if (CreateRenderingComponent)
            {
                abcTreeNode.gameObject.GetOrAddComponent<AlembicCurvesRenderer>();
            }

            var data = default(aiCurvesData);

            if (m_summary.hasPositions)
            {
                curves.positionsList.ResizeDiscard(m_sampleSummary.positionCount);
                curves.velocitiesList.ResizeDiscard(m_sampleSummary.positionCount);

                curves.curveOffsets.ResizeDiscard(m_sampleSummary.numVerticesCount);
                data.positions = curves.positionsList;
                data.velocities = curves.velocitiesList;
                data.numVertices = curves.curveOffsets;
            }

            if (m_summary.hasWidths)
            {
                curves.widths.ResizeDiscard(m_sampleSummary.positionCount);
                data.widths = curves.widths;
            }

            if (m_summary.hasUVs)
            {
                curves.uvs.ResizeDiscard(m_sampleSummary.positionCount);
                data.uvs = curves.uvs;
            }

            m_abcData[0] = data;
            sample.FillData(m_abcData);
        }

        public override void AbcSyncDataEnd()
        {
            if (disposed || !m_abcSchema.schema.isDataUpdated)
                return;

            var data = m_abcData[0];

            if (abcTreeNode.stream.streamDescriptor.Settings.ImportVisibility)
                abcTreeNode.gameObject.SetActive(data.visibility);

            var curves = abcTreeNode.gameObject.GetComponent<AlembicCurves>();

            var cnt = 0;
            for (var i = 0; i < curves.CurveOffsets.Length; ++i)
            {
                var v = curves.CurveOffsets[i];
                curves.CurveOffsets[i] = cnt;
                cnt += v;
            }

            curves.InvokeOnUpdate(curves);
        }
    }
}
