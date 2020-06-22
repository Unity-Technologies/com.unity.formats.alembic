using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    public class AlembicCurveCollection : MonoBehaviour
    {
        PinnedList<Vector3> m_positions = new PinnedList<Vector3>();
        internal PinnedList<Vector3> positionsList { get { return m_positions; } }
        public List<Vector3> Positions => positionsList.List;

        PinnedList<int> m_numVertices = new PinnedList<int>();
        internal PinnedList<int> numVerticesList { get { return m_numVertices; } }
        public List<int> NumVertices => numVerticesList.List;
    }
}
