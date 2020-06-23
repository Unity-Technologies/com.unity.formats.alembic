using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    public class AlembicCurves : MonoBehaviour
    {
        PinnedList<Vector3> m_positions = new PinnedList<Vector3>();
        internal PinnedList<Vector3> positionsList { get { return m_positions; } }
        public List<Vector3> Positions => positionsList.List;

        PinnedList<int> m_positionsOffsetBuffer = new PinnedList<int>();
        internal PinnedList<int> positionOffsetBuffer { get { return m_positionsOffsetBuffer; } }
        public List<int> PositionsOffsetBuffer => positionOffsetBuffer.List;
    }
}
