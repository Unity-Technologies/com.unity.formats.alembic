using System;
using UnityEngine;

namespace UnityEngine.Formats.Alembic.Sdk
{
    /// <summary>
    //Alembic marshals bool as 1 byte, whereas the C# default is to marshal bool as 4 bytes. This struct implements 1-byte marshalling.
    /// </summary>
    [Serializable]
    public struct Bool
    {
        [SerializeField] byte v;
        public static implicit operator bool(Bool v) { return v.v != 0; }
        public static bool ToBool(Bool v) { return v; }

        public static implicit operator Bool(bool v) { Bool r; r.v = v ? (byte)1 : (byte)0; return r; }
        public static Bool ToBool(bool v) { return v; }
    }
}
