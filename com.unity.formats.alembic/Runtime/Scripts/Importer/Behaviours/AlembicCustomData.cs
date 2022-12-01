using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.Formats.Alembic.Importer
{
    /// <summary>
    /// Class that stores additional data for the current Alembic Node.
    /// </summary>
    public class AlembicCustomData : MonoBehaviour
    {
        [SerializeField]
        List<string> faceSetNames;

        /// <summary>
        /// Retrieves the set of Face Set names.
        /// </summary>
        public List<string> FaceSetNames => faceSetNames;

        public struct V2FAttribute
        {
            public FixedString128Bytes Name;
            public NativeArray<Vector2> Data;

            public V2FAttribute(FixedString128Bytes name, NativeArray<Vector2> data)
            {
                Name = name;
                Data = new NativeArray<Vector2>(data.Length, Allocator.Persistent);
                Data.CopyFrom(data);
            }
        }

        public List<V2FAttribute> VertexAttributes = new();

        void OnDisable()
        {
            ClearCustomAttributes();
        }

        internal void ClearCustomAttributes()
        {
            foreach (var attribute in VertexAttributes)
            {
                var v2FAttribute = attribute;
                v2FAttribute.Data.DisposeIfPossible();
            }

            VertexAttributes.Clear();
        }

        internal void SetFacesetNames(List<string> names)
        {
            faceSetNames = names;
            for (var i = 0; i < faceSetNames.Count; i++)
            {
                faceSetNames[i] = faceSetNames[i].TrimEnd('\0');
            }
        }
    }
}
