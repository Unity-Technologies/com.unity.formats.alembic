using System.Collections.Generic;
using UnityEngine.Formats.Alembic.Sdk;


namespace UnityEngine.Formats.Alembic.Importer
{
    /// <summary>
    /// This class is the scene data container for animated point clouds.
    /// </summary>
    [ExecuteInEditMode]
    public class AlembicPointsCloud : MonoBehaviour
    {
        // members
        PinnedList<Vector3> m_points = new PinnedList<Vector3>();
        PinnedList<Vector3> m_velocities = new PinnedList<Vector3>();
        PinnedList<uint> m_ids = new PinnedList<uint>();

        internal AlembicPoints m_abc = null;

        [Tooltip("Sort points by distance from sortFrom object")]
        internal bool m_sort = false;
        internal Transform m_sortFrom;

        // properties
        internal PinnedList<Vector3> pointsList { get { return m_points; } }
        internal PinnedList<Vector3> velocitiesList { get { return m_velocities; } }
        internal PinnedList<uint> idsList { get { return m_ids; } }

        /// <summary>
        /// The list of point cloud positions.
        /// </summary>
        public List<Vector3> positions => pointsList.List;
        /// <summary>
        /// The list of point cloud velocities.
        /// </summary>
        public List<Vector3> velocities => velocitiesList.List;
        /// <summary>
        /// The list of point cloud identifiers.
        /// </summary>
        public List<uint> ids => idsList.List;

        /// <summary>
        /// The center of the point cloud bounding box
        /// </summary>
        public Vector3 boundsCenter { get; internal set; }
        /// <summary>
        /// The extent of the point cloud bounding box
        /// </summary>
        public Vector3 boundsExtents { get; internal set; }

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
