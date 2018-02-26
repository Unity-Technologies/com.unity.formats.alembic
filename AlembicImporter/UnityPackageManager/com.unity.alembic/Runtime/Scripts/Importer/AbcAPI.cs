using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{
    public enum aiAspectRatioMode
    {
        CurrentResolution,
        DefaultResolution,
        CameraAperture
    };

    public enum aiNormalsMode
    {
        Import,
        CalculateIfMissing,
        AlwaysCalculate,
        None
    }

    public enum aiTangentsMode
    {
        None,
        Calculate,
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
        public aiNormalsMode normalsMode;
        public aiTangentsMode tangentsMode;
        public float scaleFactor;
        public float aspectRatio;
        public float vertexMotionScale;
        public int splitUnit;
        public Bool swapHandedness;
        public Bool flipFaces;
        public Bool interpolateSamples;
        public Bool turnQuadEdges;
        public Bool asyncLoad;
        public Bool importPointPolygon;
        public Bool importLinePolygon;
        public Bool importTrianglePolygon;

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
            turnQuadEdges = false;
            asyncLoad = true;
            importPointPolygon = true;
            importLinePolygon = true;
            importTrianglePolygon = true;
        }
    }

    public struct aiSampleSelector
    {
        public ulong requestedIndex;
        public double requestedTime;
        public int requestedTimeIndexType;
    }

    public struct aiMeshSummary
    {
        public aiTopologyVariance topologyVariance;
        public Bool hasVelocities;
        public Bool hasNormals;
        public Bool hasTangents;
        public Bool hasUV0;
        public Bool hasUV1;
        public Bool hasColors;
        public Bool constantPoints;
        public Bool constantVelocities;
        public Bool constantNormals;
        public Bool constantTangents;
        public Bool constantUV0;
        public Bool constantUV1;
        public Bool constantColors;
    }

    public struct aiMeshSampleSummary
    {
        public Bool visibility;

        public int splitCount;
        public int submeshCount;
        public int vertexCount;
        public int indexCount;
        public Bool topologyChanged;
    }

    public struct aiMeshSplitSummary
    {
        public int submeshCount;
        public int submeshOffset;
        public int vertexCount;
        public int vertexOffset;
        public int indexCount;
        public int indexOffset;
    }

    public struct aiSubmeshSummary
    {
        public int splitIndex;
        public int submeshIndex;
        public int indexCount;
        public aiTopology topology;
    }

    public struct aiPolyMeshData
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

    public struct aiSubmeshData
    {
        public IntPtr indices;
    }

    public struct aiXformData
    {
        public Bool visibility;

        public Vector3 translation;
        public Quaternion rotation;
        public Vector3 scale;
        public Bool inherits;
    }

    public struct aiCameraData
    {
        public Bool visibility;

        public float nearClippingPlane;
        public float farClippingPlane;
        public float fieldOfView;   // in degree. vertical one
        public float aspectRatio;

        public float focusDistance; // in cm
        public float focalLength;   // in mm
        public float aperture;      // in cm. vertical one
    }

    public struct aiPointsSummary
    {
        public Bool hasVelocities;
        public Bool hasIDs;
        public Bool constantPoints;
        public Bool constantVelocities;
        public Bool constantIDs;
    };

    public struct aiPointsSampleSummary
    {
        public int count;
    }

    public struct aiPointsData
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

        public aiObject topObject { get { return aiContextGetTopObject(self); } }
        public int timeSamplingCount { get { return aiContextGetTimeSamplingCount(self); } }
        public aiTimeSampling GetTimeSampling(int i) { return aiContextGetTimeSampling(self, i); }
        public void GetTimeRange(ref double begin, ref double end) { aiContextGetTimeRange(self, ref begin, ref end); }

        #region internal
        [DllImport("abci")] public static extern void aiClearContextsWithPath(string path);
        [DllImport("abci")] public static extern aiContext aiContextCreate(int uid);
        [DllImport("abci")] public static extern void aiContextDestroy(IntPtr ctx);
        [DllImport("abci")] static extern Bool aiContextLoad(IntPtr ctx, string path);
        [DllImport("abci")] static extern void aiContextSetConfig(IntPtr ctx, ref aiConfig conf);
        [DllImport("abci")] static extern int aiContextGetTimeSamplingCount(IntPtr ctx);
        [DllImport("abci")] static extern aiTimeSampling aiContextGetTimeSampling(IntPtr ctx, int i);
        [DllImport("abci")] static extern void aiContextGetTimeRange(IntPtr ctx, ref double begin, ref double end);
        [DllImport("abci")] static extern aiObject aiContextGetTopObject(IntPtr ctx);
        [DllImport("abci")] static extern void aiContextUpdateSamples(IntPtr ctx, double time);
        #endregion
    }

    public struct aiTimeSampling
    {
        public IntPtr self;

        public int sampleCount { get { return aiTimeSamplingGetSampleCount(self); } }
        public double GetTime(int index) { return aiTimeSamplingGetTime(self, index); }
        public void GetRange(ref double start, ref double end) { aiTimeSamplingGetRange(self, ref start, ref end); }

        #region internal
        [DllImport("abci")] static extern int aiTimeSamplingGetSampleCount(IntPtr self);
        [DllImport("abci")] static extern double aiTimeSamplingGetTime(IntPtr self, int index);
        [DllImport("abci")] static extern void aiTimeSamplingGetRange(IntPtr self, ref double start, ref double end);
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

        public aiXform AsXform() { return aiObjectAsXform(self); }
        public aiCamera AsCamera() { return aiObjectAsCamera(self); }
        public aiPoints AsPoints() { return aiObjectAsPoints(self); }
        public aiPolyMesh AsPolyMesh() { return aiObjectAsPolyMesh(self); }

        public void EachChild(Action<aiObject> act)
        {
            int n = childCount;
            for (int ci = 0; ci < n; ++ci)
                act.Invoke(GetChild(ci));
        }

        #region internal
        [DllImport("abci")] static extern int aiObjectGetNumChildren(IntPtr obj);
        [DllImport("abci")] static extern aiObject aiObjectGetChild(IntPtr obj, int i);
        [DllImport("abci")] static extern void aiObjectSetEnabled(IntPtr obj, Bool v);
        [DllImport("abci")] static extern IntPtr aiObjectGetName(IntPtr obj);
        [DllImport("abci")] static extern IntPtr aiObjectGetFullName(IntPtr obj);

        [DllImport("abci")] static extern aiXform aiObjectAsXform(IntPtr obj);
        [DllImport("abci")] static extern aiCamera aiObjectAsCamera(IntPtr obj);
        [DllImport("abci")] static extern aiPoints aiObjectAsPoints(IntPtr obj);
        [DllImport("abci")] static extern aiPolyMesh aiObjectAsPolyMesh(IntPtr obj);
        #endregion
    }

    public struct aiSchema
    {
        public IntPtr self;
        public static implicit operator bool(aiSchema v) { return v.self != IntPtr.Zero; }
        public static explicit operator aiXform(aiSchema v) { var tmp = default(aiXform); tmp.self = v.self; return tmp; }
        public static explicit operator aiCamera(aiSchema v) { var tmp = default(aiCamera); tmp.self = v.self; return tmp; }
        public static explicit operator aiPolyMesh(aiSchema v) { var tmp = default(aiPolyMesh); tmp.self = v.self; return tmp; }
        public static explicit operator aiPoints(aiSchema v) { var tmp = default(aiPoints); tmp.self = v.self; return tmp; }

        public bool isConstant { get { return aiSchemaIsConstant(self); } }
        public bool isDataUpdated { get { aiSchemaSync(self); return aiSchemaIsDataUpdated(self); } }
        public aiSample sample { get { return aiSchemaGetSample(self); } }

        public void UpdateSample(ref aiSampleSelector ss) { aiSchemaUpdateSample(self, ref ss); }

        #region internal
        [DllImport("abci")] static extern void aiSchemaUpdateSample(IntPtr schema, ref aiSampleSelector ss);
        [DllImport("abci")] static extern void aiSchemaSync(IntPtr schema);
        [DllImport("abci")] static extern aiSample aiSchemaGetSample(IntPtr schema);

        [DllImport("abci")] static extern Bool aiSchemaIsConstant(IntPtr schema);
        [DllImport("abci")] static extern Bool aiSchemaIsDataUpdated(IntPtr schema);

        [DllImport("abci")] static extern int aiSchemaGetNumProperties(IntPtr schema);
        [DllImport("abci")] static extern aiProperty aiSchemaGetPropertyByIndex(IntPtr schema, int i);
        [DllImport("abci")] static extern aiProperty aiSchemaGetPropertyByName(IntPtr schema, string name);
        #endregion
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct aiXform
    {
        [FieldOffset(0)] public IntPtr self;
        [FieldOffset(0)] public aiSchema schema;
        public static implicit operator bool(aiXform v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSchema(aiXform v) { return v.schema; }

        public aiXformSample sample { get { return aiSchemaGetSample(self); } }

        #region internal
        [DllImport("abci")] static extern aiXformSample aiSchemaGetSample(IntPtr schema);
        #endregion
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct aiCamera
    {
        [FieldOffset(0)] public IntPtr self;
        [FieldOffset(0)] public aiSchema schema;
        public static implicit operator bool(aiCamera v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSchema(aiCamera v) { return v.schema; }

        public aiCameraSample sample { get { return aiSchemaGetSample(self); } }

        #region internal
        [DllImport("abci")] static extern aiCameraSample aiSchemaGetSample(IntPtr schema);
        #endregion
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct aiPolyMesh
    {
        [FieldOffset(0)] public IntPtr self;
        [FieldOffset(0)] public aiSchema schema;
        public static implicit operator bool(aiPolyMesh v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSchema(aiPolyMesh v) { return v.schema; }

        public aiPolyMeshSample sample { get { return aiSchemaGetSample(self); } }
        public void GetSummary(ref aiMeshSummary dst) { aiPolyMeshGetSummary(self, ref dst); }

        #region internal
        [DllImport("abci")] static extern void aiPolyMeshGetSummary(IntPtr schema, ref aiMeshSummary dst);
        [DllImport("abci")] static extern aiPolyMeshSample aiSchemaGetSample(IntPtr schema);
        #endregion
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct aiPoints
    {
        [FieldOffset(0)] public IntPtr self;
        [FieldOffset(0)] public aiSchema schema;
        public static implicit operator bool(aiPoints v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSchema(aiPoints v) { return v.schema; }

        public aiPointsSample sample { get { return aiSchemaGetSample(self); } }
        public bool sort { set { aiPointsSetSort(self, value); } }
        public Vector3 sortBasePosition { set { aiPointsSetSortBasePosition(self, value); } }

        public void GetSummary(ref aiPointsSummary dst) { aiPointsGetSummary(self, ref dst); }

        #region internal
        [DllImport("abci")] static extern aiPointsSample aiSchemaGetSample(IntPtr schema);
        [DllImport("abci")] static extern void aiPointsSetSort(IntPtr schema, Bool v);
        [DllImport("abci")] static extern void aiPointsSetSortBasePosition(IntPtr schema, Vector3 v);
        [DllImport("abci")] static extern void aiPointsGetSummary(IntPtr schema, ref aiPointsSummary dst);
        #endregion
    }


    public struct aiSample
    {
        public IntPtr self;
        public static implicit operator bool(aiSample v) { return v.self != IntPtr.Zero; }
        public static explicit operator aiXformSample(aiSample v) { aiXformSample tmp; tmp.self = v.self; return tmp; }
        public static explicit operator aiCameraSample(aiSample v) { aiCameraSample tmp; tmp.self = v.self; return tmp; }
        public static explicit operator aiPolyMeshSample(aiSample v) { aiPolyMeshSample tmp; tmp.self = v.self; return tmp; }
        public static explicit operator aiPointsSample(aiSample v) { aiPointsSample tmp; tmp.self = v.self; return tmp; }
    }

    public struct aiXformSample
    {
        public IntPtr self;
        public static implicit operator bool(aiXformSample v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSample(aiXformSample v) { aiSample tmp; tmp.self = v.self; return tmp; }

        public void GetData(ref aiXformData dst) { aiXformGetData(self, ref dst); }

        #region internal
        [DllImport("abci")] public static extern void aiXformGetData(IntPtr sample, ref aiXformData data);
        #endregion
    }

    public struct aiCameraSample
    {
        public IntPtr self;
        public static implicit operator bool(aiCameraSample v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSample(aiCameraSample v) { aiSample tmp; tmp.self = v.self; return tmp; }

        public void GetData(ref aiCameraData dst) { aiCameraGetData(self, ref dst); }

        #region internal
        [DllImport("abci")] public static extern void aiCameraGetData(IntPtr sample, ref aiCameraData dst);
        #endregion
    }

    public struct aiPolyMeshSample
    {
        public IntPtr self;
        public static implicit operator bool(aiPolyMeshSample v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSample(aiPolyMeshSample v) { aiSample tmp; tmp.self = v.self; return tmp; }

        public void GetSummary(ref aiMeshSampleSummary dst) { aiPolyMeshGetSampleSummary(self, ref dst); }
        public void GetSplitSummaries(PinnedList<aiMeshSplitSummary> dst) { aiPolyMeshGetSplitSummaries(self, dst); }
        public void GetSubmeshSummaries(PinnedList<aiSubmeshSummary> dst) { aiPolyMeshGetSubmeshSummaries(self, dst); }
        public void FillVertexBuffer(PinnedList<aiPolyMeshData> vbs, PinnedList<aiSubmeshData> ibs) { aiPolyMeshFillVertexBuffer(self, vbs, ibs); }
        public void Sync() { aiSampleSync(self); }

        #region internal
        [DllImport("abci")] static extern void aiPolyMeshGetSampleSummary(IntPtr sample, ref aiMeshSampleSummary dst);
        [DllImport("abci")] static extern int aiPolyMeshGetSplitSummaries(IntPtr sample, IntPtr dst);
        [DllImport("abci")] static extern void aiPolyMeshGetSubmeshSummaries(IntPtr sample, IntPtr dst);
        [DllImport("abci")] static extern void aiPolyMeshFillVertexBuffer(IntPtr sample, IntPtr vbs, IntPtr ibs);
        [DllImport("abci")] static extern void aiSampleSync(IntPtr sample);
        #endregion
    }

    public struct aiPointsSample
    {
        public IntPtr self;
        public static implicit operator bool(aiPointsSample v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSample(aiPointsSample v) { aiSample tmp; tmp.self = v.self; return tmp; }

        public void GetSummary(ref aiPointsSampleSummary dst) { aiPointsGetSampleSummary(self, ref dst); }
        public void FillData(PinnedList<aiPointsData> dst) { aiPointsFillData(self, dst); }
        public void Sync() { aiSampleSync(self); }

        #region internal
        [DllImport("abci")] static extern void aiPointsGetSampleSummary(IntPtr sample, ref aiPointsSampleSummary dst);
        [DllImport("abci")] static extern void aiPointsFillData(IntPtr sample, IntPtr dst);
        [DllImport("abci")] static extern void aiSampleSync(IntPtr sample);
        #endregion
    }


    public struct aiProperty
    {
        public IntPtr self;
        public static implicit operator bool(aiProperty v) { return v.self != IntPtr.Zero; }

        #region internal
        [DllImport("abci")] static extern IntPtr aiPropertyGetName(IntPtr prop);
        [DllImport("abci")] static extern aiPropertyType aiPropertyGetType(IntPtr prop);
        [DllImport("abci")] static extern void aiPropertyGetData(IntPtr prop, aiPropertyData oData);
        #endregion
    }

    public partial class AbcAPI
    {
        [DllImport("abci")] public static extern aiSampleSelector aiTimeToSampleSelector(double time);
        [DllImport("abci")] public static extern void aiCleanup();

        [DllImport("libdl")] static extern IntPtr dlopen(string filename, int flags);
        [DllImport("libdl")] static extern IntPtr dlsym(IntPtr handle, string symbol);
        const int RTLD_LAZY = 1;
        const int RTLD_NOW = 2;
        const int RTLD_LOCAL = 4;
        const int RTLD_GLOBAL = 8;
        const int RTLD_FIRST = 256;

        const string libpath_in_unity = "Packages/com.unity.alembic/Runtime/Plugins/x86_64/abci.bundle/Contents/MacOS/abci";
        static string libpath { get { return System.IO.Path.GetFullPath(libpath_in_unity); } }
        static IntPtr s_moduleHandle = dlopen(libpath, RTLD_LAZY|RTLD_LOCAL|RTLD_FIRST);
        static T Get<T>(string name) where T: class {
            if(s_moduleHandle == (IntPtr)0) { throw new System.ArgumentException(string.Format("abci not found in `{0}'", libpath)); }
            var funHandle = dlsym(s_moduleHandle, name);
            if(funHandle == (IntPtr)0) { throw new System.ArgumentException(string.Format("symbol `{0}' not found in abci", name)); }
            return Marshal.GetDelegateForFunctionPointer(funHandle, typeof(T)) as T;
        }

        public delegate void clearContextsWithPathDelegate(string path);
        public static clearContextsWithPathDelegate clearContextsWithPath = Get<clearContextsWithPathDelegate>("clearContextsWithPath");

        public delegate aiSampleSelector aiTimeToSampleSelectorDelegate(float time);
        public static aiTimeToSampleSelectorDelegate aiTimeToSampleSelector = Get<aiTimeToSampleSelectorDelegate>("aiTimeToSampleSelector");

        public delegate void       aiCleanupDelegate();
        public static aiCleanupDelegate aiCleanup = Get<aiCleanupDelegate>("aiCleanup");

        public delegate aiContext  aiCreateContextDelegate(int uid);
        public static aiCreateContextDelegate aiCreateContext = Get<aiCreateContextDelegate>("aiCreateContext");

        public delegate void       aiDestroyContextDelegate(aiContext ctx);
        public static aiDestroyContextDelegate aiDestroyContext = Get<aiDestroyContextDelegate>("aiDestroyContext");

        public delegate Bool       aiLoadDelegate(aiContext ctx, string path);
        public static aiLoadDelegate aiLoad = Get<aiLoadDelegate>("aiLoad");

        public delegate void       aiSetConfigDelegate(aiContext ctx, ref aiConfig conf);
        public static aiSetConfigDelegate aiSetConfig = Get<aiSetConfigDelegate>("aiSetConfig");

        public delegate float      aiGetStartTimeDelegate(aiContext ctx);
        public static aiGetStartTimeDelegate aiGetStartTime = Get<aiGetStartTimeDelegate>("aiGetStartTime");

        public delegate float      aiGetEndTimeDelegate(aiContext ctx);
        public static aiGetEndTimeDelegate aiGetEndTime = Get<aiGetEndTimeDelegate>("aiGetEndTime");

        public delegate int        getFrameCountDelegate(aiContext ctx);
        public static getFrameCountDelegate getFrameCount = Get<getFrameCountDelegate>("getFrameCount");

        public delegate aiObject   aiGetTopObjectDelegate(aiContext ctx);
        public static aiGetTopObjectDelegate aiGetTopObject = Get<aiGetTopObjectDelegate>("aiGetTopObject");

        public delegate void       aiDestroyObjectDelegate(aiContext ctx, aiObject obj);
        public static aiDestroyObjectDelegate aiDestroyObject = Get<aiDestroyObjectDelegate>("aiDestroyObject");

        public delegate void       aiUpdateSamplesDelegate(aiContext ctx, float time);
        public static aiUpdateSamplesDelegate aiUpdateSamples = Get<aiUpdateSamplesDelegate>("aiUpdateSamples");

        public delegate void       aiEnumerateChildDelegate(aiObject obj, aiNodeEnumerator e, IntPtr userData);
        public static aiEnumerateChildDelegate aiEnumerateChild = Get<aiEnumerateChildDelegate>("aiEnumerateChild");

        private delegate IntPtr    aiGetNameSDelegate(aiObject obj);
        private static aiGetNameSDelegate aiGetNameS = Get<aiGetNameSDelegate>("aiGetNameS");
        public static string aiGetName(aiObject obj)      { return Marshal.PtrToStringAnsi(aiGetNameS(obj)); }

        public delegate void       aiSchemaSetSampleCallbackDelegate(aiSchema schema, aiSampleCallback cb, IntPtr arg);
        public static aiSchemaSetSampleCallbackDelegate aiSchemaSetSampleCallback = Get<aiSchemaSetSampleCallbackDelegate>("aiSchemaSetSampleCallback");

        public delegate void       aiSchemaSetConfigCallbackDelegate(aiSchema schema, aiConfigCallback cb, IntPtr arg);
        public static aiSchemaSetConfigCallbackDelegate aiSchemaSetConfigCallback = Get<aiSchemaSetConfigCallbackDelegate>("aiSchemaSetConfigCallback");

        public delegate aiSample   aiSchemaUpdateSampleDelegate(aiSchema schema, ref aiSampleSelector ss);
        public static aiSchemaUpdateSampleDelegate aiSchemaUpdateSample = Get<aiSchemaUpdateSampleDelegate>("aiSchemaUpdateSample");

        public delegate aiSchema   aiGetXFormDelegate(aiObject obj);
        public static aiGetXFormDelegate aiGetXForm = Get<aiGetXFormDelegate>("aiGetXForm");

        public delegate void       aiXFormGetDataDelegate(aiSample sample, ref aiXFormData data);
        public static aiXFormGetDataDelegate aiXFormGetData = Get<aiXFormGetDataDelegate>("aiXFormGetData");

        public delegate aiSchema   aiGetPolyMeshDelegate(aiObject obj);
        public static aiGetPolyMeshDelegate aiGetPolyMesh = Get<aiGetPolyMeshDelegate>("aiGetPolyMesh");

        public delegate void       aiPolyMeshGetSummaryDelegate(aiSchema schema, ref aiMeshSummary summary);
        public static aiPolyMeshGetSummaryDelegate aiPolyMeshGetSummary = Get<aiPolyMeshGetSummaryDelegate>("aiPolyMeshGetSummary");

        public delegate void       aiPolyMeshGetSampleSummaryDelegate(aiSample sample, ref aiMeshSampleSummary summary, Bool forceRefresh);
        public static aiPolyMeshGetSampleSummaryDelegate aiPolyMeshGetSampleSummary = Get<aiPolyMeshGetSampleSummaryDelegate>("aiPolyMeshGetSampleSummary");

        public delegate int        aiPolyMeshGetVertexBufferLengthDelegate(aiSample sample, int splitIndex);
        public static aiPolyMeshGetVertexBufferLengthDelegate aiPolyMeshGetVertexBufferLength = Get<aiPolyMeshGetVertexBufferLengthDelegate>("aiPolyMeshGetVertexBufferLength");

        public delegate void       aiPolyMeshFillVertexBufferDelegate(aiSample sample, int splitIndex, ref aiPolyMeshData data);
        public static aiPolyMeshFillVertexBufferDelegate aiPolyMeshFillVertexBuffer = Get<aiPolyMeshFillVertexBufferDelegate>("aiPolyMeshFillVertexBuffer");

        public delegate int        aiPolyMeshPrepareSubmeshesDelegate(aiSample sample, ref aiFacesets facesets);
        public static aiPolyMeshPrepareSubmeshesDelegate aiPolyMeshPrepareSubmeshes = Get<aiPolyMeshPrepareSubmeshesDelegate>("aiPolyMeshPrepareSubmeshes");

        public delegate int        aiPolyMeshGetSplitSubmeshCountDelegate(aiSample sample, int splitIndex);
        public static aiPolyMeshGetSplitSubmeshCountDelegate aiPolyMeshGetSplitSubmeshCount = Get<aiPolyMeshGetSplitSubmeshCountDelegate>("aiPolyMeshGetSplitSubmeshCount");

        public delegate Bool       aiPolyMeshGetNextSubmeshDelegate(aiSample sample, ref aiSubmeshSummary smi);
        public static aiPolyMeshGetNextSubmeshDelegate aiPolyMeshGetNextSubmesh = Get<aiPolyMeshGetNextSubmeshDelegate>("aiPolyMeshGetNextSubmesh");

        public delegate void       aiPolyMeshFillSubmeshIndicesDelegate(aiSample sample, ref aiSubmeshSummary smi, ref aiSubmeshData data);
        public static aiPolyMeshFillSubmeshIndicesDelegate aiPolyMeshFillSubmeshIndices = Get<aiPolyMeshFillSubmeshIndicesDelegate>("aiPolyMeshFillSubmeshIndices");

        public delegate aiSchema   aiGetCameraDelegate(aiObject obj);
        public static aiGetCameraDelegate aiGetCamera = Get<aiGetCameraDelegate>("aiGetCamera");

        public delegate void       aiCameraGetDataDelegate(aiSample sample, ref aiCameraData data);
        public static aiCameraGetDataDelegate aiCameraGetData = Get<aiCameraGetDataDelegate>("aiCameraGetData");

        public delegate aiSchema   aiGetPointsDelegate(aiObject obj);
        public static aiGetPointsDelegate aiGetPoints = Get<aiGetPointsDelegate>("aiGetPoints");

        public delegate void       aiPointsSetSortDelegate(aiSchema schema, Bool v);
        public static aiPointsSetSortDelegate aiPointsSetSort = Get<aiPointsSetSortDelegate>("aiPointsSetSort");

        public delegate void       aiPointsSetSortBasePositionDelegate(aiSchema schema, Vector3 v);
        public static aiPointsSetSortBasePositionDelegate aiPointsSetSortBasePosition = Get<aiPointsSetSortBasePositionDelegate>("aiPointsSetSortBasePosition");

        public delegate void       aiPointsGetSummaryDelegate(aiSchema schema, ref aiPointsSummary summary);
        public static aiPointsGetSummaryDelegate aiPointsGetSummary = Get<aiPointsGetSummaryDelegate>("aiPointsGetSummary");

        public delegate void       aiPointsCopyDataDelegate(aiSample sample, ref aiPointsData data);
        public static aiPointsCopyDataDelegate aiPointsCopyData = Get<aiPointsCopyDataDelegate>("aiPointsCopyData");

        public delegate int             aiSchemaGetNumPropertiesDelegate(aiSchema schema);
        public static aiSchemaGetNumPropertiesDelegate aiSchemaGetNumProperties = Get<aiSchemaGetNumPropertiesDelegate>("aiSchemaGetNumProperties");

        public delegate aiProperty      aiSchemaGetPropertyByIndexDelegate(aiSchema schema, int i);
        public static aiSchemaGetPropertyByIndexDelegate aiSchemaGetPropertyByIndex = Get<aiSchemaGetPropertyByIndexDelegate>("aiSchemaGetPropertyByIndex");

        public delegate aiProperty      aiSchemaGetPropertyByNameDelegate(aiSchema schema, string name);
        public static aiSchemaGetPropertyByNameDelegate aiSchemaGetPropertyByName = Get<aiSchemaGetPropertyByNameDelegate>("aiSchemaGetPropertyByName");

        public delegate IntPtr          aiPropertyGetNameSDelegate(aiProperty prop);
        public static aiPropertyGetNameSDelegate aiPropertyGetNameS = Get<aiPropertyGetNameSDelegate>("aiPropertyGetNameS");

        public delegate aiPropertyType  aiPropertyGetTypeDelegate(aiProperty prop);
        public static aiPropertyGetTypeDelegate aiPropertyGetType = Get<aiPropertyGetTypeDelegate>("aiPropertyGetType");

/*
        public delegate void            aiPropertyGetDataDelegate(aiProperty prop, aiPropertyData oData);
        public static aiPropertyGetDataDelegate aiPropertyGetData = Get<aiPropertyGetDataDelegate>("aiPropertyGetData");
*/

    }
}
