using UnityEngine;

namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    public class AlembicXForm : AlembicElement
    {
        AbcAPI.aiXFormData m_abcData;

        // No config overrides on AlembicXForm

        public override void AbcSampleUpdated(AbcAPI.aiSample sample, bool topologyChanged)
        {
            AbcAPI.aiXFormGetData(sample, ref m_abcData);

            AbcDirty();
        }

        public override void AbcUpdate()
        {
            if (AbcIsDirty())
            {
                if (m_abcData.inherits)
                {
					AlembicTreeNode.linkedGameObj.transform.localPosition = m_abcData.translation;
					AlembicTreeNode.linkedGameObj.transform.localRotation = m_abcData.rotation;
					AlembicTreeNode.linkedGameObj.transform.localScale = m_abcData.scale;
                }
                else
                {
					AlembicTreeNode.linkedGameObj.transform.position = m_abcData.translation;
					AlembicTreeNode.linkedGameObj.transform.rotation = m_abcData.rotation;
					AlembicTreeNode.linkedGameObj.transform.localScale = m_abcData.scale;
                }

                AbcClean();
            }
        }
    }
}
