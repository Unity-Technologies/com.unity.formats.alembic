using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    [DisallowMultipleComponent]
    public class AlembicCurves : MonoBehaviour
    {
        public delegate void OnUpdateDataHandler(AlembicCurves curves);
        public Vector3[] Positions => positionsList.GetArray();
        public int[] CurvePointCount => curvePointCount.GetArray();
        public Vector2[] UVs => uvs.GetArray();
        public float[] Widths => widths.GetArray();
        public Vector3[] Velocities => velocitiesList.GetArray();
        public event OnUpdateDataHandler OnUpdate
        {
            add => updateHandler += value;
            remove => updateHandler -= value;
        }

        internal void InvokeOnUpdate(AlembicCurves curves)
        {
            updateHandler?.Invoke(curves);
        }

        OnUpdateDataHandler updateHandler;

        internal PinnedList<Vector3> positionsList { get; } = new PinnedList<Vector3>();
        internal PinnedList<int> curvePointCount { get; } = new PinnedList<int>();
        internal PinnedList<Vector2> uvs { get; } = new PinnedList<Vector2>();
        internal PinnedList<float> widths { get; } = new PinnedList<float>();
        internal PinnedList<Vector3> velocitiesList { get; } = new PinnedList<Vector3>();
    }
}
