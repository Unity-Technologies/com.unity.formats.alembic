using UnityEngine;

namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    public class AlembicCamera : AlembicElement
    {
        public bool ignoreClippingPlanes = false;

        private Camera m_Camera;
        private AbcAPI.aiCameraData m_AbcData;
        private bool m_LastIgnoreClippingPlanes = false;

        public override void AbcSetup(AbcAPI.aiObject abcObj,
                                      AbcAPI.aiSchema abcSchema)
        {
            base.AbcSetup( abcObj, abcSchema);

            m_Camera = GetOrAddComponent<Camera>();
        }

        public override void AbcUpdateConfig()
        {
            // nothing to do
        }

        public override void AbcSampleUpdated(AbcAPI.aiSample sample, bool topologyChanged)
        {
            AbcAPI.aiCameraGetData(sample, ref m_AbcData);

            AbcDirty();
        }

        public override void AbcUpdate()
        {
            if (AbcIsDirty() || m_LastIgnoreClippingPlanes != ignoreClippingPlanes)
            {
                AlembicTreeNode.linkedGameObj.transform.forward = -AlembicTreeNode.linkedGameObj.transform.parent.forward;
                m_Camera.fieldOfView = m_AbcData.fieldOfView;

                if (!ignoreClippingPlanes)
                {
                    m_Camera.nearClipPlane = m_AbcData.nearClippingPlane;
                    m_Camera.farClipPlane = m_AbcData.farClippingPlane;
                }

                // no use for focusDistance and focalLength yet (could be usefull for DoF component)

                AbcClean();

                m_LastIgnoreClippingPlanes = ignoreClippingPlanes;
            }
        }
    }
}
