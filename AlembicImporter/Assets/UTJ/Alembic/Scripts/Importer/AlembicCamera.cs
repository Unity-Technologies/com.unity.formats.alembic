using UnityEngine;

namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    public class AlembicCamera : AlembicElement
    {
        public bool m_ignoreClippingPlanes = false;
        Camera m_camera;
        AbcAPI.aiCameraData m_abcData;
        bool m_lastIgnoreClippingPlanes = false;

        public override void AbcSetup(AbcAPI.aiObject abcObj, AbcAPI.aiSchema abcSchema)
        {
            base.AbcSetup( abcObj, abcSchema);

            m_camera = GetOrAddComponent<Camera>();
        }

        public override void AbcSampleUpdated(AbcAPI.aiSample sample)
        {
            AbcAPI.aiCameraGetData(sample, ref m_abcData);
        }

        public override void AbcUpdate()
        {
            if (!m_abcSchema.dirty)
                return;

            AbcSampleUpdated(m_abcSchema.sample);

            if (m_lastIgnoreClippingPlanes != m_ignoreClippingPlanes)
            {
                abcTreeNode.linkedGameObj.transform.forward = -abcTreeNode.linkedGameObj.transform.parent.forward;
                m_camera.fieldOfView = m_abcData.fieldOfView;

                if (!m_ignoreClippingPlanes)
                {
                    m_camera.nearClipPlane = m_abcData.nearClippingPlane;
                    m_camera.farClipPlane = m_abcData.farClippingPlane;
                }

                // no use for focusDistance and focalLength yet (could be usefull for DoF component)
                m_lastIgnoreClippingPlanes = m_ignoreClippingPlanes;
            }
        }
    }
}
