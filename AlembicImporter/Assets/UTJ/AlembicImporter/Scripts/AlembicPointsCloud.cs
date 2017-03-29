using UnityEngine;


namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(AlembicPointsRenderer))]
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

        AlembicPoints m_abc;
        public bool m_sort = false;
        public Transform m_sortFrom;

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
            var cam = Camera.main;
            if(cam != null)
            {
                m_sortFrom = cam.GetComponent<Transform>();
            }
        }
    }
}
