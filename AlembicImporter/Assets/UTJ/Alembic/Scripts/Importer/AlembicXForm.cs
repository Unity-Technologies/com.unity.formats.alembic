using UnityEngine;

namespace UTJ.Alembic
{
    public class AlembicXForm : AlembicElement
    {
        private AbcAPI.aiXFormData m_abcData;

        // No config overrides on AlembicXForm

        public override void AbcBeforeUpdateSamples()
        {
            // nothing to do
        }

        public override void AbcSampleUpdated(AbcAPI.aiSample sample)
        {
            AbcAPI.aiXFormGetData(sample, ref m_abcData);
        }

        public override void AbcUpdate()
        {
            if (!m_abcSchema.dirty)
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
