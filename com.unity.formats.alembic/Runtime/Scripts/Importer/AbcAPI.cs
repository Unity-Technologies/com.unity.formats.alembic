using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEngine.Formats.Alembic.Sdk
{
    internal enum aiAspectRatioMode
    {
        CurrentResolution,
        DefaultResolution,
        CameraAperture
    };

    internal enum aiNormalsMode
    {
        Import,
        CalculateIfMissing,
        AlwaysCalculate,
        None
    }

    internal enum aiTangentsMode
    {
        None,
        Calculate,
    }

    internal enum aiTopologyVariance
    {
        Constant,
        Homogeneous, // vertices are variant, topology is constant
        Heterogeneous, // both vertices and topology are variant
    }

    internal enum aiTopology
    {
        Points,
        Lines,
        Triangles,
        Quads,
    };

    internal enum aiTimeSamplingType
    {
        Uniform,
        Cyclic,
        Acyclic,
    };

    internal enum aiPropertyType
    {
        Unknown,

        // scalar types
        Bool,
        Int,
        UInt,
        Float,
        Float2,
        Float3,
        Float4,
        Float4x4,

        // array types
        BoolArray,
        IntArray,
        UIntArray,
        FloatArray,
        Float2Array,
        Float3Array,
        Float4Array,
        Float4x4Array,

        ScalarTypeBegin = Bool,
        ScalarTypeEnd = Float4x4,

        ArrayTypeBegin = BoolArray,
        ArrayTypeEnd = Float4x4Array,
    };

    [StructLayout(LayoutKind.Sequential)]
    internal struct aiConfig
    {
        public aiNormalsMode normalsMode { get; set; }
        public aiTangentsMode tangentsMode { get; set; }
        public float scaleFactor { get; set; }
        public float aspectRatio { get; set; }
        public float vertexMotionScale { get; set; }
        public int splitUnit { get; set; }
        public Bool swapHandedness { get; set; }
        public Bool flipFaces { get; set; }
        public Bool interpolateSamples { get; set; }
        public Bool asyncLoad { get; set; }
        public Bool importPointPolygon { get; set; }
        public Bool importLinePolygon { get; set; }
        public Bool importTrianglePolygon { get; set; }

        public void SetDefaults()
        {
            normalsMode = aiNormalsMode.CalculateIfMissing;
            tangentsMode = aiTangentsMode.None;
            scaleFactor = 0.01f;
            aspectRatio = -1.0f;
            vertexMotionScale = 1.0f;
#if UNITY_2017_3_OR_NEWER
            splitUnit = 0x7fffffff;
#else
            splitUnit = 65000;
#endif
            swapHandedness = true;
            flipFaces = false;
            interpolateSamples = true;
            asyncLoad = true;
            importPointPolygon = true;
            importLinePolygon = true;
            importTrianglePolygon = true;
        }
    }

    internal struct aiSampleSelector
    {
        public ulong requestedIndex { get; set; }
        public double requestedTime { get; set; }
        public int requestedTimeIndexType { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct aiMeshSummary
    {
        public aiTopologyVariance topologyVariance { get; set; }
        public Bool hasCounts { get; set; }
        public Bool hasIndsices { get; set; }
        public Bool hasPoints { get; set; }
        public Bool hasVelocities { get; set; }
        public Bool hasNormals { get; set; }
        public Bool hasTangents { get; set; }
        public Bool hasUV0 { get; set; }
        public Bool hasUV1 { get; set; }
        public Bool hasRgba { get; set; }
        public Bool hasRgb { get; set; }
        public Bool constantPoints { get; set; }
        public Bool constantVelocities { get; set; }
        public Bool constantNormals { get; set; }
        public Bool constantTangents { get; set; }
        public Bool constantUV0 { get; set; }
        public Bool constantUV1 { get; set; }
        public Bool constantRgba { get; set; }
        public Bool constantRgb { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct aiMeshSampleSummary
    {
        public Bool visibility { get; set; }

        public int splitCount { get; set; }
        public int submeshCount { get; set; }
        public int vertexCount { get; set; }
        public int indexCount { get; set; }
        public Bool topologyChanged { get; set; }
    }

    internal struct aiMeshSplitSummary
    {
        public int submeshCount { get; set; }
        public int submeshOffset { get; set; }
        public int vertexCount { get; set; }
        public int vertexOffset { get; set; }
        public int indexCount { get; set; }
        public int indexOffset { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct aiSubmeshSummary
    {
        public int splitIndex { get; set; }
        public int submeshIndex { get; set; }
        public int indexCount { get; set; }
        public aiTopology topology { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct aiPolyMeshData
    {
        public IntPtr positions;
        public IntPtr velocities;
        public IntPtr normals;
        public IntPtr tangents;
        public IntPtr uv0;
        public IntPtr uv1;
        public IntPtr rgba;
        public IntPtr rgb;
        public IntPtr indices;

        public int vertexCount;
        public int indexCount;

        public Vector3 center;
        public Vector3 extents;

        public aiPolyMeshData(
            IntPtr positions, IntPtr velocities, IntPtr normals,
            IntPtr tangents, IntPtr uv0, IntPtr uv1, IntPtr rgba,
            IntPtr rgb, IntPtr indices, int vertexCount, int indexCount,
            Vector3 center, Vector3 extents)
        {
            this.positions = positions;
            this.velocities = velocities;
            this.normals = normals;
            this.tangents = tangents;
            this.uv0 = uv0;
            this.uv1 = uv1;
            this.rgba = rgba;
            this.rgb = rgb;
            this.indices = indices;
            this.vertexCount = vertexCount;
            this.indexCount = indexCount;
            this.center = center;
            this.extents = extents;
        }
    }

    internal struct aiSubmeshData
    {
        public IntPtr indexes;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct aiXformData
    {
        public Bool visibility { get; set; }

        public Vector3 translation { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 scale { get; set; }
        public Bool inherits { get; set; }
    }


    [StructLayout(LayoutKind.Sequential)]
    internal struct aiPointsSummary
    {
        public Bool hasVelocities { get; set; }
        public Bool hasIDs { get; set; }
        public Bool constantPoints { get; set; }
        public Bool constantVelocities { get; set; }
        public Bool constantIDs { get; set; }
    };

    internal struct aiPointsSampleSummary
    {
        public int count { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct aiPointsData
    {
        public Bool visibility;

        public IntPtr points;
        public IntPtr velocities;
        public IntPtr ids;
        public int count;

        public Vector3 boundsCenter;
        public Vector3 boundsExtents;

        public aiPointsData(
            Bool visibility, IntPtr points, IntPtr velocities, IntPtr ids,
            int count, Vector3 boundsCenter, Vector3 boundsExtents)
        {
            this.visibility = visibility;
            this.points = points;
            this.velocities = velocities;
            this.ids = ids;
            this.count = count;
            this.boundsCenter = boundsCenter;
            this.boundsExtents = boundsExtents;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct aiPropertyData
    {
        public IntPtr data;
        public int size;
        public aiPropertyType type;

        public aiPropertyData(IntPtr data, int size, aiPropertyType type)
        {
            this.data = data;
            this.size = size;
            this.type = type;
        }
    }

    internal static class Abci
    {
#if UNITY_EDITOR_OSX
        internal const string Lib = "Packages/com.unity.formats.alembic/Runtime/Plugins/x86_64/abci.bundle/Contents/MacOS/abci";
#elif UNITY_EDITOR_LINUX
        internal const string Lib = "Packages/com.unity.formats.alembic/Runtime/Plugins/x86_64/abci.so";
#elif UNITY_EDITOR_WIN
        internal const string Lib = "Packages/com.unity.formats.alembic/Runtime/Plugins/x86_64/abci.dll";
#elif UNITY_STANDALONE
        internal const string Lib = "abci";
#endif
    }

    internal struct aiContext
    {
        internal IntPtr self;
        public static implicit operator bool(aiContext v) { return v.self != IntPtr.Zero; }
        public static bool ToBool(aiContext v) { return v; }

        public static aiContext Create(int uid) { return NativeMethods.aiContextCreate(uid); }

        public static void DestroyByPath(string path)
        {
            var fullPath = Path.GetFullPath(path);
            NativeMethods.aiClearContextsWithPath(fullPath);
        }

        public void Destroy() { NativeMethods.aiContextDestroy(self); self = IntPtr.Zero; }

        public bool Load(string path)
        {
            var fullPath = Path.GetFullPath(path);
            return NativeMethods.aiContextLoad(self, fullPath);
        }

        internal void SetConfig(ref aiConfig conf) { NativeMethods.aiContextSetConfig(self, ref conf); }
        public void UpdateSamples(double time) { NativeMethods.aiContextUpdateSamples(self, time); }

        internal aiObject topObject { get { return NativeMethods.aiContextGetTopObject(self); } }
        public int timeSamplingCount { get { return NativeMethods.aiContextGetTimeSamplingCount(self); } }
        public aiTimeSampling GetTimeSampling(int i) { return NativeMethods.aiContextGetTimeSampling(self, i); }
        internal void GetTimeRange(ref double begin, ref double end) { NativeMethods.aiContextGetTimeRange(self, ref begin, ref end); }
    }

    internal struct aiTimeSampling
    {
        internal IntPtr self;

        internal aiTimeSampling(IntPtr self)
        {
            this.self = self;
        }

        public int sampleCount { get { return NativeMethods.aiTimeSamplingGetSampleCount(self); } }
        public double GetTime(int index) { return NativeMethods.aiTimeSamplingGetTime(self, index); }
        internal void GetRange(ref double start, ref double end) { NativeMethods.aiTimeSamplingGetRange(self, ref start, ref end); }
    }

    internal struct aiObject
    {
        internal IntPtr self;

        internal aiObject(IntPtr self)
        {
            this.self = self;
        }

        public static implicit operator bool(aiObject v) { return v.self != IntPtr.Zero; }
        public static bool ToBool(aiObject v) { return v; }

        public aiContext context { get { return NativeMethods.aiObjectGetContext(self); } }
        public string name { get { return Marshal.PtrToStringAnsi(NativeMethods.aiObjectGetName(self)); } }
        public string fullname { get { return Marshal.PtrToStringAnsi(NativeMethods.aiObjectGetFullName(self)); } }
        public aiObject parent { get { return NativeMethods.aiObjectGetParent(self); } }

        public void SetEnabled(bool value) { NativeMethods.aiObjectSetEnabled(self, value); }
        public int childCount { get { return NativeMethods.aiObjectGetNumChildren(self); } }
        public aiObject GetChild(int i) { return NativeMethods.aiObjectGetChild(self, i); }

        internal aiXform AsXform() { return NativeMethods.aiObjectAsXform(self); }
        internal aiCamera AsCamera() { return NativeMethods.aiObjectAsCamera(self); }
        internal aiPoints AsPoints() { return NativeMethods.aiObjectAsPoints(self); }
        internal aiPolyMesh AsPolyMesh() { return NativeMethods.aiObjectAsPolyMesh(self); }

        public void EachChild(Action<aiObject> act)
        {
            if (act == null)
            {
                return;
            }
            int n = childCount;
            for (int ci = 0; ci < n; ++ci)
                act.Invoke(GetChild(ci));
        }
    }

    internal struct aiSchema
    {
        public IntPtr self;

        public aiSchema(IntPtr self)
        {
            this.self = self;
        }

        public static implicit operator bool(aiSchema v) { return v.self != IntPtr.Zero; }
        public static explicit operator aiXform(aiSchema v) { var tmp = default(aiXform); tmp.self = v.self; return tmp; }
        public static explicit operator aiCamera(aiSchema v) { var tmp = default(aiCamera); tmp.self = v.self; return tmp; }
        public static explicit operator aiPolyMesh(aiSchema v) { var tmp = default(aiPolyMesh); tmp.self = v.self; return tmp; }
        public static explicit operator aiPoints(aiSchema v) { var tmp = default(aiPoints); tmp.self = v.self; return tmp; }

        public bool isConstant { get { return NativeMethods.aiSchemaIsConstant(self); } }
        public bool isDataUpdated { get { NativeMethods.aiSchemaSync(self); return NativeMethods.aiSchemaIsDataUpdated(self); } }
        internal aiSample sample { get { return NativeMethods.aiSchemaGetSample(self); } }

        public void UpdateSample(ref aiSampleSelector ss) { NativeMethods.aiSchemaUpdateSample(self, ref ss); }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct aiXform
    {
        [FieldOffset(0)] public IntPtr self;
        [FieldOffset(0)] public aiSchema schema;
        public static implicit operator bool(aiXform v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSchema(aiXform v) { return v.schema; }

        public aiXformSample sample { get { return NativeMethods.aiXform.aiSchemaGetSample(self); } }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct aiCamera
    {
        [FieldOffset(0)] public IntPtr self;
        [FieldOffset(0)] public aiSchema schema;
        public static implicit operator bool(aiCamera v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSchema(aiCamera v) { return v.schema; }

        public aiCameraSample sample { get { return NativeMethods.aiCamera.aiSchemaGetSample(self); } }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct aiPolyMesh
    {
        [FieldOffset(0)] public IntPtr self;
        [FieldOffset(0)] public aiSchema schema;
        public static implicit operator bool(aiPolyMesh v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSchema(aiPolyMesh v) { return v.schema; }

        public aiPolyMeshSample sample { get { return NativeMethods.aiPolyMesh.aiSchemaGetSample(self); } }
        public void GetSummary(ref aiMeshSummary dst) { NativeMethods.aiPolyMeshGetSummary(self, ref dst); }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct aiPoints
    {
        [FieldOffset(0)] public IntPtr self;
        [FieldOffset(0)] public aiSchema schema;
        public static implicit operator bool(aiPoints v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSchema(aiPoints v) { return v.schema; }

        internal aiPointsSample sample { get { return NativeMethods.aiPoints.aiSchemaGetSample(self); } }
        public bool sort { set { NativeMethods.aiPointsSetSort(self, value); } }
        public Vector3 sortBasePosition { set { NativeMethods.aiPointsSetSortBasePosition(self, value); } }

        public void GetSummary(ref aiPointsSummary dst) { NativeMethods.aiPointsGetSummary(self, ref dst); }
    }


    internal struct aiSample
    {
        public IntPtr self;
        public static implicit operator bool(aiSample v) { return v.self != IntPtr.Zero; }
        public static explicit operator aiXformSample(aiSample v) { aiXformSample tmp; tmp.self = v.self; return tmp; }
        public static explicit operator aiCameraSample(aiSample v) { aiCameraSample tmp; tmp.self = v.self; return tmp; }
        public static explicit operator aiPolyMeshSample(aiSample v) { aiPolyMeshSample tmp; tmp.self = v.self; return tmp; }
        public static explicit operator aiPointsSample(aiSample v) { aiPointsSample tmp; tmp.self = v.self; return tmp; }
    }

    internal struct aiXformSample
    {
        public IntPtr self;
        public static implicit operator bool(aiXformSample v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSample(aiXformSample v) { aiSample tmp; tmp.self = v.self; return tmp; }

        public void GetData(ref aiXformData dst) { NativeMethods.aiXformGetData(self, ref dst); }
    }

    internal struct aiCameraSample
    {
        public IntPtr self;
        public static implicit operator bool(aiCameraSample v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSample(aiCameraSample v) { aiSample tmp; tmp.self = v.self; return tmp; }

        public void GetData(ref CameraData dst) { NativeMethods.aiCameraGetData(self, ref dst); }
    }

    internal struct aiPolyMeshSample
    {
        public IntPtr self;
        public static implicit operator bool(aiPolyMeshSample v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSample(aiPolyMeshSample v) { aiSample tmp; tmp.self = v.self; return tmp; }

        public void GetSummary(ref aiMeshSampleSummary dst) { NativeMethods.aiPolyMeshGetSampleSummary(self, ref dst); }
        public void GetSplitSummaries(PinnedList<aiMeshSplitSummary> dst) { NativeMethods.aiPolyMeshGetSplitSummaries(self, dst); }
        public void GetSubmeshSummaries(PinnedList<aiSubmeshSummary> dst) { NativeMethods.aiPolyMeshGetSubmeshSummaries(self, dst); }
        internal void FillVertexBuffer(PinnedList<aiPolyMeshData> vbs, PinnedList<aiSubmeshData> ibs) { NativeMethods.aiPolyMeshFillVertexBuffer(self, vbs, ibs); }
        public void Sync() { NativeMethods.aiSampleSync(self); }
    }

    internal struct aiPointsSample
    {
        public IntPtr self;
        public static implicit operator bool(aiPointsSample v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSample(aiPointsSample v) { aiSample tmp; tmp.self = v.self; return tmp; }

        public void GetSummary(ref aiPointsSampleSummary dst) { NativeMethods.aiPointsGetSampleSummary(self, ref dst); }
        public void FillData(PinnedList<aiPointsData> dst) { NativeMethods.aiPointsFillData(self, dst); }
        public void Sync() { NativeMethods.aiSampleSync(self); }
    }


    internal struct aiProperty
    {
        public IntPtr self;

        public aiProperty(IntPtr self)
        {
            this.self = self;
        }

        public static implicit operator bool(aiProperty v) { return v.self != IntPtr.Zero; }
        public static bool ToBool(aiProperty v) { return v; }
    }
}
