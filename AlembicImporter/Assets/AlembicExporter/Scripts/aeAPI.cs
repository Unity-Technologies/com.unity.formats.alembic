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

public class aeAPI
{
    public enum aeArchiveType
    {
        HDF5,
        Ogawa,
    };

    public struct aeContext { public IntPtr ptr; }
    public struct aeObject  { public IntPtr ptr; }

    [Serializable]
    public struct aeConfig
    {
        [MarshalAs(UnmanagedType.U4)] public aeArchiveType archiveType;
        [MarshalAs(UnmanagedType.U1)] public bool swapHandedness;


        public static aeConfig default_value
        {
            get
            {
                return new aeConfig
                {
                    archiveType = aeArchiveType.Ogawa,
                    swapHandedness = true,
                };
            }
        }
    }

    public struct aeXFormSampleData
    {
        public Vector3 translation;
        public Vector3 rotation_axis;
        public float rotation_angle;
        public Vector3 scale;
        [MarshalAs(UnmanagedType.U1)] public bool inherits;
    }

    public struct aePointsSampleData
    {
        IntPtr positions; // Vector3*
        int count;
    }

    public struct aePolyMeshSampleData
    {
        public IntPtr positions; // Vector3*
        public IntPtr normals; // Vector3*. can be null
        public IntPtr uvs; // Vector2*. can be null
        public IntPtr indices; // int*
        public IntPtr faces; // int*. can be null. assume all faces are triangles if null

        public int vertex_count;
        public int index_count;
        public int face_count;
    }

    public struct aeCameraSampleData
    {
        public float nearClippingPlane;
        public float farClippingPlane;
        public float fieldOfView;
        public float focusDistance;
        public float focalLength;
    }

    [DllImport ("AlembicExporter")] public static extern void       aeCleanup();

    [DllImport ("AlembicExporter")] public static extern aeContext  aeCreateContext(ref aeConfig conf);
    [DllImport ("AlembicExporter")] public static extern void       aeDestroyContext(aeContext ctx);
    [DllImport ("AlembicExporter")] public static extern bool       aeOpenArchive(aeContext ctx, string path);

    [DllImport ("AlembicExporter")] public static extern void       aeSetTime(aeContext ctx, float time);
    [DllImport ("AlembicExporter")] public static extern aeObject   aeGetTopObject(aeContext ctx);

    [DllImport ("AlembicExporter")] public static extern aeObject   aeNewXForm(aeObject parent, string name);
    [DllImport ("AlembicExporter")] public static extern aeObject   aeNewCamera(aeObject parent, string name);
    [DllImport ("AlembicExporter")] public static extern aeObject   aeNewPoints(aeObject parent, string name);
    [DllImport ("AlembicExporter")] public static extern aeObject   aeNewPolyMesh(aeObject parent, string name);
    [DllImport ("AlembicExporter")] public static extern void       aeXFormWriteSample(aeObject obj, ref aeXFormSampleData data);
    [DllImport ("AlembicExporter")] public static extern void       aePointsWriteSample(aeObject obj, ref aePointsSampleData data);
    [DllImport ("AlembicExporter")] public static extern void       aePolyMeshWriteSample(aeObject obj, ref aePolyMeshSampleData data);
    [DllImport ("AlembicExporter")] public static extern void       aeCameraWriteSample(aeObject obj, ref aeCameraSampleData data);
}
