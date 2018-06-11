using UnityEngine;

namespace UTJ.Alembic
{
    public class AlembicXform : AlembicElement
    {
        aiXform m_abcSchema;
        aiXformData m_abcData;

        public override aiSchema abcSchema { get { return m_abcSchema; } }
        public override bool visibility { get { return m_abcData.visibility; } }

        public override void AbcSetup(aiObject abcObj, aiSchema abcSchema)
        {
            base.AbcSetup(abcObj, abcSchema);

            m_abcSchema = (aiXform)abcSchema;
        }

        public override void AbcSyncDataEnd()
        {
            if (!m_abcSchema.schema.isDataUpdated)
                return;

            m_abcSchema.sample.GetData(ref m_abcData);

            if (!abcTreeNode.stream.ignoreVisibility)
                abcTreeNode.gameObject.SetActive(m_abcData.visibility);

            var trans = abcTreeNode.gameObject.GetComponent<Transform>();
            if (m_abcData.inherits)
            {
                trans.localPosition = m_abcData.translation;
                trans.localRotation = m_abcData.rotation;
                trans.localScale = m_abcData.scale;
            }
            else
            {
                trans.position = m_abcData.translation;
                trans.rotation = m_abcData.rotation;
                trans.localScale = m_abcData.scale;
            }
        }
    }
}
