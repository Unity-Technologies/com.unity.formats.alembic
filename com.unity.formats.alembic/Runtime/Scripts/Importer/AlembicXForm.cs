using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    internal class AlembicXform : AlembicElement
    {
        aiXform m_abcSchema;
        aiXformData m_abcData;

        internal override aiSchema abcSchema { get { return m_abcSchema; } }
        public override bool visibility { get { return m_abcData.visibility; } }

        internal override void AbcSetup(aiObject abcObj, aiSchema abcSchema)
        {
            base.AbcSetup(abcObj, abcSchema);

            m_abcSchema = (aiXform)abcSchema;
        }

        public override void AbcSyncDataEnd()
        {
            if (disposed || !m_abcSchema.schema.isDataUpdated)
                return;

            m_abcSchema.sample.GetData(ref m_abcData);

            if (abcTreeNode.stream.streamDescriptor.Settings.ImportVisibility)
                abcTreeNode.gameObject.SetActive(m_abcData.visibility);

            var trans = abcTreeNode.gameObject.GetComponent<Transform>();
            if (m_abcData.inherits || abcObject.parent == abcObject.context.topObject)
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
