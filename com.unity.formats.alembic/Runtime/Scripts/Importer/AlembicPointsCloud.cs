using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;


namespace UnityEngine.Formats.Alembic.Importer
{
    [ExecuteInEditMode]
    internal class AlembicPointsCloud : MonoBehaviour
    {
        // members
        [ReadOnly] public PinnedList<Vector3> m_points = new PinnedList<Vector3>();
        [ReadOnly] public PinnedList<Vector3> m_velocities = new PinnedList<Vector3>();
        [ReadOnly] public PinnedList<uint> m_ids = new PinnedList<uint>();

        [ReadOnly] public Vector3 m_boundsCenter;
        [ReadOnly] public Vector3 m_boundsExtents;

        internal AlembicPoints m_abc = null;

        [Tooltip("Sort points by distance from sortFrom object")]
        public bool m_sort = false;
        public Transform m_sortFrom;

        // properties
        internal AlembicPoints abcPoints { get { return m_abc; } }
        public PinnedList<Vector3> points { get { return m_points; } }
        public PinnedList<Vector3> velocities { get { return m_velocities; } }
        public PinnedList<uint> ids { get { return m_ids; } }


        void Reset()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                m_sortFrom = cam.GetComponent<Transform>();
            }
        }

        private void OnDestroy()
        {
            if (m_points != null) m_points.Dispose();
            if (m_velocities != null) m_velocities.Dispose();
            if (m_ids != null) m_ids.Dispose();
        }
    }
}
