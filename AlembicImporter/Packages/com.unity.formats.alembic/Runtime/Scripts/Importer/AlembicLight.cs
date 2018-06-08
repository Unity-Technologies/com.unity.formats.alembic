using UnityEngine;

namespace UTJ.Alembic
{
    /*
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


        public override void AbcSyncDataEnd()
        {
            if (!m_abcSchema.isDataUpdated)
                return;

            // ToDo
        }
    }
    */
}
