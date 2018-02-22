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
            Smooth,
            Split
        }

        public enum aiTopologyVariance
        {
            Constant,
            Homogeneous,
            Heterogeneous
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

        public delegate void aiNodeEnumerator(aiObject obj, IntPtr userData);
        public delegate void aiConfigCallback(IntPtr _this, ref aiConfig config);
        public delegate void aiSampleCallback(IntPtr _this, aiSample sample, Bool topologyChanged);

        public struct aiConfig
        {
            public Bool swapHandedness;
            public Bool swapFaceWinding;
            public aiNormalsMode normalsMode;
            public aiTangentsMode tangentsMode;
            public Bool cacheTangentsSplits;
            public float aspectRatio;
            public Bool forceUpdate;
            public Bool cacheSamples;
            public Bool shareVertices;
            public Bool treatVertexExtraDataAsStatics;
            public Bool interpolateSamples;
            public Bool turnQuadEdges;
            public float vertexMotionScale;
            public Bool use32BitsIndexBuffer;

            public void SetDefaults()
            {
                swapHandedness = true;
                swapFaceWinding = false;
                normalsMode = aiNormalsMode.ComputeIfMissing;
                tangentsMode = aiTangentsMode.None;
                cacheTangentsSplits = true;
                aspectRatio = -1.0f;
                forceUpdate = false;
                cacheSamples = false;
                shareVertices = true;
                treatVertexExtraDataAsStatics = false;
                interpolateSamples = true;
                turnQuadEdges = false;
                vertexMotionScale = 1.0f;
                use32BitsIndexBuffer = false;
            }
        }

        public struct aiSampleSelector
        {
            public ulong requestedIndex;
            public double requestedTime;
            public int requestedTimeIndexType;
        }

        public struct aiFacesets
        {
            public int count;
            public IntPtr faceCounts;
            public IntPtr faceIndices;
        }

        public struct aiMeshSummary
        {
            public aiTopologyVariance topologyVariance;
            public int peakVertexCount;
            public int peakIndexCount;
            public int peakTriangulatedIndexCount;
            public int peakSubmeshCount;
        }

        public struct aiMeshSampleSummary
        {
            public int splitCount;
            public Bool hasNormals;
            public Bool hasUVs;
            public Bool hasTangents;
            public Bool hasVelocities;
        }

        public struct aiPolyMeshData
        {
            public IntPtr positions;
            public IntPtr velocities;
            public IntPtr interpolatedVelocitiesXY;
            public IntPtr interpolatedVelocitiesZ;
            public IntPtr normals;
            public IntPtr uvs;
            public IntPtr tangents;

            public IntPtr indices;
            public IntPtr normalIndices;
            public IntPtr uvIndices;
            public IntPtr faces;

            public int positionCount;
            public int normalCount;
            public int uvCount;

            public int indexCount;
            public int normalIndexCount;
            public int uvIndexCount;
            public int faceCount;

            public int triangulatedIndexCount;

            public Vector3 center;
            public Vector3 size;
        }

        public struct aiSubmeshSummary
        {
            public int index;
            public int splitIndex;
            public int splitSubmeshIndex;
            public int facesetIndex;
            public int triangleCount;
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
            public System.IntPtr ptr;
            public static implicit operator bool(aiContext v) { return v.ptr != IntPtr.Zero; }
        }

        public struct aiObject
        {
            public System.IntPtr ptr;
            public static implicit operator bool(aiObject v) { return v.ptr != IntPtr.Zero; }
        }

        public struct aiSchema
        {
            public System.IntPtr ptr;
            public static implicit operator bool(aiSchema v) { return v.ptr != IntPtr.Zero; }
        }

        public struct aiProperty
        {
            public System.IntPtr ptr;
            public static implicit operator bool(aiProperty v) { return v.ptr != IntPtr.Zero; }
        }

        public struct aiSample
        {
            public System.IntPtr ptr;
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

        [DllImport("libdl")] static extern IntPtr dlopen(string filename, int flags);
        [DllImport("libdl")] static extern IntPtr dlsym(IntPtr handle, string symbol);
        const int RTLD_LAZY = 1;
        const int RTLD_NOW = 2;
        const int RTLD_LOCAL = 4;
        const int RTLD_GLOBAL = 8;
        const int RTLD_FIRST = 256;

        const string libpath = "UnityPackageManager/com.unity.alembic/Runtime/Plugins/x86_64/abci.bundle/Contents/MacOS/abci";
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


        class ImportContext
        {
            public AlembicTreeNode alembicTreeNode;
            public aiSampleSelector ss;
            public bool createMissingNodes;
            public List<aiObject> objectsToDelete;
        }

        public static float GetAspectRatio(aiAspectRatioMode mode)
        {
            if (mode == aiAspectRatioMode.CameraAperture)
            {
                return 0.0f;
            }
            else if (mode == aiAspectRatioMode.CurrentResolution)
            {
                return (float) Screen.width / (float) Screen.height;
            }
            else
            {
    #if UNITY_EDITOR
                return (float) PlayerSettings.defaultScreenWidth / (float) PlayerSettings.defaultScreenHeight;
    #else
                // fallback on current resoltution
                return (float) Screen.width / (float) Screen.height;
    #endif
            }
        }
        
        public static void UpdateAbcTree(aiContext ctx, AlembicTreeNode node, float time, bool createMissingNodes=true)
        {
            var ic = new ImportContext
            {
                alembicTreeNode = node,
                ss = aiTimeToSampleSelector(time),
                createMissingNodes = createMissingNodes,
                objectsToDelete = new List<aiObject>()
            };

            GCHandle hdl = GCHandle.Alloc(ic);

            aiObject top = aiGetTopObject(ctx);

            if (top.ptr != (IntPtr)0)
            {
                aiEnumerateChild(top, ImportEnumerator, GCHandle.ToIntPtr(hdl));

                foreach (aiObject obj in ic.objectsToDelete)
                {
                    aiDestroyObject(ctx, obj);
                }
            }
        }

        static void ImportEnumerator(aiObject obj, IntPtr userData)
        {
            var ic = GCHandle.FromIntPtr(userData).Target as ImportContext;
            AlembicTreeNode treeNode = ic.alembicTreeNode;

            // Get child. create if needed and allowed.
            string childName = aiGetName(obj);

            // Find targetted child GameObj
            AlembicTreeNode childTreeNode = null;
            GameObject childGO = null;
           
            var childTransf = treeNode.linkedGameObj==null ? null : treeNode.linkedGameObj.transform.Find(childName);
            if (childTransf == null)
            {
                if (!ic.createMissingNodes)
                {
                    ic.objectsToDelete.Add(obj);
                    return;
                }

                childGO = new GameObject {name = childName};
                var trans = childGO.GetComponent<Transform>();
                trans.parent = treeNode.linkedGameObj.transform;
                trans.localPosition = Vector3.zero;
                trans.localEulerAngles = Vector3.zero;
                trans.localScale = Vector3.one;
            }
            else
                childGO = childTransf.gameObject;            
            
            childTreeNode = new AlembicTreeNode() {linkedGameObj = childGO, streamDescriptor = treeNode.streamDescriptor };
            treeNode.children.Add(childTreeNode);

            // Update
            AlembicElement elem = null;
            aiSchema schema = default(aiSchema);

            if (aiGetXForm(obj))
            {
                elem = childTreeNode.GetOrAddAlembicObj<AlembicXForm>();
                schema = aiGetXForm(obj);
            }
            else if (aiGetPolyMesh(obj))
            {
                elem = childTreeNode.GetOrAddAlembicObj<AlembicMesh>();
                schema = aiGetPolyMesh(obj);
            }
            else if (aiGetCamera(obj))
            {
                elem = childTreeNode.GetOrAddAlembicObj<AlembicCamera>();
                schema = aiGetCamera(obj);
            }
            else if (aiGetPoints(obj))
            {
                elem = childTreeNode.GetOrAddAlembicObj<AlembicPoints>();
                schema = aiGetPoints(obj);
            }

            if (elem != null)
            {
                elem.AbcSetup(obj, schema);
                elem.AbcUpdateConfig();
                aiSchemaUpdateSample(schema, ref ic.ss);
                elem.AbcUpdate();
            }

            ic.alembicTreeNode = childTreeNode;
            aiEnumerateChild(obj, ImportEnumerator, userData);
            ic.alembicTreeNode = treeNode;
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
