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
        public aiNormalsMode normalsMode;
        public aiTangentsMode tangentsMode;
        public float scaleFactor;
        public float aspectRatio;
        public float vertexMotionScale;
        public int splitUnit;
        public Bool swapHandedness;
        public Bool swapFaceWinding;
        public Bool interpolateSamples;
        public Bool turnQuadEdges;
        public Bool asyncLoad;
        public Bool importPointPolygon;
        public Bool importLinePolygon;
        public Bool importTrianglePolygon;

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
        public delegate void aiClearContextsWithPathDelegate(string path);
        public static aiClearContextsWithPathDelegate aiClearContextsWithPath = ModuleImporter.Resolve<aiClearContextsWithPathDelegate>("aiClearContextsWithPath");

        public delegate aiContext aiContextCreateDelegate(int uid);
        public static aiContextCreateDelegate aiContextCreate = ModuleImporter.Resolve<aiContextCreateDelegate>("aiContextCreate");

        public delegate void aiContextDestroyDelegate(IntPtr ctx);
        public static aiContextDestroyDelegate aiContextDestroy = ModuleImporter.Resolve<aiContextDestroyDelegate>("aiContextDestroy");

        public delegate Bool aiContextLoadDelegate(IntPtr ctx, string path);
        public static aiContextLoadDelegate aiContextLoad = ModuleImporter.Resolve<aiContextLoadDelegate>("aiContextLoad");

        public delegate void aiContextSetConfigDelegate(IntPtr ctx, ref aiConfig conf);
        public static aiContextSetConfigDelegate aiContextSetConfig = ModuleImporter.Resolve<aiContextSetConfigDelegate>("aiContextSetConfig");

        public delegate int aiContextGetTimeSamplingCountDelegate(IntPtr ctx);
        public static aiContextGetTimeSamplingCountDelegate aiContextGetTimeSamplingCount = ModuleImporter.Resolve<aiContextGetTimeSamplingCountDelegate>("aiContextGetTimeSamplingCount");

        public delegate aiTimeSampling aiContextGetTimeSamplingDelegate(IntPtr ctx, int i);
        public static aiContextGetTimeSamplingDelegate aiContextGetTimeSampling = ModuleImporter.Resolve<aiContextGetTimeSamplingDelegate>("aiContextGetTimeSampling");

        public delegate void aiContextGetTimeRangeDelegate(IntPtr ctx, ref double begin, ref double end);
        public static aiContextGetTimeRangeDelegate aiContextGetTimeRange = ModuleImporter.Resolve<aiContextGetTimeRangeDelegate>("aiContextGetTimeRange");

        public delegate aiObject aiContextGetTopObjectDelegate(IntPtr ctx);
        public static aiContextGetTopObjectDelegate aiContextGetTopObject = ModuleImporter.Resolve<aiContextGetTopObjectDelegate>("aiContextGetTopObject");

        public delegate void aiContextUpdateSamplesDelegate(IntPtr ctx, double time);
        public static aiContextUpdateSamplesDelegate aiContextUpdateSamples = ModuleImporter.Resolve<aiContextUpdateSamplesDelegate>("aiContextUpdateSamples");
        #endregion
    }

    public struct aiTimeSampling
    {
        public IntPtr self;

        public int sampleCount { get { return aiTimeSamplingGetSampleCount(self); } }
        public double GetTime(int index) { return aiTimeSamplingGetTime(self, index); }
        public void GetRange(ref double start, ref double end) { aiTimeSamplingGetRange(self, ref start, ref end); }

        #region internal
        delegate int aiTimeSamplingGetSampleCountDelegate(IntPtr self);
        static aiTimeSamplingGetSampleCountDelegate aiTimeSamplingGetSampleCount = ModuleImporter.Resolve<aiTimeSamplingGetSampleCountDelegate>("aiTimeSamplingGetSampleCount");

        delegate double aiTimeSamplingGetTimeDelegate(IntPtr self, int index);
        static aiTimeSamplingGetTimeDelegate aiTimeSamplingGetTime = ModuleImporter.Resolve<aiTimeSamplingGetTimeDelegate>("aiTimeSamplingGetTime");

        delegate void aiTimeSamplingGetRangeDelegate(IntPtr self, ref double start, ref double end);
        static aiTimeSamplingGetRangeDelegate aiTimeSamplingGetRange = ModuleImporter.Resolve<aiTimeSamplingGetRangeDelegate>("aiTimeSamplingGetRange");
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
        delegate int aiObjectGetNumChildrenDelegate(IntPtr obj);
        static aiObjectGetNumChildrenDelegate aiObjectGetNumChildren = ModuleImporter.Resolve<aiObjectGetNumChildrenDelegate>("aiObjectGetNumChildren");

        delegate aiObject aiObjectGetChildDelegate(IntPtr obj, int i);
        static aiObjectGetChildDelegate aiObjectGetChild = ModuleImporter.Resolve<aiObjectGetChildDelegate>("aiObjectGetChild");

        delegate void aiObjectSetEnabledDelegate(IntPtr obj, Bool v);
        static aiObjectSetEnabledDelegate aiObjectSetEnabled = ModuleImporter.Resolve<aiObjectSetEnabledDelegate>("aiObjectSetEnabled");

        delegate IntPtr aiObjectGetNameDelegate(IntPtr obj);
        static aiObjectGetNameDelegate aiObjectGetName = ModuleImporter.Resolve<aiObjectGetNameDelegate>("aiObjectGetName");

        delegate IntPtr aiObjectGetFullNameDelegate(IntPtr obj);
        static aiObjectGetFullNameDelegate aiObjectGetFullName = ModuleImporter.Resolve<aiObjectGetFullNameDelegate>("aiObjectGetFullName");

        delegate aiXform aiObjectAsXformDelegate(IntPtr obj);
        static aiObjectAsXformDelegate aiObjectAsXform = ModuleImporter.Resolve<aiObjectAsXformDelegate>("aiObjectAsXform");

        delegate aiCamera aiObjectAsCameraDelegate(IntPtr obj);
        static aiObjectAsCameraDelegate aiObjectAsCamera = ModuleImporter.Resolve<aiObjectAsCameraDelegate>("aiObjectAsCamera");

        delegate aiPoints aiObjectAsPointsDelegate(IntPtr obj);
        static aiObjectAsPointsDelegate aiObjectAsPoints = ModuleImporter.Resolve<aiObjectAsPointsDelegate>("aiObjectAsPoints");

        delegate aiPolyMesh aiObjectAsPolyMeshDelegate(IntPtr obj);
        static aiObjectAsPolyMeshDelegate aiObjectAsPolyMesh = ModuleImporter.Resolve<aiObjectAsPolyMeshDelegate>("aiObjectAsPolyMesh");
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
        delegate void aiSchemaUpdateSampleDelegate(IntPtr schema, ref aiSampleSelector ss);
        static aiSchemaUpdateSampleDelegate aiSchemaUpdateSample = ModuleImporter.Resolve<aiSchemaUpdateSampleDelegate>("aiSchemaUpdateSample");

        delegate void aiSchemaSyncDelegate(IntPtr schema);
        static aiSchemaSyncDelegate aiSchemaSync = ModuleImporter.Resolve<aiSchemaSyncDelegate>("aiSchemaSync");

        delegate aiSample aiSchemaGetSampleDelegate(IntPtr schema);
        static aiSchemaGetSampleDelegate aiSchemaGetSample = ModuleImporter.Resolve<aiSchemaGetSampleDelegate>("aiSchemaGetSample");

        delegate Bool aiSchemaIsConstantDelegate(IntPtr schema);
        static aiSchemaIsConstantDelegate aiSchemaIsConstant = ModuleImporter.Resolve<aiSchemaIsConstantDelegate>("aiSchemaIsConstant");

        delegate Bool aiSchemaIsDataUpdatedDelegate(IntPtr schema);
        static aiSchemaIsDataUpdatedDelegate aiSchemaIsDataUpdated = ModuleImporter.Resolve<aiSchemaIsDataUpdatedDelegate>("aiSchemaIsDataUpdated");

        delegate int aiSchemaGetNumPropertiesDelegate(IntPtr schema);
        static aiSchemaGetNumPropertiesDelegate aiSchemaGetNumProperties = ModuleImporter.Resolve<aiSchemaGetNumPropertiesDelegate>("aiSchemaGetNumProperties");

        delegate aiProperty aiSchemaGetPropertyByIndexDelegate(IntPtr schema, int i);
        static aiSchemaGetPropertyByIndexDelegate aiSchemaGetPropertyByIndex = ModuleImporter.Resolve<aiSchemaGetPropertyByIndexDelegate>("aiSchemaGetPropertyByIndex");

        delegate aiProperty aiSchemaGetPropertyByNameDelegate(IntPtr schema, string name);
        static aiSchemaGetPropertyByNameDelegate aiSchemaGetPropertyByName = ModuleImporter.Resolve<aiSchemaGetPropertyByNameDelegate>("aiSchemaGetPropertyByName");
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
        delegate aiXformSample aiSchemaGetSampleDelegate(IntPtr schema);
        static aiSchemaGetSampleDelegate aiSchemaGetSample = ModuleImporter.Resolve<aiSchemaGetSampleDelegate>("aiSchemaGetSample");
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
        delegate aiCameraSample aiSchemaGetSampleDelegate(IntPtr schema);
        static aiSchemaGetSampleDelegate aiSchemaGetSample = ModuleImporter.Resolve<aiSchemaGetSampleDelegate>("aiSchemaGetSample");
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
        delegate void aiPolyMeshGetSummaryDelegate(IntPtr schema, ref aiMeshSummary dst);
        static aiPolyMeshGetSummaryDelegate aiPolyMeshGetSummary = ModuleImporter.Resolve<aiPolyMeshGetSummaryDelegate>("aiPolyMeshGetSummary");

        delegate aiPolyMeshSample aiSchemaGetSampleDelegate(IntPtr schema);
        static aiSchemaGetSampleDelegate aiSchemaGetSample = ModuleImporter.Resolve<aiSchemaGetSampleDelegate>("aiSchemaGetSample");
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
        delegate aiPointsSample aiSchemaGetSampleDelegate(IntPtr schema);
        static aiSchemaGetSampleDelegate aiSchemaGetSample = ModuleImporter.Resolve<aiSchemaGetSampleDelegate>("aiSchemaGetSample");

        delegate void aiPointsSetSortDelegate(IntPtr schema, Bool v);
        static aiPointsSetSortDelegate aiPointsSetSort = ModuleImporter.Resolve<aiPointsSetSortDelegate>("aiPointsSetSort");

        delegate void aiPointsSetSortBasePositionDelegate(IntPtr schema, Vector3 v);
        static aiPointsSetSortBasePositionDelegate aiPointsSetSortBasePosition = ModuleImporter.Resolve<aiPointsSetSortBasePositionDelegate>("aiPointsSetSortBasePosition");

        delegate void aiPointsGetSummaryDelegate(IntPtr schema, ref aiPointsSummary dst);
        static aiPointsGetSummaryDelegate aiPointsGetSummary = ModuleImporter.Resolve<aiPointsGetSummaryDelegate>("aiPointsGetSummary");
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
        public delegate void aiXformGetDataDelegate(IntPtr sample, ref aiXformData data);
        public static aiXformGetDataDelegate aiXformGetData = ModuleImporter.Resolve<aiXformGetDataDelegate>("aiXformGetData");
        #endregion
    }

    public struct aiCameraSample
    {
        public IntPtr self;
        public static implicit operator bool(aiCameraSample v) { return v.self != IntPtr.Zero; }
        public static implicit operator aiSample(aiCameraSample v) { aiSample tmp; tmp.self = v.self; return tmp; }

        public void GetData(ref aiCameraData dst) { aiCameraGetData(self, ref dst); }

        #region internal
        public delegate void aiCameraGetDataDelegate(IntPtr sample, ref aiCameraData dst);
        public static aiCameraGetDataDelegate aiCameraGetData = ModuleImporter.Resolve<aiCameraGetDataDelegate>("aiCameraGetData");
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
        delegate void aiPolyMeshGetSampleSummaryDelegate(IntPtr sample, ref aiMeshSampleSummary dst);
        static aiPolyMeshGetSampleSummaryDelegate aiPolyMeshGetSampleSummary = ModuleImporter.Resolve<aiPolyMeshGetSampleSummaryDelegate>("aiPolyMeshGetSampleSummary");

        delegate int aiPolyMeshGetSplitSummariesDelegate(IntPtr sample, IntPtr dst);
        static aiPolyMeshGetSplitSummariesDelegate aiPolyMeshGetSplitSummaries = ModuleImporter.Resolve<aiPolyMeshGetSplitSummariesDelegate>("aiPolyMeshGetSplitSummaries");

        delegate void aiPolyMeshGetSubmeshSummariesDelegate(IntPtr sample, IntPtr dst);
        static aiPolyMeshGetSubmeshSummariesDelegate aiPolyMeshGetSubmeshSummaries = ModuleImporter.Resolve<aiPolyMeshGetSubmeshSummariesDelegate>("aiPolyMeshGetSubmeshSummaries");

        delegate void aiPolyMeshFillVertexBufferDelegate(IntPtr sample, IntPtr vbs, IntPtr ibs);
        static aiPolyMeshFillVertexBufferDelegate aiPolyMeshFillVertexBuffer = ModuleImporter.Resolve<aiPolyMeshFillVertexBufferDelegate>("aiPolyMeshFillVertexBuffer");

        delegate void aiSampleSyncDelegate(IntPtr sample);
        static aiSampleSyncDelegate aiSampleSync = ModuleImporter.Resolve<aiSampleSyncDelegate>("aiSampleSync");
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
        delegate void aiPointsGetSampleSummaryDelegate(IntPtr sample, ref aiPointsSampleSummary dst);
        static aiPointsGetSampleSummaryDelegate aiPointsGetSampleSummary = ModuleImporter.Resolve<aiPointsGetSampleSummaryDelegate>("aiPointsGetSampleSummary");

        delegate void aiPointsFillDataDelegate(IntPtr sample, IntPtr dst);
        static aiPointsFillDataDelegate aiPointsFillData = ModuleImporter.Resolve<aiPointsFillDataDelegate>("aiPointsFillData");

        delegate void aiSampleSyncDelegate(IntPtr sample);
        static aiSampleSyncDelegate aiSampleSync = ModuleImporter.Resolve<aiSampleSyncDelegate>("aiSampleSync");
        #endregion
    }


    public struct aiProperty
    {
        public IntPtr self;
        public static implicit operator bool(aiProperty v) { return v.self != IntPtr.Zero; }

        #region internal
        delegate IntPtr aiPropertyGetNameDelegate(IntPtr prop);
        static aiPropertyGetNameDelegate aiPropertyGetName = ModuleImporter.Resolve<aiPropertyGetNameDelegate>("aiPropertyGetName");

        delegate aiPropertyType aiPropertyGetTypeDelegate(IntPtr prop);
        static aiPropertyGetTypeDelegate aiPropertyGetType = ModuleImporter.Resolve<aiPropertyGetTypeDelegate>("aiPropertyGetType");

        delegate void aiPropertyGetDataDelegate(IntPtr prop, aiPropertyData oData);
        static aiPropertyGetDataDelegate aiPropertyGetData = ModuleImporter.Resolve<aiPropertyGetDataDelegate>("aiPropertyGetData");
        #endregion
    }

    public partial class AbcAPI
    {
        public delegate aiSampleSelector aiTimeToSampleSelectorDelegate(double time);
        public static aiTimeToSampleSelectorDelegate aiTimeToSampleSelector = ModuleImporter.Resolve<aiTimeToSampleSelectorDelegate>("aiTimeToSampleSelector");

        public delegate void aiCleanupDelegate();
        public static aiCleanupDelegate aiCleanup = ModuleImporter.Resolve<aiCleanupDelegate>("aiCleanup");
    }
}
