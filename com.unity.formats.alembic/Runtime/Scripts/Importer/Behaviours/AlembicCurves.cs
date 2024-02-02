using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    /// <summary>
    /// The AlembicCurves component stores the curve information for the associated Alembic tree Node.
    /// </summary>
    [DisallowMultipleComponent]
    public class AlembicCurves : MonoBehaviour
    {
        /// <summary>
        /// Defines the type for the update callback.
        /// </summary>
        /// <param name="curves">the component that was updated.</param>
        public delegate void OnUpdateDataHandler(AlembicCurves curves);
        /// <summary>
        /// Returns the positions for all the curves in the current Alembic node.
        /// </summary>
        public Vector3[] Positions => positionsList.GetArray();
        /// <summary>
        /// Returns an array where: the i-th entry returns the starting index of the i-th curve ( the i-th curve is between [CurveOffsets[i]..CurveOffsets[i+1]-1) ).
        /// </summary>
        public int[] CurveOffsets => curveOffsets.GetArray();
        /// <summary>
        /// Returns an array of UVs (optional), if the imported file contained UVs.
        /// </summary>
        public Vector2[] UVs => uvs.GetArray();
        /// <summary>
        /// Returns an array of Widths (optional), if the imported file contained widths/
        /// </summary>
        public float[] Widths => widths.GetArray();
        /// <summary>
        /// Returns an array of Velocities (optional), if the imported file contained UVs.
        /// </summary>
        public Vector3[] Velocities => velocitiesList.GetArray();

        public AttributeData[] attributes => attributesList.GetArray();

        public aiAttributesSummary[] attributesSummary => attributesSummaryList.GetArray();

        /// <summary>
        /// Is an event that is invoked every time the data in the component is updated by the AlembicStreamPlayer. This is caused by the evaluation time or import options changing.
        /// This allows users to perform data post-processing only when needed.
        /// </summary>
        public event OnUpdateDataHandler OnUpdateData
        {
            add => update += value;
            remove => update -= value;
        }

        internal void InvokeOnUpdate(AlembicCurves curves)
        {
            update?.Invoke(curves);
        }

        OnUpdateDataHandler update;

        internal PinnedList<Vector3> positionsList { get; } = new PinnedList<Vector3>();
        internal PinnedList<int> curveOffsets { get; } = new PinnedList<int>();
        internal PinnedList<Vector2> uvs { get; } = new PinnedList<Vector2>();
        internal PinnedList<float> widths { get; } = new PinnedList<float>();
        internal PinnedList<Vector3> velocitiesList { get; } = new PinnedList<Vector3>();
        internal PinnedList<AttributeData> attributesList { get; }   = new PinnedList<AttributeData>();
        internal PinnedList<aiAttributesSummary> attributesSummaryList { get; }   = new PinnedList<aiAttributesSummary>();


        public List<T> GetAttributeWithName<T>(string name) where T : unmanaged
        {
            List<T> col = new List<T>();
            for (int j = 0; j < attributes.Length; j++)
            {
                if(Marshal.PtrToStringAnsi(attributesSummary[j].name) == name)
                    for (int i = 0; i < attributes[j].length; i++)
                    {
                        unsafe
                        {
                            if (attributes[j].data == null) break;
                            col.Add(*(((T*)(attributes[0].data)) + i));
                        }
                    }
            }
            return col;
        }

        // has attribute with name : return boolean
        public bool HasAttributeWithName(string name)
        {
            for (int j = 0; j < attributes.Length; j++)
            {
                if (Marshal.PtrToStringAnsi(attributesSummary[j].name) == name)
                    return true;
            }
            return false;
        }

        public bool getAttributeType(string name, out aiPropertyType type)
        {
            bool found = false;
            type = aiPropertyType.Unknown;
            for (int j = 0; j < attributes.Length; j++)
            {
                if (Marshal.PtrToStringAnsi(attributesSummary[j].name) == name)
                {
                    type = attributes[j].type1;
                    found = true;
                }
            }

            // or just return unknown?
            return found;
        }


        public bool getAttributeSize(string name, out ulong size)
        {
            bool found = false;
            size = 0;
            for (int j = 0; j < attributes.Length; j++)
            {
                if (Marshal.PtrToStringAnsi(attributesSummary[j].name) == name)
                {
                    size = attributesSummary[j].size * (ulong)attributes[j].length;
                    found = true;
                }
            }
           // or just return 0?
            return found;
        }

        public int GetAttributeCount()
        {
            return attributesList.Count;
        }
    }
}
