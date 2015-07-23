using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

[ExecuteInEditMode]
public class AlembicMesh : MonoBehaviour
{
    [Serializable]
    public class Split
    {
        public Vector3[] positionCache;
        public Vector3[] normalCache;
        public Vector2[] uvCache;
        public Vector4[] tangentCache;
        public Mesh mesh;
        public GameObject host;
    }

    [Serializable]
    public class Submesh
    {
        public int[] indexCache;
        public int facesetIndex;
        public int splitIndex;
    }

    public AlembicImporter.aiFaceWindingOverride m_faceWinding = AlembicImporter.aiFaceWindingOverride.InheritStreamSetting;
    public AlembicImporter.aiNormalsModeOverride m_normalsMode = AlembicImporter.aiNormalsModeOverride.InheritStreamSetting;
    public AlembicImporter.aiTangentsModeOverride m_tangentsMode = AlembicImporter.aiTangentsModeOverride.InheritStreamSetting;
    public bool m_cacheTangentsSplits = true;
    
    [HideInInspector] public bool hasFacesets = false;
    [HideInInspector] public List<Submesh> m_submeshes = new List<Submesh>();
    [HideInInspector] public List<Split> m_splits = new List<Split>();
}
