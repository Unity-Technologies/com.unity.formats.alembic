using UnityEngine;

namespace UTJ.Alembic
{
    public class AlembicXForm : AlembicElement
    {
        private AbcAPI.aiXFormData m_AbcData;

        // No config overrides on AlembicXForm

        public override void AbcSampleUpdated(AbcAPI.aiSample sample)
        {
            AbcAPI.aiXFormGetData(sample, ref m_AbcData);

            AbcDirty();
        }

        public override void AbcUpdateConfig()
        {
            // nothing to do
        }

        public override void AbcUpdate()
        {
            if (AbcIsDirty())
            {
                if (m_AbcData.inherits)
                {
                    AlembicTreeNode.linkedGameObj.transform.localPosition = m_AbcData.translation;
                    AlembicTreeNode.linkedGameObj.transform.localRotation = m_AbcData.rotation;
                    AlembicTreeNode.linkedGameObj.transform.localScale = m_AbcData.scale;
                }
                else
                {
                    AlembicTreeNode.linkedGameObj.transform.position = m_AbcData.translation;
                    AlembicTreeNode.linkedGameObj.transform.rotation = m_AbcData.rotation;
                    AlembicTreeNode.linkedGameObj.transform.localScale = m_AbcData.scale;
                }

                AbcClean();
            }
        }
    }
}
