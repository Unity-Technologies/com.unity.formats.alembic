using UnityEngine;

namespace UTJ.Alembic
{
    public class AlembicStreamDescriptor : ScriptableObject
    {
        public AlembicImportSettings m_ImportSettings;
        [Header("Overview")] [ReadOnly] [SerializeField] public AlembicImportMode m_importMode;
    }

}
