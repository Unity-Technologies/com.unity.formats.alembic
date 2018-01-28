using UnityEngine;

namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    public class AlembicCamera : AlembicElement
    {
        aiCamera m_abcSchema;
        aiCameraData m_abcData;
        Camera m_camera;
        bool m_ignoreClippingPlanes = false;
        bool m_lastIgnoreClippingPlanes = false;

        public override void AbcSetup(aiObject abcObj, aiSchema abcSchema)
        {
            base.AbcSetup( abcObj, abcSchema);
            m_abcSchema = (aiCamera)abcSchema;

            m_camera = GetOrAddComponent<Camera>();
        }

        public override void AbcSampleUpdated(aiSample sample_)
        {
            var sample = (aiCameraSample)sample_;
            sample.GetData(ref m_abcData);
        }

        public override void AbcUpdate()
        {
            if (!m_abcSchema.schema.dirty)
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
