using UnityEngine;
#if UNITY_EDITOR

#endif

namespace UTJ.Alembic
{
    [ExecuteInEditMode]
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

        public override void AbcSampleUpdated(AbcAPI.aiSample sample, bool topologyChanged)
        {
            // ToDo
        }

        public override void AbcUpdate()
        {
            if (AbcIsDirty())
            {
                // ToDo

                AbcClean();
            }
        }
    }
}
