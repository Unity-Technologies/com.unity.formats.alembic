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

        public override void AbcSetup(aiObject abcObj, aiSchema abcSchema)
        {
            base.AbcSetup( abcObj, abcSchema);
            m_abcSchema = (aiCamera)abcSchema;

            m_camera = GetOrAddComponent<Camera>();
        }

        public override void AbcSyncDataEnd()
        {
            if (!m_abcSchema.schema.isDataUpdated)
                return;

            m_abcSchema.sample.GetData(ref m_abcData);
            abcTreeNode.linkedGameObj.transform.forward = -abcTreeNode.linkedGameObj.transform.parent.forward;
            m_camera.fieldOfView = m_abcData.fieldOfView;

            if (!m_ignoreClippingPlanes)
            {
                m_camera.nearClipPlane = m_abcData.nearClippingPlane;
                m_camera.farClipPlane = m_abcData.farClippingPlane;
            }

            // no use for focusDistance and focalLength yet (could be usefull for DoF component)
        }
    }
}
