using UnityEngine;


namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(AlembicPointsRenderer))]
    public class AlembicPointsCloud : MonoBehaviour
    {
        // members
        [ReadOnly] public PinnedList<Vector3> m_abcPoints = new PinnedList<Vector3>();
        [ReadOnly] public PinnedList<Vector3> m_abcVelocities = new PinnedList<Vector3>();
        [ReadOnly] public PinnedList<ulong> m_abcIDs = new PinnedList<ulong>();

        [ReadOnly] public Vector3 m_boundsCenter;
        [ReadOnly] public Vector3 m_boundsExtents;
        [ReadOnly] public int m_peakPointCount;
        [ReadOnly] public int m_count;

        AlembicPoints m_abc;

        [Tooltip("Sort points by distance from sortFrom object")]
        public bool m_sort = false;
        public Transform m_sortFrom;

        // properties
        public PinnedList<Vector3> abcPoints
        {
            get { return m_abcPoints; }
        }

        public PinnedList<Vector3> abcVelocities
        {
            get { return m_abcVelocities; }
        }

        public PinnedList<ulong> abcIDs
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
