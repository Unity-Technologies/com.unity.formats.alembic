using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UTJ.Alembic
{
    public partial class AbcAPI
    {
        public enum aiAspectRatioMode
        {
            CurrentResolution = 0,
            DefaultResolution =1,
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
            public float aspectRatio;
            public Bool swapHandedness;
            public Bool swapFaceWinding;
            public Bool interpolateSamples;
            public Bool turnQuadEdges;
            public float vertexMotionScale;
            public int splitUnit;

            public void SetDefaults()
            {
                swapHandedness = true;
                swapFaceWinding = false;
                normalsMode = aiNormalsMode.ComputeIfMissing;
                tangentsMode = aiTangentsMode.None;
                aspectRatio = -1.0f;
                interpolateSamples = true;
                turnQuadEdges = false;
                vertexMotionScale = 1.0f;
                splitUnit = 65000;
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
            public Vector3 size;
        }

        public struct aiSubmeshData
        {
            public IntPtr indices;
        }

        public struct aiXFormData
        {
            public Vector3 translation;
            public Quaternion rotation;
            public Vector3 scale;
            public Bool inherits;
        }

        public struct aiCameraData
        {
            public float nearClippingPlane;
            public float farClippingPlane;
            public float fieldOfView;   // in degree. vertical one
            public float aspectRatio;

            public float focusDistance; // in cm
            public float focalLength;   // in mm
            public float aperture;      // in cm. vertical one
        }

        public struct aiContext
        {
            public IntPtr ptr;
            public static implicit operator bool(aiContext v) { return v.ptr != IntPtr.Zero; }
        }

        public struct aiObject
        {
            public IntPtr ptr;
            public static implicit operator bool(aiObject v) { return v.ptr != IntPtr.Zero; }
        }

        public struct aiSchema
        {
            public IntPtr ptr;
            public static implicit operator bool(aiSchema v) { return v.ptr != IntPtr.Zero; }

            public bool isConstant
            {
                get { return aiSchemaIsConstant(this); }
            }

            public bool dirty
            {
                get { return aiSchemaIsDirty(this); }
            }

            public aiSample sample
            {
                get { return aiSchemaGetSample(this); }
            }

            public void markForceUpdate() { aiSchemaMarkForceUpdate(this); }

            [DllImport("abci")] public static extern Bool aiSchemaIsConstant(aiSchema schema);
            [DllImport("abci")] public static extern Bool aiSchemaIsDirty(aiSchema schema);
            [DllImport("abci")] public static extern aiSample aiSchemaGetSample(aiSchema schema);
            [DllImport("abci")] public static extern void aiSchemaMarkForceUpdate(aiSchema schema);
        }

        public struct aiProperty
        {
            public IntPtr ptr;
            public static implicit operator bool(aiProperty v) { return v.ptr != IntPtr.Zero; }
        }

        public struct aiSample
        {
            public IntPtr ptr;
            public static implicit operator bool(aiSample v) { return v.ptr != IntPtr.Zero; }
        }

        public struct aiPointsSummary
        {
            public Bool hasVelocity;
            public Bool positionIsConstant;
            public Bool idIsConstant;
            public int peakCount;
            public ulong minID;
            public ulong maxID;
            public Vector3 boundsCenter;
            public Vector3 boundsExtents;
        };

        public struct aiPointsData
        {
            public IntPtr positions;
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


        [DllImport("abci")] public static extern            aiSampleSelector aiTimeToSampleSelector(float time);
        [DllImport("abci")] public static extern void       aiCleanup();
        [DllImport("abci")] public static extern void       aiClearContextsWithPath(string path);
        [DllImport("abci")] public static extern aiContext  aiCreateContext(int uid);
        [DllImport("abci")] public static extern void       aiDestroyContext(aiContext ctx);
        [DllImport("abci")] public static extern Bool       aiLoad(aiContext ctx, string path);
        [DllImport("abci")] public static extern void       aiSetConfig(aiContext ctx, ref aiConfig conf);
        [DllImport("abci")] public static extern float      aiGetStartTime(aiContext ctx);
        [DllImport("abci")] public static extern float      aiGetEndTime(aiContext ctx);
        [DllImport("abci")] public static extern int        aiGetFrameCount(aiContext ctx);
        [DllImport("abci")] public static extern aiObject   aiGetTopObject(aiContext ctx);
        [DllImport("abci")] public static extern int        aiGetNumChildren(aiObject obj);
        [DllImport("abci")] public static extern aiObject   aiGetChild(aiObject obj, int i);
        [DllImport("abci")] public static extern void       aiSetEnabled(aiObject obj, Bool v);
        [DllImport("abci")] public static extern void       aiUpdateSamples(aiContext ctx, float time);
        [DllImport("abci")] private static extern IntPtr    aiGetNameS(aiObject obj);
        public static string aiGetName(aiObject obj)      { return Marshal.PtrToStringAnsi(aiGetNameS(obj)); }
        [DllImport("abci")] public static extern aiSample   aiSchemaUpdateSample(aiSchema schema, ref aiSampleSelector ss);

        [DllImport("abci")] public static extern aiSchema   aiGetXForm(aiObject obj);
        [DllImport("abci")] public static extern void       aiXFormGetData(aiSample sample, ref aiXFormData data);
        [DllImport("abci")] public static extern aiSchema   aiGetPolyMesh(aiObject obj);
        [DllImport("abci")] public static extern void       aiPolyMeshGetSummary(aiSchema schema, ref aiMeshSummary summary);
        [DllImport("abci")] public static extern void       aiPolyMeshGetSampleSummary(aiSample sample, ref aiMeshSampleSummary summary);
        [DllImport("abci")] public static extern int        aiPolyMeshGetSplitSummary(aiSample sample, int splitIndex, ref aiMeshSplitSummary dst);
        [DllImport("abci")] public static extern void       aiPolyMeshGetSubmeshSummary(aiSample sample, int splitIndex, int submeshIndex, ref aiSubmeshSummary smi);
        [DllImport("abci")] public static extern void       aiPolyMeshFillVertexBuffer(aiSample sample, int splitIndex, ref aiPolyMeshData data);
        [DllImport("abci")] public static extern void       aiPolyMeshFillSubmeshIndices(aiSample sample, int splitIndex, int submeshIndex, ref aiSubmeshData data);
        [DllImport("abci")] public static extern aiSchema   aiGetCamera(aiObject obj);
        [DllImport("abci")] public static extern void       aiCameraGetData(aiSample sample, ref aiCameraData data);
        [DllImport("abci")] public static extern aiSchema   aiGetPoints(aiObject obj);
        [DllImport("abci")] public static extern void       aiPointsSetSort(aiSchema schema, Bool v);
        [DllImport("abci")] public static extern void       aiPointsSetSortBasePosition(aiSchema schema, Vector3 v);
        [DllImport("abci")] public static extern void       aiPointsGetSummary(aiSchema schema, ref aiPointsSummary summary);
        [DllImport("abci")] public static extern void       aiPointsCopyData(aiSample sample, ref aiPointsData data);
        [DllImport("abci")] public static extern int             aiSchemaGetNumProperties(aiSchema schema);
        [DllImport("abci")] public static extern aiProperty      aiSchemaGetPropertyByIndex(aiSchema schema, int i);
        [DllImport("abci")] public static extern aiProperty      aiSchemaGetPropertyByName(aiSchema schema, string name);
        [DllImport("abci")] public static extern IntPtr          aiPropertyGetNameS(aiProperty prop);
        [DllImport("abci")] public static extern aiPropertyType  aiPropertyGetType(aiProperty prop);
        [DllImport("abci")] public static extern void            aiPropertyGetData(aiProperty prop, aiPropertyData oData);

        public static void aiEnumerateChild(aiObject obj, Action<aiObject> act)
        {
            int numChildren = aiGetNumChildren(obj);
            for (int ci = 0; ci < numChildren; ++ci)
                act.Invoke(aiGetChild(obj, ci));
        }
    }

    public class AbcUtils
    {
    #if UNITY_EDITOR
        
        static MethodInfo s_GetBuiltinExtraResourcesMethod;

        public static Material GetDefaultMaterial()
        {
            if (s_GetBuiltinExtraResourcesMethod == null)
            {
                BindingFlags bfs = BindingFlags.NonPublic | BindingFlags.Static;
                s_GetBuiltinExtraResourcesMethod = typeof(EditorGUIUtility).GetMethod("GetBuiltinExtraResource", bfs);
            }
            return (Material)s_GetBuiltinExtraResourcesMethod.Invoke(null, new object[] { typeof(Material), "Default-Material.mat" });
        }

    #endif
    }
}
