using UnityEngine;


namespace UTJ.Alembic
{
	[ExecuteInEditMode]
	public class AlembicPointsCloud : MonoBehaviour
	{
		// members
		[ReadOnly] public Vector3[] m_abcPositions;
		[ReadOnly] public Vector3[] m_abcVelocities;
		[ReadOnly] public ulong[] m_abcIDs;

		[ReadOnly] public Vector3 m_boundsCenter;
		[ReadOnly] public Vector3 m_boundsExtents;
		[ReadOnly] public int m_peakVertexCount;
		[ReadOnly] public int m_count;

		// properties
		public Vector3[] abcPositions
		{
			get { return m_abcPositions; }
		}

		public ulong[] abcIDs
		{
			get { return m_abcIDs; }
		}


		void Reset()
		{
		}

	}
}
