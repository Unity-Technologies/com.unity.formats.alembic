using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{
    public partial class AbcAPI
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

        public enum aeXFormType
        {
            Matrix,
            TRS,
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

        public struct aeContext { public IntPtr ptr; }
        public struct aeObject { public IntPtr ptr; }
        public struct aeProperty { public IntPtr ptr; }

        [Serializable]
        public struct aeConfig
        {
            public aeArchiveType archiveType;
            public aeTimeSamplingType timeSamplingType;
            public float startTime;
            public float frameRate;
            public aeXFormType xformType;
            public Bool swapHandedness;
            public Bool swapFaces;
            public float scaleFactor;

            public static aeConfig default_value
            {
                get
                {
                    return new aeConfig
                    {
                        archiveType = aeArchiveType.Ogawa,
                        timeSamplingType = aeTimeSamplingType.Uniform,
                        startTime = 0.0f,
                        frameRate = 30.0f,
                        xformType = aeXFormType.TRS,
                        swapHandedness = true,
                        swapFaces = false,
                        scaleFactor = 100.0f,
                    };
                }
            }
        }

        public struct aeXFormData
        {
            public Vector3 translation;
            public Quaternion rotation;
            public Vector3 scale;
            public Bool inherits;
        }

        public struct aePointsData
        {
            public IntPtr positions; // Vector3*
            public IntPtr velocities; // Vector3*. can be null
            public IntPtr ids; // ulong*. can be null
            public int count;
        }

        public struct aePolyMeshData
        {
            public IntPtr positions;        // Vector3*
            public IntPtr velocities;       // Vector3*. can be null
            public IntPtr normals;          // Vector3*. can be null
            public IntPtr uvs;              // Vector2*. can be null

            public IntPtr indices;          // int*. 
            public IntPtr normalIndices;    // int*. if null, assume same as indices
            public IntPtr uvIndices;        // int*. if null, assume same as indices

            public IntPtr faces;            // int*. if null, assume all faces are triangles

            public int positionCount;       
            public int normalCount;         // if 0, assume same as positionCount
            public int uvCount;             // if 0, assume same as positionCount

            public int indexCount;
            public int normalIndexCount;    // if 0, assume same as indexCount
            public int uvIndexCount;        // if 0, assume same as indexCount

            public int faceCount;
        }

        public struct aeCameraData
        {
            public float nearClippingPlane;
            public float farClippingPlane;
            public float fieldOfView;   // in degree. relevant only if focalLength==0.0 (default)
            public float aspectRatio;

            public float focusDistance; // in cm
            public float focalLength;   // in mm. if 0.0f, automatically computed by aperture and fieldOfView. alembic's default value is 0.035f.
            public float aperture;      // in cm. vertical one

            public static aeCameraData default_value
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


        [DllImport("abci")] public static extern aeContext   aeCreateContext();
        [DllImport("abci")] public static extern void        aeDestroyContext(aeContext ctx);

        [DllImport("abci")] public static extern void        aeSetConfig(aeContext ctx, ref aeConfig conf);
        [DllImport("abci")] public static extern Bool        aeOpenArchive(aeContext ctx, string path);
        [DllImport("abci")] public static extern aeObject    aeGetTopObject(aeContext ctx);
        [DllImport("abci")] public static extern int         aeAddTimeSampling(aeContext ctx, float start_time);
        // relevant only if timeSamplingType is acyclic. if tsi==-1, add time to all time samplings.
        [DllImport("abci")] public static extern void        aeAddTime(aeContext ctx, float time, int tsi = -1);

        [DllImport("abci")] public static extern aeObject    aeNewXForm(aeObject parent, string name, int tsi = 1);
        [DllImport("abci")] public static extern aeObject    aeNewCamera(aeObject parent, string name, int tsi = 1);
        [DllImport("abci")] public static extern aeObject    aeNewPoints(aeObject parent, string name, int tsi = 1);
        [DllImport("abci")] public static extern aeObject    aeNewPolyMesh(aeObject parent, string name, int tsi = 1);
        [DllImport("abci")] public static extern void        aeXFormWriteSample(aeObject obj, ref aeXFormData data);
        [DllImport("abci")] public static extern void        aePointsWriteSample(aeObject obj, ref aePointsData data);
        [DllImport("abci")] public static extern void        aePolyMeshWriteSample(aeObject obj, ref aePolyMeshData data);
        [DllImport("abci")] public static extern void        aeCameraWriteSample(aeObject obj, ref aeCameraData data);

        [DllImport("abci")] public static extern aeProperty  aeNewProperty(aeObject parent, string name, aePropertyType type);
        [DllImport("abci")] public static extern void        aePropertyWriteArraySample(aeProperty prop, IntPtr data, int num_data);
        [DllImport("abci")] public static extern void        aePropertyWriteScalarSample(aeProperty prop, IntPtr data);

        // all of these are same as IntPtr version. just for convenience.
        [DllImport("abci")] public static extern void        aePropertyWriteScalarSample(aeProperty prop, ref float data);
        [DllImport("abci")] public static extern void        aePropertyWriteScalarSample(aeProperty prop, ref int data);
        [DllImport("abci")] public static extern void        aePropertyWriteScalarSample(aeProperty prop, ref Bool data);
        [DllImport("abci")] public static extern void        aePropertyWriteScalarSample(aeProperty prop, ref Vector2 data);
        [DllImport("abci")] public static extern void        aePropertyWriteScalarSample(aeProperty prop, ref Vector3 data);
        [DllImport("abci")] public static extern void        aePropertyWriteScalarSample(aeProperty prop, ref Vector4 data);
        [DllImport("abci")] public static extern void        aePropertyWriteScalarSample(aeProperty prop, ref Matrix4x4 data);

        public static void aeWait(float wt)
        {
            while (Time.realtimeSinceStartup - Time.unscaledTime < wt) {
                System.Threading.Thread.Sleep(1);
            }
        }

        public static void aeWaitMaxDeltaTime()
        {
            aeWait(Time.maximumDeltaTime);
        }
    }
}
