using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    internal class AlembicSubD : AlembicMesh
    {
        aiSubD m_abcSchema;

        internal override aiSchema abcSchema
        {
            get { return m_abcSchema; }
        }

        internal override void AbcSetup(aiObject abcObj, aiSchema abcSchema)
        {
            base.AbcSetup(abcObj, abcSchema);
            m_abcSchema = (aiSubD)abcSchema;

            m_abcSchema.GetSummary(ref m_summary);
        }
    }
}
