using UnityEngine;

namespace UTJ.Alembic
{
    public class AlembicXForm : AlembicElement
    {
        aiXform m_abcSchema;
        aiXFormData m_abcData;

        public override void AbcSetup(aiObject abcObj, aiSchema abcSchema)
        {
            base.AbcSetup(abcObj, abcSchema);

            m_abcSchema = (aiXform)abcSchema;
        }

        public override void AbcSampleUpdated(aiSample sample_)
        {
            var sample = (aiXformSample)sample_;
            sample.GetData(ref m_abcData);
        }

        public override void AbcUpdate()
        {
            if (!m_abcSchema.schema.dirty)
                return;

            AbcSampleUpdated(m_abcSchema.sample);

            if (m_abcData.inherits)
            {
                abcTreeNode.linkedGameObj.transform.localPosition = m_abcData.translation;
                abcTreeNode.linkedGameObj.transform.localRotation = m_abcData.rotation;
                abcTreeNode.linkedGameObj.transform.localScale = m_abcData.scale;
            }
            else
            {
                abcTreeNode.linkedGameObj.transform.position = m_abcData.translation;
                abcTreeNode.linkedGameObj.transform.rotation = m_abcData.rotation;
                abcTreeNode.linkedGameObj.transform.localScale = m_abcData.scale;
            }
        }
    }
}
