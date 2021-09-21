using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

[assembly: InternalsVisibleTo("Unity.Formats.Alembic.UnitTests.Editor")]
[assembly: InternalsVisibleTo("Unity.Formats.Alembic.UnitTests.Runtime")]
[assembly: InternalsVisibleTo("Unity.Formats.Alembic.RecorderTests.Editor")]
[assembly: InternalsVisibleTo("Unity.Formats.Alembic.Tests")]
[assembly: InternalsVisibleTo("Unity.Formats.Alembic.Editor")]

namespace UnityEngine.Formats.Alembic.Sdk
{
    /// <summary>
    /// The time sampling mode for the Alembic file.
    /// </summary>
    public enum TimeSamplingType
    {
        /// <summary>
        /// The interval between frames on the Alembic side is always constant.
        /// </summary>
        Uniform = 0,
        // Cyclic = 1,
        /// <summary>
        /// The delta time in Unity is the interval between the frames on the Alembic side. The interval is not constant, but the impact on the game progress is minimal. It is a mode mainly assuming 3D recording of games.
        /// </summary>
        Acyclic = 2,
    };

    /// <summary>
    /// The transform format (Xform Type).
    /// </summary>
    public enum TransformType
    {
        /// <summary>
        /// Record the full transformation matrix.
        /// </summary>
        Matrix,
        /// <summary>
        /// Record the animation in TRS format (translation, rotation, scale).
        /// </summary>
        TRS,
    };

    enum aeTopology
    {
        Points,
        Lines,
        Triangles,
        Quads,
    };

    enum aePropertyType
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

    /// <summary>
    /// Class containing Alembic file format options.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public class AlembicExportOptions
    {
        [SerializeField]
        TimeSamplingType timeSamplingType = TimeSamplingType.Uniform;
        /// <summary>
        /// Get or set the time sampling type (Uniform or Acyclic).
        /// </summary>
        public TimeSamplingType TimeSamplingType
        {
            get { return timeSamplingType; }
            set { timeSamplingType = value; }
        }
        [HideInInspector, SerializeField]
        float frameRate = 30;
        /// <summary>
        /// Get or set the capture frame rate. Only available when TimeSamplingType is set to Uniform.
        /// </summary>
        public float FrameRate
        {
            get { return frameRate; }
            set { frameRate = Mathf.Max(value, Mathf.Epsilon); } // Prevent divisions by 0
        }
        [SerializeField]
        TransformType xformType = TransformType.TRS;
        /// <summary>
        /// Get or set the transform format (full matrix or TRS).
        /// </summary>
        public TransformType TranformType
        {
            get { return xformType; }
            set { xformType = value; }
        }
        [SerializeField]
        Bool swapHandedness = true;
        /// <summary>
        /// Enable to change from a left hand coordinate system (Unity) to a right hand coordinate system (Autodesk® Maya®).
        /// </summary>
        public bool SwapHandedness
        {
            get { return swapHandedness; }
            set { swapHandedness = value; }
        }
        [SerializeField]
        Bool swapFaces = false;
        /// <summary>
        /// Enable to reverse the front and back of all faces.
        /// </summary>
        public bool SwapFaces
        {
            get { return swapFaces; }
            set { swapFaces = value; }
        }
        [SerializeField]
        float scaleFactor = 100;
        /// <summary>
        /// Get or set the scale factor to convert between different system units. For example, using 0.1 converts the Unity units to 1/10 of their value in the resulting Alembic file. This also affects position and speed.
        /// </summary>
        public float ScaleFactor
        {
            get { return scaleFactor; }
            set { scaleFactor = value; }
        }
    }

    struct aeXformData
    {
        public Bool visibility { get; set; }
        public Vector3 translation { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 scale { get; set; }
        public Bool inherits { get; set; }
    }

    struct aePointsData
    {
        public Bool visibility { get; set; }
        public IntPtr positions { get; set; } // Vector3*

        public IntPtr velocities { get; set; } // Vector3*. can be null

        public IntPtr ids { get; set; } // uint*. can be null

        public int count { get; set; }
    }


    struct aeSubmeshData
    {
        internal IntPtr indexes { get; set; }
        public int indexCount { get; set; }
        public aeTopology topology { get; set; }
    };

    struct aePolyMeshData
    {
        public Bool visibility { get; set; }
        public IntPtr points { get; set; }  // Vector3*
        public int pointCount { get; set; }
        public IntPtr normals;          // Vector3*. can be null
        public IntPtr uv0;              // Vector2*. can be null
        public IntPtr uv1;              // Vector2*. can be null
        public IntPtr colors;           // Vector2*. can be null
        public IntPtr submeshes;        // aeSubmeshData*. can be null
        public int submeshCount;
    }

    struct aeCameraData
    {
        public Bool visibility;

        public float nearClippingPlane;
        public float farClippingPlane;
        public float fieldOfView;   // in degree. relevant only if focalLength==0.0 (default)
        public float aspectRatio;

        public float focusDistance; // in cm
        public float focalLength;   // in mm. if 0.0f, automatically computed by aperture and fieldOfView. alembic's default value is 0.035f.
        public float aperture;      // in cm. vertical one

        public static aeCameraData defaultValue
        {
            get
            {
                return new aeCameraData
                {
                    nearClippingPlane = 0.3f,
                    farClippingPlane = 1000.0f,
                    fieldOfView = 60.0f,
                    aspectRatio = 16.0f / 9.0f,
                    focusDistance = 5.0f,
                    focalLength = 0.0f,
                    aperture = 2.4f,
                };
            }
        }
    }

    struct aeContext
    {
        public IntPtr self;

        public aeObject topObject { get { return NativeMethods.aeGetTopObject(self); } }

        public static aeContext Create() { return NativeMethods.aeCreateContext(); }
        public void Destroy() { NativeMethods.aeDestroyContext(self); self = IntPtr.Zero; }
        public void SetConfig(AlembicExportOptions conf) { NativeMethods.aeSetConfig(self, conf); }
        public bool OpenArchive(string path) { return NativeMethods.aeOpenArchive(self, path); }
        public int AddTimeSampling(float start_time) { return NativeMethods.aeAddTimeSampling(self, start_time); }
        public void AddTime(float start_time) { NativeMethods.aeAddTime(self, start_time); }
        public void MarkFrameBegin() { NativeMethods.aeMarkFrameBegin(self); }
        public void MarkFrameEnd() { NativeMethods.aeMarkFrameEnd(self); }
    }

    struct aeObject
    {
        public IntPtr self;

        public aeObject(IntPtr self)
        {
            this.self = self;
        }

        public aeObject NewXform(string name, int tsi) { return NativeMethods.aeNewXform(self, SanitizeName(name), tsi); }
        public aeObject NewCamera(string name, int tsi) { return NativeMethods.aeNewCamera(self, SanitizeName(name), tsi); }
        public aeObject NewPoints(string name, int tsi) { return NativeMethods.aeNewPoints(self, SanitizeName(name), tsi); }
        public aeObject NewPolyMesh(string name, int tsi) { return NativeMethods.aeNewPolyMesh(self, SanitizeName(name), tsi); }

        public void WriteSample(ref aeXformData data) { NativeMethods.aeXformWriteSample(self, ref data); }
        public void WriteSample(ref CameraData data) { NativeMethods.aeCameraWriteSample(self, ref data); }

        public void WriteSample(ref aePolyMeshData data) { NativeMethods.aePolyMeshWriteSample(self, ref data); }
        public void AddFaceSet(string name) { NativeMethods.aePolyMeshAddFaceSet(self, name); }

        public void WriteSample(ref aePointsData data) { NativeMethods.aePointsWriteSample(self, ref data); }

        public aeProperty NewProperty(string name, aePropertyType type) { return NativeMethods.aeNewProperty(self, name, type); }

        public void MarkForceInvisible() { NativeMethods.aeMarkForceInvisible(self); }

        static string SanitizeName(string name)
        {
            if (!name.Contains("/")) return name;

            var ret = name.Replace('/', '_');
            Debug.LogWarning($"AlembicExporter: Illegal character '/' in Alembic object name '{name}'. Replaced with {ret}");
            return ret;
        }
    }

    struct aeProperty
    {
        public IntPtr self;

        public aeProperty(IntPtr self)
        {
            this.self = self;
        }

        public void WriteArraySample(IntPtr data, int numData) { NativeMethods.aePropertyWriteArraySample(self, data, numData); }

        public void WriteScalarSample(ref float data) { NativeMethods.aePropertyWriteScalarSample(self, ref data); }
        public void WriteScalarSample(ref int data) { NativeMethods.aePropertyWriteScalarSample(self, ref data); }
        public void WriteScalarSample(ref Bool data) { NativeMethods.aePropertyWriteScalarSample(self, ref data); }
        public void WriteScalarSample(ref Vector2 data) { NativeMethods.aePropertyWriteScalarSample(self, ref data); }
        public void WriteScalarSample(ref Vector3 data) { NativeMethods.aePropertyWriteScalarSample(self, ref data); }
        public void WriteScalarSample(ref Vector4 data) { NativeMethods.aePropertyWriteScalarSample(self, ref data); }
        public void WriteScalarSample(ref Matrix4x4 data) { NativeMethods.aePropertyWriteScalarSample(self, ref data); }
    }


    static class AbcAPI
    {
        public static void aeWaitMaxDeltaTime()
        {
            var next = Time.unscaledTime + Time.maximumDeltaTime;
            while (Time.realtimeSinceStartup < next)
                System.Threading.Thread.Sleep(1);
        }
    }

    static class NativeMethods
    {
        [DllImport(Abci.Lib)] public static extern aeContext aeCreateContext();
        [DllImport(Abci.Lib)] public static extern void aeDestroyContext(IntPtr ctx);
        [DllImport(Abci.Lib)] public static extern bool aiContextGetIsHDF5(IntPtr ctx);

        [DllImport(Abci.Lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr aiContextGetApplication(IntPtr ctx);
        [DllImport(Abci.Lib)] public static extern void aeSetConfig(IntPtr ctx, AlembicExportOptions conf);
        [DllImport(Abci.Lib, BestFitMapping = false, ThrowOnUnmappableChar = true)] public static extern Bool aeOpenArchive(IntPtr ctx, string path);
        [DllImport(Abci.Lib)] public static extern aeObject aeGetTopObject(IntPtr ctx);
        [DllImport(Abci.Lib)] public static extern int aeAddTimeSampling(IntPtr ctx, float start_time);
        // relevant only if plingType is acyclic. if tsi==-1, add time to all time samplings.
        [DllImport(Abci.Lib)] public static extern void aeAddTime(IntPtr ctx, float time, int tsi = -1);
        [DllImport(Abci.Lib)] public static extern void aeMarkFrameBegin(IntPtr ctx);
        [DllImport(Abci.Lib)] public static extern void aeMarkFrameEnd(IntPtr ctx);


        [DllImport(Abci.Lib, BestFitMapping = false, ThrowOnUnmappableChar = true)] public static extern aeObject aeNewXform(IntPtr self, string name, int tsi);
        [DllImport(Abci.Lib, BestFitMapping = false, ThrowOnUnmappableChar = true)] public static extern aeObject aeNewCamera(IntPtr self, string name, int tsi);
        [DllImport(Abci.Lib, BestFitMapping = false, ThrowOnUnmappableChar = true)] public static extern aeObject aeNewPoints(IntPtr self, string name, int tsi);
        [DllImport(Abci.Lib, BestFitMapping = false, ThrowOnUnmappableChar = true)] public static extern aeObject aeNewPolyMesh(IntPtr self, string name, int tsi);
        [DllImport(Abci.Lib)] public static extern void aeXformWriteSample(IntPtr self, ref aeXformData data);
        [DllImport(Abci.Lib)] public static extern void aeCameraWriteSample(IntPtr self, ref CameraData data);
        [DllImport(Abci.Lib)] public static extern void aePolyMeshWriteSample(IntPtr self, ref aePolyMeshData data);
        [DllImport(Abci.Lib, BestFitMapping = false, ThrowOnUnmappableChar = true)] public static extern int aePolyMeshAddFaceSet(IntPtr self, string name);
        [DllImport(Abci.Lib)] public static extern void aePointsWriteSample(IntPtr self, ref aePointsData data);
        [DllImport(Abci.Lib, BestFitMapping = false, ThrowOnUnmappableChar = true)] public static extern aeProperty aeNewProperty(IntPtr self, string name, aePropertyType type);
        [DllImport(Abci.Lib)] public static extern void aeMarkForceInvisible(IntPtr self);


        [DllImport(Abci.Lib)] public static extern void aePropertyWriteArraySample(IntPtr self, IntPtr data, int num_data);

        // all of these are  IntPtr version. just for convenience.
        [DllImport(Abci.Lib)] public static extern void aePropertyWriteScalarSample(IntPtr self, ref float data);
        [DllImport(Abci.Lib)] public static extern void aePropertyWriteScalarSample(IntPtr self, ref int data);
        [DllImport(Abci.Lib)] public static extern void aePropertyWriteScalarSample(IntPtr self, ref Bool data);
        [DllImport(Abci.Lib)] public static extern void aePropertyWriteScalarSample(IntPtr self, ref Vector2 data);
        [DllImport(Abci.Lib)] public static extern void aePropertyWriteScalarSample(IntPtr self, ref Vector3 data);
        [DllImport(Abci.Lib)] public static extern void aePropertyWriteScalarSample(IntPtr self, ref Vector4 data);
        [DllImport(Abci.Lib)] public static extern void aePropertyWriteScalarSample(IntPtr self, ref Matrix4x4 data);


        [DllImport(Abci.Lib)] public static extern int aeGenerateRemapIndices(IntPtr dstIndices, IntPtr points, IntPtr weights4, int numPoints);
        [DllImport(Abci.Lib)] public static extern void aeApplyMatrixP(IntPtr dstPoints, int num, ref Matrix4x4 mat);
        [DllImport(Abci.Lib)] public static extern void aeApplyMatrixV(IntPtr dstVectors, int num, ref Matrix4x4 mat);


        [DllImport(Abci.Lib, BestFitMapping = false, ThrowOnUnmappableChar = true)] public static extern void aiClearContextsWithPath(string path);
        [DllImport(Abci.Lib)] public static extern aiContext aiContextCreate(int uid);
        [DllImport(Abci.Lib)] public static extern void aiContextDestroy(IntPtr ctx);
        [DllImport(Abci.Lib, BestFitMapping = false, ThrowOnUnmappableChar = true)] public static extern Bool aiContextLoad(IntPtr ctx, string path);
        [DllImport(Abci.Lib)] public static extern void aiContextSetConfig(IntPtr ctx, ref aiConfig conf);
        [DllImport(Abci.Lib)] public static extern int aiContextGetTimeSamplingCount(IntPtr ctx);
        [DllImport(Abci.Lib)] public static extern aiTimeSampling aiContextGetTimeSampling(IntPtr ctx, int i);
        [DllImport(Abci.Lib)] public static extern void aiContextGetTimeRange(IntPtr ctx, out double begin, out double end);
        [DllImport(Abci.Lib)] public static extern aiObject aiContextGetTopObject(IntPtr ctx);
        [DllImport(Abci.Lib)] public static extern void aiContextUpdateSamples(IntPtr ctx, double time);

        [DllImport(Abci.Lib)] public static extern int aiTimeSamplingGetSampleCount(IntPtr self);
        [DllImport(Abci.Lib)] public static extern double aiTimeSamplingGetTime(IntPtr self, int index);
        [DllImport(Abci.Lib)] public static extern void aiTimeSamplingGetRange(IntPtr self, ref double start, ref double end);


        [DllImport(Abci.Lib)] public static extern aiContext aiObjectGetContext(IntPtr obj);
        [DllImport(Abci.Lib)] public static extern int aiObjectGetNumChildren(IntPtr obj);
        [DllImport(Abci.Lib)] public static extern aiObject aiObjectGetChild(IntPtr obj, int i);
        [DllImport(Abci.Lib)] public static extern aiObject aiObjectGetParent(IntPtr obj);
        [DllImport(Abci.Lib)] public static extern void aiObjectSetEnabled(IntPtr obj, Bool v);
        [DllImport(Abci.Lib)] public static extern IntPtr aiObjectGetName(IntPtr obj);
        [DllImport(Abci.Lib)] public static extern IntPtr aiObjectGetFullName(IntPtr obj);
        [DllImport(Abci.Lib)] public static extern Sdk.aiXform aiObjectAsXform(IntPtr obj);
        [DllImport(Abci.Lib)] public static extern Sdk.aiCamera aiObjectAsCamera(IntPtr obj);
        [DllImport(Abci.Lib)] public static extern Sdk.aiPoints aiObjectAsPoints(IntPtr obj);
        [DllImport(Abci.Lib)] public static extern Sdk.aiCurves aiObjectAsCurves(IntPtr obj);
        [DllImport(Abci.Lib)] public static extern Sdk.aiPolyMesh aiObjectAsPolyMesh(IntPtr obj);
        [DllImport(Abci.Lib)] public static extern Sdk.aiSubD aiObjectAsSubD(IntPtr obj);

        [DllImport(Abci.Lib)] public static extern void aiSchemaUpdateSample(IntPtr schema, ref aiSampleSelector ss);
        [DllImport(Abci.Lib)] public static extern void aiSchemaSync(IntPtr schema);
        [DllImport(Abci.Lib)] public static extern aiSample aiSchemaGetSample(IntPtr schema);
        [DllImport(Abci.Lib)] public static extern Bool aiSchemaIsConstant(IntPtr schema);
        [DllImport(Abci.Lib)] public static extern Bool aiSchemaIsDataUpdated(IntPtr schema);
        [DllImport(Abci.Lib)] public static extern int aiSchemaGetNumProperties(IntPtr schema);
        [DllImport(Abci.Lib)] public static extern aiProperty aiSchemaGetPropertyByIndex(IntPtr schema, int i);
        [DllImport(Abci.Lib, BestFitMapping = false, ThrowOnUnmappableChar = true)] public static extern aiProperty aiSchemaGetPropertyByName(IntPtr schema, string name);

        [DllImport(Abci.Lib)] public static extern void aiPolyMeshGetSummary(IntPtr schema, ref aiMeshSummary dst);
        [DllImport(Abci.Lib)] public static extern void aiSubDGetSummary(IntPtr schema, ref aiMeshSummary dst);

        [DllImport(Abci.Lib)] public static extern void aiPointsSetSort(IntPtr schema, Bool v);
        [DllImport(Abci.Lib)] public static extern void aiPointsSetSortBasePosition(IntPtr schema, Vector3 v);
        [DllImport(Abci.Lib)] public static extern void aiPointsGetSummary(IntPtr schema, ref aiPointsSummary dst);

        [DllImport(Abci.Lib)] public static extern void aiCurvesGetSummary(IntPtr schema, ref aiCurvesSummary dst);

        [DllImport(Abci.Lib)] public static extern void aiXformGetData(IntPtr sample, ref aiXformData data);

        [DllImport(Abci.Lib)] public static extern void aiCameraGetData(IntPtr sample, ref CameraData dst);

        [DllImport(Abci.Lib)] public static extern void aiPolyMeshGetSampleSummary(IntPtr sample, ref aiMeshSampleSummary dst);
        [DllImport(Abci.Lib)] public static extern int aiPolyMeshGetSplitSummaries(IntPtr sample, IntPtr dst);
        [DllImport(Abci.Lib)] public static extern void aiPolyMeshGetSubmeshSummaries(IntPtr sample, IntPtr dst);
        [DllImport(Abci.Lib)] public static extern void aiPolyMeshFillVertexBuffer(IntPtr sample, IntPtr vbs, IntPtr ibs);

        [DllImport(Abci.Lib)] public static extern void aiPointsGetSampleSummary(IntPtr sample, ref aiPointsSampleSummary dst);
        [DllImport(Abci.Lib)] public static extern void aiPointsFillData(IntPtr sample, IntPtr dst);

        //
        [DllImport(Abci.Lib)] public static extern void aiCurvesGetSampleSummary(IntPtr sample, ref aiCurvesSampleSummary dst);
        [DllImport(Abci.Lib)] public static extern void aiCurvesFillData(IntPtr sample, IntPtr dst);
        //

        [DllImport(Abci.Lib)] public static extern IntPtr aiPropertyGetName(IntPtr prop);
        [DllImport(Abci.Lib)] public static extern aiPropertyType aiPropertyGetType(IntPtr prop);
        [DllImport(Abci.Lib)] public static extern void aiPropertyGetData(IntPtr prop, aiPropertyData oData);

        [DllImport(Abci.Lib)] public static extern aiSampleSelector aiTimeToSampleSelector(double time);
        [DllImport(Abci.Lib)] public static extern void aiCleanup();

        internal struct aiXform
        {
            [DllImport(Abci.Lib)] public static extern aiXformSample aiSchemaGetSample(IntPtr schema);
        }
        internal struct aiCamera
        {
            [DllImport(Abci.Lib)] public static extern aiCameraSample aiSchemaGetSample(IntPtr schema);
        }
        internal struct aiPolyMesh
        {
            [DllImport(Abci.Lib)] public static extern aiPolyMeshSample aiSchemaGetSample(IntPtr schema);
        }

        internal struct aiSubD
        {
            [DllImport(Abci.Lib)] public static extern aiPolyMeshSample aiSchemaGetSample(IntPtr schema);
        }
        internal struct aiPoints
        {
            [DllImport(Abci.Lib)] public static extern aiPointsSample aiSchemaGetSample(IntPtr schema);
        }

        internal struct aiCurves
        {
            [DllImport(Abci.Lib)] public static extern aiCurvesSample aiSchemaGetSample(IntPtr schema);
        }
    }
}
