using UnityEngine;

namespace UTJ.Alembic
{
    public class AlembicLight : AlembicElement
    {
        public override void AbcSetup(AbcAPI.aiObject abcObj, AbcAPI.aiSchema abcSchema)
        {
            base.AbcSetup( abcObj, abcSchema);

            Light light = GetOrAddComponent<Light>();

            // Disable component for now
            light.enabled = false;
        }

        // No config override

        public override void AbcSampleUpdated(AbcAPI.aiSample sample)
        {
            // ToDo
        }

        public override void AbcUpdate()
        {
            if (!m_abcSchema.dirty)
                return;

            AbcSampleUpdated(m_abcSchema.sample);

            // ToDo
        }
    }
}
