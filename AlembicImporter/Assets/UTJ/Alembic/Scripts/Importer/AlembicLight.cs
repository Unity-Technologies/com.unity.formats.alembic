using UnityEngine;

namespace UTJ.Alembic
{
    public class AlembicLight : AlembicElement
    {
        aiSchema m_abcSchema;

        public override void AbcSetup(aiObject abcObj, aiSchema abcSchema)
        {
            base.AbcSetup( abcObj, abcSchema);
            m_abcSchema = abcSchema;

            Light light = GetOrAddComponent<Light>();
            // Disable component for now
            light.enabled = false;
        }

        // No config override

        public override void AbcSampleUpdated(aiSample sample)
        {
            // ToDo
        }

        public override void AbcUpdate()
        {
            if (!m_abcSchema.isDataUpdated)
                return;

            AbcSampleUpdated(m_abcSchema.sample);

            // ToDo
        }
    }
}
