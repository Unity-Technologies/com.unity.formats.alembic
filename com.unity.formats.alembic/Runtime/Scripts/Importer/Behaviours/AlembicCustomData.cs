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
        }

        public List<V2FAttribute> VertexAttributes;

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
