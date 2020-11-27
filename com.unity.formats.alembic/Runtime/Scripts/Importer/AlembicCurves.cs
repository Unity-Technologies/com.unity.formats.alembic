using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    [DisallowMultipleComponent]
    public class AlembicCurves : MonoBehaviour
    {
        PinnedList<Vector3> m_positions = new PinnedList<Vector3>();
        internal PinnedList<Vector3> positionsList { get { return m_positions; } }
        public List<Vector3> Positions => positionsList.List;

        PinnedList<int> m_curvePointCount = new PinnedList<int>();
        internal PinnedList<int> curvePointCount { get { return m_curvePointCount; } }
        public List<int> CurvePointCount => curvePointCount.List;

        PinnedList<Vector2> m_uvs = new PinnedList<Vector2>();
        internal PinnedList<Vector2> uvs { get { return m_uvs; } }
        public List<Vector2> UVs => uvs.List;

        PinnedList<float> m_widths = new PinnedList<float>();
        internal PinnedList<float> widths { get { return m_widths; } }
        public List<float> Widths => widths.List;

        PinnedList<Vector3> m_velocities = new PinnedList<Vector3>();
        internal PinnedList<Vector3> velocitiesList { get { return m_velocities; } }
        public List<Vector3> Velocities => velocitiesList.List;
    }
}
