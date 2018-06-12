using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{
    public enum aiAspectRatioMode
    {
        CurrentResolution = 0,
        DefaultResolution = 1,
        CameraAperture
    };

    public enum aiNormalsMode
    {
        ReadFromFile = 0,
        ComputeIfMissing,
        AlwaysCompute,
        Ignore
    }

    public enum aiTangentsMode
    {
        None = 0,
        Compute,
    }

    public enum aiTopologyVariance
    {
        Constant,
        Homogeneous, // vertices are variant, topology is constant
        Heterogeneous, // both vertices and topology are variant
    }

    public enum aiTopology
    {
        Points,
        Lines,
        Triangles,
        Quads,
    };

    public enum aiTimeSamplingType
    {
        Uniform,
        Cyclic,
        Acyclic,
    };

    public enum aiPropertyType
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

    public struct aiConfig
    {
        public aiNormalsMode normalsMode { get; set; }
        public aiTangentsMode tangentsMode { get; set; }
        public float scaleFactor { get; set; }
        public float aspectRatio { get; set; }
        public float vertexMotionScale { get; set; }
        public int splitUnit { get; set; }
        public Bool swapHandedness { get; set; }
        public Bool swapFaceWinding { get; set; }
        public Bool interpolateSamples { get; set; }
        public Bool turnQuadEdges { get; set; }
        public Bool asyncLoad { get; set; }
        public Bool importPointPolygon { get; set; }
        public Bool importLinePolygon { get; set; }
        public Bool importTrianglePolygon { get; set; }

        public void SetDefaults()
        {
            normalsMode = aiNormalsMode.ComputeIfMissing;
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
            swapFaceWinding = false;
            interpolateSamples = true;
            turnQuadEdges = false;
            asyncLoad = true;
            importPointPolygon = true;
            importLinePolygon = true;
            importTrianglePolygon = true;
        }
    }

    public struct aiSampleSelector
    {
        public ulong requestedIndex { get; set; }
        public double requestedTime { get; set; }
        public int requestedTimeIndexType { get; set; }
    }

    public struct aiMeshSummary
    {
        public aiTopologyVariance topologyVariance { get; set; }
        public Bool hasVelocities { get; set; }
        public Bool hasNormals { get; set; }
        public Bool hasTangents { get; set; }
        public Bool hasUV0 { get; set; }
        public Bool hasUV1 { get; set; }
        public Bool hasColors { get; set; }
        public Bool constantPoints { get; set; }
        public Bool constantVelocities { get; set; }
        public Bool constantNormals { get; set; }
        public Bool constantTangents { get; set; }
        public Bool constantUV0 { get; set; }
        public Bool constantUV1 { get; set; }
        public Bool constantColors { get; set; }
    }

    public struct aiMeshSampleSummary
    {
        public Bool visibility { get; set; }

        public int splitCount { get; set; }
        public int submeshCount { get; set; }
        public int vertexCount { get; set; }
        public int indexCount { get; set; }
        public Bool topologyChanged { get; set; }
    }

    public struct aiMeshSplitSummary
    {
        public int submeshCount { get; set; }
        public int submeshOffset { get; set; }
        public int vertexCount { get; set; }
        public int vertexOffset { get; set; }
        public int indexCount { get; set; }
        public int indexOffset { get; set; }
    }

    public struct aiSubmeshSummary
    {
        public int splitIndex { get; set; }
        public int submeshIndex { get; set; }
        public int indexCount { get; set; }
        public aiTopology topology { get; set; }
    }

    internal struct aiPolyMeshData
    {
        public IntPtr positions;
        public IntPtr velocities;
        public IntPtr normals;
        public IntPtr tangents;
        public IntPtr uv0;
        public IntPtr uv1;
        public IntPtr colors;
        public IntPtr indices;

        public int vertexCount;
        public int indexCount;

        public Vector3 center;
        public Vector3 extents;
    }

    internal struct aiSubmeshData
    {
        public IntPtr indexes;
    }

    public struct aiXformData
    {
        public Bool visibility { get; set; }

        public Vector3 translation { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 scale { get; set; }
        public Bool inherits { get; set; }
    }

    public struct aiCameraData
    {
        public Bool visibility { get; set; }

        public float nearClippingPlane { get; set; }
        public float farClippingPlane { get; set; }
        public float fieldOfView { get; set; }   // in degree. vertical one
        public float aspectRatio { get; set; }

        public float focusDistance { get; set; } // in cm
        public float focalLength { get; set; }   // in mm
        public float aperture { get; set; }      // in cm. vertical one
    }

    public struct aiPointsSummary
    {
        public Bool hasVelocities { get; set; }
        public Bool hasIDs { get; set; }
        public Bool constantPoints { get; set; }
        public Bool constantVelocities { get; set; }
        public Bool constantIDs { get; set; }
    };

    public struct aiPointsSampleSummary
    {
        public int count { get; set; }
    }

    internal struct aiPointsData
    {
        public Bool visibility;

        public IntPtr points;
        public IntPtr velocities;
        public IntPtr ids;
        public int count;

        public Vector3 boundsCenter;
        public Vector3 boundsExtents;
    }

    public struct aiPropertyData
    {
        public IntPtr data;
        public int size;
        aiPropertyType type;
    }

    internal static class Abci {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        internal const string Lib = "Packages/com.unity.formats.alembic/Runtime/Plugins/x86_64/abci.bundle/Contents/MacOS/abci";
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        internal const string Lib = "Packages/com.unity.formats.alembic/Runtime/Plugins/x86_64/libabci.so";
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        internal const string Lib = "Packages/com.unity.formats.alembic/Runtime/Plugins/x86_64/abci.dll";
#endif
    }

    public struct aiContext
    {
        public IntPtr self;
        public static implicit operator bool(aiContext v) { return v.self != IntPtr.Zero; }

        public static aiContext Create(int uid) { return aiContextCreate(uid); }
        public static void DestroyByPath(string path) { aiClearContextsWithPath(path); }

        public void Destroy() { aiContextDestroy(self); self = IntPtr.Zero; }
        public bool Load(string path) { return aiContextLoad(self, path); }
        public void SetConfig(ref aiConfig conf) { aiContextSetConfig(self, ref conf); }
        public void UpdateSamples(double time) { aiContextUpdateSamples(self, time); }

        internal aiObject topObject { get { return aiContextGetTopObject(self); } }
        public int timeSamplingCount { get { return aiContextGetTimeSamplingCount(self); } }
        public aiTimeSampling GetTimeSampling(int i) { return aiContextGetTimeSampling(self, i); }
        public void GetTimeRange(ref double begin, ref double end) { aiContextGetTimeRange(self, ref begin, ref end); }

        #region internal
        [DllImport(Abci.Lib)] public static extern  void aiClearContextsWithPath(string path);
        [DllImport(Abci.Lib)] public static extern aiContext aiContextCreate(int uid);
        [DllImport(Abci.Lib)] public static extern void aiContextDestroy(IntPtr ctx);
        [DllImport(Abci.Lib)] public static extern Bool aiContextLoad(IntPtr ctx, string path);
        [DllImport(Abci.Lib)] public static extern void aiContextSetConfig(IntPtr ctx, ref aiConfig conf);
        [DllImport(Abci.Lib)] public static extern int aiContextGetTimeSamplingCount(IntPtr ctx);
        [DllImport(Abci.Lib)] public static extern aiTimeSampling aiContextGetTimeSampling(IntPtr ctx, int i);
        [DllImport(Abci.Lib)] public static extern void aiContextGetTimeRange(IntPtr ctx, ref double begin, ref double end);
        [DllImport(Abci.Lib)] public static extern aiObject aiContextGetTopObject(IntPtr ctx);
        [DllImport(Abci.Lib)] public static extern void aiContextUpdateSamples(IntPtr ctx, double time);
        #endregion
    }

    public struct aiTimeSampling
    {
        public IntPtr self;

        public int sampleCount { get { return aiTimeSamplingGetSampleCount(self); } }
        public double GetTime(int index) { return aiTimeSamplingGetTime(self, index); }
        public void GetRange(ref double start, ref double end) { aiTimeSamplingGetRange(self, ref start, ref end); }

        #region internal
        [DllImport(Abci.Lib)] static extern int aiTimeSamplingGetSampleCount(IntPtr self);
        [DllImport(Abci.Lib)] static extern double aiTimeSamplingGetTime(IntPtr self, int index);
        [DllImport(Abci.Lib)] static extern void aiTimeSamplingGetRange(IntPtr self, ref double start, ref double end);
        #endregion
    }

    public struct aiObject
    {
        public IntPtr self;
        public static implicit operator bool(aiObject v) { return v.self != IntPtr.Zero; }

        public string name { get { return Marshal.PtrToStringAnsi(aiObjectGetName(self)); } }
        public string fullname { get { return Marshal.PtrToStringAnsi(aiObjectGetFullName(self)); } }
        public bool enabled { set { aiObjectSetEnabled(self, value); } }
        public int childCount { get { return aiObjectGetNumChildren(self); } }
        public aiObject GetChild(int i) { return aiObjectGetChild(self, i); }

        internal aiXform AsXform() { return aiObjectAsXform(self); }
        internal aiCamera AsCamera() { return aiObjectAsCamera(self); }
        internal aiPoints AsPoints() { return aiObjectAsPoints(self); }
        internal aiPolyMesh AsPolyMesh() { return aiObjectAsPolyMesh(self); }

        public void EachChild(Action<aiObject> act)
        {
            int n = childCount;
            for (int ci = 0; ci < n; ++ci)
                act.Invoke(GetChild(ci));
        }

        #region internal
        [DllImport(Abci.Lib)] static extern int aiObjectGetNumChildren(IntPtr obj);
        [DllImport(Abci.Lib)] static extern aiObject aiObjectGetChild(IntPtr obj, int i);
        [DllImport(Abci.Lib)] static extern void aiObjectSetEnabled(IntPtr obj, Bool v);
        [DllImport(Abci.Lib)] static extern IntPtr aiObjectGetName(IntPtr obj);
        [DllImport(Abci.Lib)] static extern IntPtr aiObjectGetFullName(IntPtr obj);
        [DllImport(Abci.Lib)] static extern aiXform aiObjectAsXform(IntPtr obj);
        [DllImport(Abci.Lib)] static extern aiCamera aiObjectAsCamera(IntPtr obj);
        [DllImport(Abci.Lib)] static extern aiPoints aiObjectAsPoints(IntPtr obj);
        [DllImport(Abci.Lib)] static extern aiPolyMesh aiObjectAsPolyMesh(IntPtr obj);
        #endregion
    }

    internal struct aiSchema
    {
        public IntPtr self;
        public static implicit operator bool(aiSchema v) { return v.self != IntPtr.Zero; }
        public static explicit operator aiXform(aiSchema v) { var tmp = default(aiXform); tmp.self = v.self; return tmp; }
        public static explicit operator aiCamera(aiSchema v) { var tmp = default(aiCamera); tmp.self = v.self; return tmp; }
        public static explicit operator aiPolyMesh(aiSchema v) { var tmp = default(aiPolyMesh); tmp.self = v.self; return tmp; }
        public static explicit operator aiPoints(aiSchema v) { var tmp = default(aiPoints); tmp.self = v.self; return tmp; }

        public bool isConstant { get { return aiSchemaIsConstant(self); } }
        public bool isDataUpdated { get { aiSchemaSync(self); return aiSchemaIsDataUpdated(self); } }
        internal aiSample sample { get { return aiSchemaGetSample(self); } }

        public void UpdateSample(ref aiSampleSelector ss) { aiSchemaUpdateSample(self, ref ss); }

        #region internal
        [DllImport(Abci.Lib)] static extern void aiSchemaUpdateSample(IntPtr schema, ref aiSampleSelector ss);
        [DllImport(Abci.Lib)] static extern void aiSchemaSync(IntPtr schema);
        [DllImport(Abci.Lib)] static extern aiSample aiSchemaGetSample(IntPtr schema);
        [DllImport(Abci.Lib)] static extern Bool aiSchemaIsConstant(IntPtr schema);
        [DllImport(Abci.Lib)] static extern Bool aiSchemaIsDataUpdated(IntPtr schema);
        [DllImport(Abci.Lib)] static extern int aiSchemaGetNumProperties(IntPtr schema);
        [DllImport(Abci.Lib)] static extern aiProperty aiSchemaGetPropertyByIndex(IntPtr schema, int i);
        [DllImport(Abci.Lib)] static extern aiProperty aiSchemaGetPropertyByName(IntPtr schema, string name);
        #endregion
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct aiXform
    {
        [FieldOffset(0)] public IntPtr self;
        [FieldOffset(0)] public aiSchema schema;
        public static implicit operator bool(aiXform v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSchema(aiXform v) { return v.schema; }

        public aiXformSample sample { get { return aiSchemaGetSample(self); } }

        #region internal
        [DllImport(Abci.Lib)] static extern aiXformSample aiSchemaGetSample(IntPtr schema);
        #endregion
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct aiCamera
    {
        [FieldOffset(0)] public IntPtr self;
        [FieldOffset(0)] public aiSchema schema;
        public static implicit operator bool(aiCamera v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSchema(aiCamera v) { return v.schema; }

        public aiCameraSample sample { get { return aiSchemaGetSample(self); } }

        #region internal
        [DllImport(Abci.Lib)] static extern aiCameraSample aiSchemaGetSample(IntPtr schema);
        #endregion
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct aiPolyMesh
    {
        [FieldOffset(0)] public IntPtr self;
        [FieldOffset(0)] public aiSchema schema;
        public static implicit operator bool(aiPolyMesh v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSchema(aiPolyMesh v) { return v.schema; }

        public aiPolyMeshSample sample { get { return aiSchemaGetSample(self); } }
        public void GetSummary(ref aiMeshSummary dst) { aiPolyMeshGetSummary(self, ref dst); }

        #region internal
        [DllImport(Abci.Lib)] static extern void aiPolyMeshGetSummary(IntPtr schema, ref aiMeshSummary dst);
        [DllImport(Abci.Lib)] static extern aiPolyMeshSample aiSchemaGetSample(IntPtr schema);
        #endregion
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct aiPoints
    {
        [FieldOffset(0)] public IntPtr self;
        [FieldOffset(0)] public aiSchema schema;
        public static implicit operator bool(aiPoints v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSchema(aiPoints v) { return v.schema; }

        internal aiPointsSample sample { get { return aiSchemaGetSample(self); } }
        public bool sort { set { aiPointsSetSort(self, value); } }
        public Vector3 sortBasePosition { set { aiPointsSetSortBasePosition(self, value); } }

        public void GetSummary(ref aiPointsSummary dst) { aiPointsGetSummary(self, ref dst); }

        #region internal
        [DllImport(Abci.Lib)] static extern aiPointsSample aiSchemaGetSample(IntPtr schema);
        [DllImport(Abci.Lib)] static extern void aiPointsSetSort(IntPtr schema, Bool v);
        [DllImport(Abci.Lib)] static extern void aiPointsSetSortBasePosition(IntPtr schema, Vector3 v);
        [DllImport(Abci.Lib)] static extern void aiPointsGetSummary(IntPtr schema, ref aiPointsSummary dst);
        #endregion
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

        public void GetData(ref aiXformData dst) { aiXformGetData(self, ref dst); }

        #region internal
        [DllImport(Abci.Lib)] public static extern void aiXformGetData(IntPtr sample, ref aiXformData data);
        #endregion
    }

    internal struct aiCameraSample
    {
        public IntPtr self;
        public static implicit operator bool(aiCameraSample v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSample(aiCameraSample v) { aiSample tmp; tmp.self = v.self; return tmp; }

        public void GetData(ref aiCameraData dst) { aiCameraGetData(self, ref dst); }

        #region internal
        [DllImport(Abci.Lib)] public static extern void aiCameraGetData(IntPtr sample, ref aiCameraData dst);
        #endregion
    }

    internal struct aiPolyMeshSample
    {
        public IntPtr self;
        public static implicit operator bool(aiPolyMeshSample v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSample(aiPolyMeshSample v) { aiSample tmp; tmp.self = v.self; return tmp; }

        public void GetSummary(ref aiMeshSampleSummary dst) { aiPolyMeshGetSampleSummary(self, ref dst); }
        public void GetSplitSummaries(PinnedList<aiMeshSplitSummary> dst) { aiPolyMeshGetSplitSummaries(self, dst); }
        public void GetSubmeshSummaries(PinnedList<aiSubmeshSummary> dst) { aiPolyMeshGetSubmeshSummaries(self, dst); }
        internal void FillVertexBuffer(PinnedList<aiPolyMeshData> vbs, PinnedList<aiSubmeshData> ibs) { aiPolyMeshFillVertexBuffer(self, vbs, ibs); }
        public void Sync() { aiSampleSync(self); }

        #region internal
        [DllImport(Abci.Lib)] static extern void aiPolyMeshGetSampleSummary(IntPtr sample, ref aiMeshSampleSummary dst);
        [DllImport(Abci.Lib)] static extern int aiPolyMeshGetSplitSummaries(IntPtr sample, IntPtr dst);
        [DllImport(Abci.Lib)] static extern void aiPolyMeshGetSubmeshSummaries(IntPtr sample, IntPtr dst);
        [DllImport(Abci.Lib)] static extern void aiPolyMeshFillVertexBuffer(IntPtr sample, IntPtr vbs, IntPtr ibs);
        [DllImport(Abci.Lib)] static extern void aiSampleSync(IntPtr sample);
        #endregion
    }

    internal struct aiPointsSample
    {
        public IntPtr self;
        public static implicit operator bool(aiPointsSample v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSample(aiPointsSample v) { aiSample tmp; tmp.self = v.self; return tmp; }

        public void GetSummary(ref aiPointsSampleSummary dst) { aiPointsGetSampleSummary(self, ref dst); }
        public void FillData(PinnedList<aiPointsData> dst) { aiPointsFillData(self, dst); }
        public void Sync() { aiSampleSync(self); }

        #region internal
        [DllImport(Abci.Lib)] static extern void aiPointsGetSampleSummary(IntPtr sample, ref aiPointsSampleSummary dst);
        [DllImport(Abci.Lib)] static extern void aiPointsFillData(IntPtr sample, IntPtr dst);
        [DllImport(Abci.Lib)] static extern void aiSampleSync(IntPtr sample);
        #endregion
    }


    public struct aiProperty
    {
        public IntPtr self;
        public static implicit operator bool(aiProperty v) { return v.self != IntPtr.Zero; }

        #region internal
        [DllImport(Abci.Lib)] static extern IntPtr aiPropertyGetName(IntPtr prop);
        [DllImport(Abci.Lib)] static extern aiPropertyType aiPropertyGetType(IntPtr prop);
        [DllImport(Abci.Lib)] static extern void aiPropertyGetData(IntPtr prop, aiPropertyData oData);
        #endregion
    }

    public partial class AbcAPI
    {
        [DllImport(Abci.Lib)] public static extern aiSampleSelector aiTimeToSampleSelector(double time);
        [DllImport(Abci.Lib)] public static extern void aiCleanup();
    }
}
