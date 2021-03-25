using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    [ExecuteInEditMode]
    internal class AlembicCamera : AlembicElement
    {
        aiCamera m_abcSchema;
        CameraData m_abcData;
        Camera m_camera;

        internal override aiSchema abcSchema { get { return m_abcSchema; } }
        public override bool visibility { get { return m_abcData.visibility; } }

        internal override void AbcSetup(aiObject abcObj, aiSchema abcSchema)
        {
            base.AbcSetup(abcObj, abcSchema);
            m_abcSchema = (aiCamera)abcSchema;

            m_camera = GetOrAddCamera();

            // flip forward direction (camera in Alembic has inverted forward direction)
            abcTreeNode.gameObject.transform.localEulerAngles = new Vector3(0, 180, 0);
        }

        public override void AbcSyncDataEnd()
        {
            if (disposed || !m_abcSchema.schema.isDataUpdated)
                return;

            m_abcSchema.sample.GetData(ref m_abcData);

            if (abcTreeNode.stream.streamDescriptor.Settings.ImportVisibility)
                abcTreeNode.gameObject.SetActive(m_abcData.visibility);

            m_camera.focalLength = m_abcData.focalLength;
            m_camera.sensorSize = m_abcData.sensorSize;
            m_camera.lensShift = m_abcData.lensShift;

            m_camera.nearClipPlane = m_abcData.nearClipPlane;
            m_camera.farClipPlane = m_abcData.farClipPlane;
        }
    }
}
