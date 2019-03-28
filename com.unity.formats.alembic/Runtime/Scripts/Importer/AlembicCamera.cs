using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    [ExecuteInEditMode]
    internal class AlembicCamera : AlembicElement
    {
        aiCamera m_abcSchema;
        aiCameraData m_abcData;
        Camera m_camera;
        bool m_ignoreClippingPlanes = false;

        internal override aiSchema abcSchema { get { return m_abcSchema; } }
        public override bool visibility { get { return m_abcData.visibility; } }

        internal override void AbcSetup(aiObject abcObj, aiSchema abcSchema)
        {
            base.AbcSetup( abcObj, abcSchema);
            m_abcSchema = (aiCamera)abcSchema;

            m_camera = GetOrAddComponent<Camera>();

            // flip forward direction (camera in Alembic has inverted forward direction)
            abcTreeNode.gameObject.transform.localEulerAngles = new Vector3(0, 180, 0);
        }

        public override void AbcSyncDataEnd()
        {
            if (!m_abcSchema.schema.isDataUpdated)
                return;

            m_abcSchema.sample.GetData(ref m_abcData);

            if (abcTreeNode.stream.streamDescriptor.Settings.ImportVisibility)
                abcTreeNode.gameObject.SetActive(m_abcData.visibility);

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
