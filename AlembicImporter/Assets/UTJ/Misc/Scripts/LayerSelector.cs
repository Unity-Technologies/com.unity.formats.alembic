using System;
using UnityEngine;

namespace UTJ
{
    [Serializable]
    public struct LayerSelector
    {
        [SerializeField]
        int v;
        public static implicit operator int(LayerSelector v) { return v.v; }
        public static implicit operator LayerSelector(int v) { LayerSelector r; r.v = v; return r; }
    }
}