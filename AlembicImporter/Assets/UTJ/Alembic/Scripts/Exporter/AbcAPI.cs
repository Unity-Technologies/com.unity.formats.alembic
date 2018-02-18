using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{
    public enum aeArchiveType
    {
        HDF5,
        Ogawa,
    };

    public enum aeTimeSamplingType
    {
        Uniform = 0,
        // Cyclic = 1,
        Acyclic = 2,
    };

    public enum aeXformType
    {
        Matrix,
        TRS,
    };

    public enum aeTopology
    {
        Points,
        Lines,
        Triangles,
        Quads,
    };

    public enum aePropertyType
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

    [Serializable]
    public struct aeConfig
    {
        public aeArchiveType archiveType;
        public aeTimeSamplingType timeSamplingType;
        public float frameRate;
        public aeXformType xformType;
        public Bool swapHandedness;
        public Bool swapFaces;
        public float scaleFactor;

        public static aeConfig defaultValue
        {
            get
            {
                return new aeConfig
                {
                    archiveType = aeArchiveType.Ogawa,
                    timeSamplingType = aeTimeSamplingType.Uniform,
                    frameRate = 30.0f,
                    xformType = aeXformType.TRS,
                    swapHandedness = true,
                    swapFaces = false,
                    scaleFactor = 100.0f,
                };
            }
        }
    }

    public struct aeXformData
    {
        public Bool visibility;

        public Vector3 translation;
        public Quaternion rotation;
        public Vector3 scale;
        public Bool inherits;
    }

    public struct aePointsData
    {
        public Bool visibility;

        public IntPtr positions; // Vector3*
        public IntPtr velocities; // Vector3*. can be null
        public IntPtr ids; // uint*. can be null
        public int count;
    }


    public struct aeSubmeshData
    {
        public IntPtr     indices;
        public int        indexCount;
        public aeTopology topology;
    };

    public struct aePolyMeshData
    {
        public Bool visibility;

        public IntPtr   faces;            // int*. if null, assume all faces are triangles
        public IntPtr   indices;          // int*. 
        public int      faceCount;
        public int      indexCount;

        public IntPtr   points;           // Vector3*
        public IntPtr   velocities;       // Vector3*. can be null
        public int      pointCount;

        public IntPtr   normals;          // Vector3*. can be null
        public IntPtr   normalIndices;    // int*. if null, assume same as indices
        public int      normalCount;      // if 0, assume same as pointCount
        public int      normalIndexCount; // if 0, assume same as indexCount

        public IntPtr   uv0;              // Vector2*. can be null
        public IntPtr   uv0Indices;       // int*. if null, assume same as indices
        public int      uv0Count;         // if 0, assume same as pointCount
        public int      uv0IndexCount;    // if 0, assume same as indexCount
        
        public IntPtr   uv1;              // Vector2*. can be null
        public IntPtr   uv1Indices;       // int*. if null, assume same as indices
        public int      uv1Count;         // if 0, assume same as pointCount
        public int      uv1IndexCount;    // if 0, assume same as indexCount
        
        public IntPtr   colors;           // Vector2*. can be null
        public IntPtr   colorIndices;     // int*. if null, assume same as indices
        public int      colorCount;       // if 0, assume same as pointCount
        public int      colorIndexCount;  // if 0, assume same as indexCount

        public IntPtr   submeshes;        // aeSubmeshData*. can be null
        public int      submeshCount;
    }

    public struct aeFaceSetData
    {
        public IntPtr faces;
        public int faceCount;
    }

    public struct aeCameraData
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

    public struct aeContext
    {
        public IntPtr self;

        public aeObject topObject { get { return aeGetTopObject(self); } }

        public static aeContext Create() { return aeCreateContext(); }
        public void Destroy() { aeDestroyContext(self); self = IntPtr.Zero; }
        public void SetConfig(ref aeConfig conf) { aeSetConfig(self, ref conf); }
        public bool OpenArchive(string path) { return aeOpenArchive(self, path); }
        public int AddTimeSampling(float start_time) { return aeAddTimeSampling(self, start_time); }
        public void AddTime(float start_time) { aeAddTime(self, start_time); }
        public void MarkFrameBegin() { aeMarkFrameBegin(self); }
        public void MarkFrameEnd() { aeMarkFrameEnd(self); }

        #region internal
        [DllImport("abci")] static extern aeContext aeCreateContext();
        [DllImport("abci")] static extern void aeDestroyContext(IntPtr ctx);

        [DllImport("abci")] static extern void aeSetConfig(IntPtr ctx, ref aeConfig conf);
        [DllImport("abci")] static extern Bool aeOpenArchive(IntPtr ctx, string path);
        [DllImport("abci")] static extern aeObject aeGetTopObject(IntPtr ctx);
        [DllImport("abci")] static extern int aeAddTimeSampling(IntPtr ctx, float start_time);
        // relevant only if plingType is acyclic. if tsi==-1, add time to all time samplings.
        [DllImport("abci")] static extern void aeAddTime(IntPtr ctx, float time, int tsi = -1);
        [DllImport("abci")] static extern void aeMarkFrameBegin(IntPtr ctx);
        [DllImport("abci")] static extern void aeMarkFrameEnd(IntPtr ctx);
        #endregion
    }

    public struct aeObject
    {
        public IntPtr self;

        public aeObject NewXform(string name, int tsi) { return aeNewXform(self, name, tsi); }
        public aeObject NewCamera(string name, int tsi) { return aeNewCamera(self, name, tsi); }
        public aeObject NewPoints(string name, int tsi) { return aeNewPoints(self, name, tsi); }
        public aeObject NewPolyMesh(string name, int tsi) { return aeNewPolyMesh(self, name, tsi); }

        public void WriteSample(ref aeXformData data) { aeXformWriteSample(self, ref data); }
        public void WriteSample(ref aeCameraData data) { aeCameraWriteSample(self, ref data); }

        public void WriteSample(ref aePolyMeshData data) { aePolyMeshWriteSample(self, ref data); }
        public void AddFaceSet(string name) { aePolyMeshAddFaceSet(self, name); }

        public void WriteSample(ref aePointsData data) { aePointsWriteSample(self, ref data); }

        public aeProperty NewProperty(string name, aePropertyType type) { return aeNewProperty(self, name, type); }

        public void MarkForceInvisible() { aeMarkForceInvisible(self); }

        #region internal
        [DllImport("abci")] static extern aeObject aeNewXform(IntPtr self, string name, int tsi);
        [DllImport("abci")] static extern aeObject aeNewCamera(IntPtr self, string name, int tsi);
        [DllImport("abci")] static extern aeObject aeNewPoints(IntPtr self, string name, int tsi);
        [DllImport("abci")] static extern aeObject aeNewPolyMesh(IntPtr self, string name, int tsi);
        [DllImport("abci")] static extern void aeXformWriteSample(IntPtr self, ref aeXformData data);
        [DllImport("abci")] static extern void aeCameraWriteSample(IntPtr self, ref aeCameraData data);
        [DllImport("abci")] static extern void aePolyMeshWriteSample(IntPtr self, ref aePolyMeshData data);
        [DllImport("abci")] static extern int aePolyMeshAddFaceSet(IntPtr self, string name);
        [DllImport("abci")] static extern void aePointsWriteSample(IntPtr self, ref aePointsData data);
        [DllImport("abci")] static extern aeProperty aeNewProperty(IntPtr self, string name, aePropertyType type);
        [DllImport("abci")] static extern void aeMarkForceInvisible(IntPtr self);
        #endregion
    }

    public struct aeProperty
    {
        public IntPtr self;

        public void WriteArraySample(IntPtr data, int num_data) { aePropertyWriteArraySample(self, data, num_data); }

        public void WriteScalarSample(ref float data) { aePropertyWriteScalarSample(self, ref data); }
        public void WriteScalarSample(ref int data) { aePropertyWriteScalarSample(self, ref data); }
        public void WriteScalarSample(ref Bool data) { aePropertyWriteScalarSample(self, ref data); }
        public void WriteScalarSample(ref Vector2 data) { aePropertyWriteScalarSample(self, ref data); }
        public void WriteScalarSample(ref Vector3 data) { aePropertyWriteScalarSample(self, ref data); }
        public void WriteScalarSample(ref Vector4 data) { aePropertyWriteScalarSample(self, ref data); }
        public void WriteScalarSample(ref Matrix4x4 data) { aePropertyWriteScalarSample(self, ref data); }

        #region internal
        [DllImport("abci")] static extern void aePropertyWriteArraySample(IntPtr self, IntPtr data, int num_data);

        // all of these are  IntPtr version. just for convenience.
        [DllImport("abci")] static extern void aePropertyWriteScalarSample(IntPtr self, ref float data);
        [DllImport("abci")] static extern void aePropertyWriteScalarSample(IntPtr self, ref int data);
        [DllImport("abci")] static extern void aePropertyWriteScalarSample(IntPtr self, ref Bool data);
        [DllImport("abci")] static extern void aePropertyWriteScalarSample(IntPtr self, ref Vector2 data);
        [DllImport("abci")] static extern void aePropertyWriteScalarSample(IntPtr self, ref Vector3 data);
        [DllImport("abci")] static extern void aePropertyWriteScalarSample(IntPtr self, ref Vector4 data);
        [DllImport("abci")] static extern void aePropertyWriteScalarSample(IntPtr self, ref Matrix4x4 data);
        #endregion
    }



    public partial class AbcAPI
    {
        public static void aeWaitMaxDeltaTime()
        {
            var next = Time.unscaledTime + Time.maximumDeltaTime;
            while (Time.realtimeSinceStartup < next)
                System.Threading.Thread.Sleep(1);
        }
    }
}
