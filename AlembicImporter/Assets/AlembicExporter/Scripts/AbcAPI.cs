using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class AbcAPI
{
    public enum aeArchiveType
    {
        HDF5,
        Ogawa,
    };

    public enum aeTypeSamplingType
    {
        Uniform,
        Acyclic,
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
        [MarshalAs(UnmanagedType.U4)]
        public aeArchiveType archiveType;

        [MarshalAs(UnmanagedType.U4)]
        public aeTypeSamplingType timeSamplingType;

        [Tooltip("Start time on Alembic.")]
        public float startTime;

        [Tooltip("Frame rate on Alembic. Relevant only if timeSamplingType is Uniform")]
        public float frameRate;

        [MarshalAs(UnmanagedType.U4)]
        public aeXFormType xformType;

        [Tooltip("Swap right-hand space and left-hand space")]
        [MarshalAs(UnmanagedType.U1)]
        public bool swapHandedness;

        [Tooltip("Swap triangle indices")]
        [MarshalAs(UnmanagedType.U1)]
        public bool swapFaces;

        [Tooltip("Global scale for unit conversion.")]
        public float scale;


        public static aeConfig default_value
        {
            get
            {
                return new aeConfig
                {
                    archiveType = aeArchiveType.Ogawa,
                    timeSamplingType = aeTypeSamplingType.Uniform,
                    startTime = 0.0f,
                    frameRate = 30.0f,
                    xformType = aeXFormType.TRS,
                    swapHandedness = true,
                    swapFaces = false,
                    scale = 1.0f,
                };
            }
        }
    }

    public struct aeXFormSampleData
    {
        public Vector3 translation;
        public Vector3 rotationAxis;
        public float rotationAngle;
        public Vector3 scale;
        [MarshalAs(UnmanagedType.U1)] public bool inherits;
    }

    public struct aePointsSampleData
    {
        public IntPtr positions; // Vector3*
        public IntPtr velocities; // Vector3*. can be null
        public IntPtr ids; // ulong*. can be null
        public int count;
    }

    public struct aePolyMeshSampleData
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

    public struct aeCameraSampleData
    {
        public float nearClippingPlane;
        public float farClippingPlane;
        public float fieldOfView; // degree. relevant only if focusDistance==0.0 (default)
        public float aspectRatio;

        // if 0.0f, automatically computed by aperture and fieldOfView. alembic's default value is 0.05f.
        public float focalLength;
        public float focusDistance;
        public float aperture;

        public static aeCameraSampleData default_value
        {
            get
            {
                return new aeCameraSampleData
                {
                    nearClippingPlane = 0.3f,
                    farClippingPlane = 1000.0f,
                    fieldOfView = 60.0f,
                    aspectRatio = 16.0f / 9.0f,
                    focalLength = 0.0f,
                    focusDistance = 5.0f,
                    aperture = 2.4f,
                };
            }
        }
    }


    [DllImport("AlembicExporter")] public static extern aeContext   aeCreateContext(ref aeConfig conf);
    [DllImport("AlembicExporter")] public static extern void        aeDestroyContext(aeContext ctx);

    [DllImport("AlembicExporter")] public static extern bool        aeOpenArchive(aeContext ctx, string path);
    [DllImport("AlembicExporter")] public static extern aeObject    aeGetTopObject(aeContext ctx);
    [DllImport("AlembicExporter")] public static extern void        aeAddTime(aeContext ctx, float time); // relevant only if timeSamplingType is acyclic

    [DllImport("AlembicExporter")] public static extern aeObject    aeNewXForm(aeObject parent, string name);
    [DllImport("AlembicExporter")] public static extern aeObject    aeNewCamera(aeObject parent, string name);
    [DllImport("AlembicExporter")] public static extern aeObject    aeNewPoints(aeObject parent, string name);
    [DllImport("AlembicExporter")] public static extern aeObject    aeNewPolyMesh(aeObject parent, string name);
    [DllImport("AlembicExporter")] public static extern void        aeXFormWriteSample(aeObject obj, ref aeXFormSampleData data);
    [DllImport("AlembicExporter")] public static extern void        aePointsWriteSample(aeObject obj, ref aePointsSampleData data);
    [DllImport("AlembicExporter")] public static extern void        aePolyMeshWriteSample(aeObject obj, ref aePolyMeshSampleData data);
    [DllImport("AlembicExporter")] public static extern void        aeCameraWriteSample(aeObject obj, ref aeCameraSampleData data);

    [DllImport("AlembicExporter")] public static extern aeProperty  aeNewProperty(aeObject parent, string name, aePropertyType type);
    [DllImport("AlembicExporter")] public static extern void        aePropertyWriteArraySample(aeProperty prop, IntPtr data, int num_data);
    [DllImport("AlembicExporter")] public static extern void        aePropertyWriteScalarSample(aeProperty prop, IntPtr data);

    // all of these are same as IntPtr version. just for convenience.
    [DllImport("AlembicExporter")] public static extern void        aePropertyWriteScalarSample(aeProperty prop, ref float data);
    [DllImport("AlembicExporter")] public static extern void        aePropertyWriteScalarSample(aeProperty prop, ref int data);
    [DllImport("AlembicExporter")] public static extern void        aePropertyWriteScalarSample(aeProperty prop, ref bool data);
    [DllImport("AlembicExporter")] public static extern void        aePropertyWriteScalarSample(aeProperty prop, ref Vector2 data);
    [DllImport("AlembicExporter")] public static extern void        aePropertyWriteScalarSample(aeProperty prop, ref Vector3 data);
    [DllImport("AlembicExporter")] public static extern void        aePropertyWriteScalarSample(aeProperty prop, ref Vector4 data);
    [DllImport("AlembicExporter")] public static extern void        aePropertyWriteScalarSample(aeProperty prop, ref Matrix4x4 data);

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
