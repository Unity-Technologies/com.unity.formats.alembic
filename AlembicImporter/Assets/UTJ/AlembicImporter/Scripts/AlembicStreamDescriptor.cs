using UnityEngine;

namespace UTJ.Alembic
{
	[ExecuteInEditMode]
	public class AlembicStreamDescriptor : ScriptableObject
	{
		public AlembicImportSettings m_ImportSettings;
		[Header("Overview")] [ReadOnly] [SerializeField] public AlembicImportMode m_importMode;
	}

}
