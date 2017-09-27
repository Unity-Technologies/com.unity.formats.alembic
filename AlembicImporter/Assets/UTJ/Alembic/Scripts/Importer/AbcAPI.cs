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
            DefaultResolution,
            CameraAperture
        };

        public enum aiAspectRatioModeOverride
        {
            InheritStreamSetting = -1,
            CurrentResolution,
            DefaultResolution,
            CameraAperture
        };

        public enum aiNormalsMode
        {
            ReadFromFile = 0,
            ComputeIfMissing,
            AlwaysCompute,
            Ignore
        }
        
        public enum aiNormalsModeOverride
        {
            InheritStreamSetting = -1,
            ReadFromFile,
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

        public enum aiTangentsModeOverride
        {
            InheritStreamSetting = -1,
            None,
            Smooth,
            Split
        }

        public enum aiFaceWindingOverride
        {
            InheritStreamSetting = -1,
            Preserve,
            Swap
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


        public enum aiTextureFormat
        {
            Unknown,
            ARGB32,
            ARGB2101010,
            RHalf,
            RGHalf,
            ARGBHalf,
            RFloat,
            RGFloat,
            ARGBFloat,
            RInt,
            RGInt,
            ARGBInt,
        }

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
                treatVertexExtraDataAsStatics = true;
                interpolateSamples = true;
                turnQuadEdges = false;
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
            // normalIndices, uvIndices, faces and their counts are
            // available only when get by aiPolyMeshCopyData() without triangulating

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


        [DllImport("abci")] public static extern            aiSampleSelector aiTimeToSampleSelector(float time);
        [DllImport("abci")] public static extern            aiSampleSelector aiIndexToSampleSelector(int index);

        [DllImport("abci")] public static extern void       aiEnableFileLog(Bool on, string path);

        [DllImport("abci")] public static extern void       aiCleanup();
        [DllImport("abci")] public static extern void       clearContextsWithPath(string path);
        [DllImport("abci")] public static extern aiContext  aiCreateContext(int uid);
        [DllImport("abci")] public static extern void       aiDestroyContext(aiContext ctx);
        
        [DllImport("abci")] public static extern Bool       aiLoad(aiContext ctx, string path);
        [DllImport("abci")] public static extern void       aiSetConfig(aiContext ctx, ref aiConfig conf);
        [DllImport("abci")] public static extern float      aiGetStartTime(aiContext ctx);
        [DllImport("abci")] public static extern float      aiGetEndTime(aiContext ctx);
        [DllImport("abci")] public static extern aiObject   aiGetTopObject(aiContext ctx);
        [DllImport("abci")] public static extern void       aiDestroyObject(aiContext ctx, aiObject obj);

        [DllImport("abci")] public static extern void       aiUpdateSamples(aiContext ctx, float time);
        [DllImport("abci")] public static extern void       aiEnumerateChild(aiObject obj, aiNodeEnumerator e, IntPtr userData);
        [DllImport("abci")] private static extern IntPtr    aiGetNameS(aiObject obj);
        [DllImport("abci")] private static extern IntPtr    aiGetFullNameS(aiObject obj);
        public static string aiGetName(aiObject obj)      { return Marshal.PtrToStringAnsi(aiGetNameS(obj)); }
        public static string aiGetFullName(aiObject obj)  { return Marshal.PtrToStringAnsi(aiGetFullNameS(obj)); }
        
        [DllImport("abci")] public static extern void       aiSchemaSetSampleCallback(aiSchema schema, aiSampleCallback cb, IntPtr arg);
        [DllImport("abci")] public static extern void       aiSchemaSetConfigCallback(aiSchema schema, aiConfigCallback cb, IntPtr arg);
        [DllImport("abci")] public static extern aiSample   aiSchemaUpdateSample(aiSchema schema, ref aiSampleSelector ss);
        [DllImport("abci")] public static extern aiSample   aiSchemaGetSample(aiSchema schema, ref aiSampleSelector ss);

        [DllImport("abci")] public static extern aiSchema   aiGetXForm(aiObject obj);
        [DllImport("abci")] public static extern void       aiXFormGetData(aiSample sample, ref aiXFormData data);

        [DllImport("abci")] public static extern aiSchema   aiGetPolyMesh(aiObject obj);
        [DllImport("abci")] public static extern void       aiPolyMeshGetSummary(aiSchema schema, ref aiMeshSummary summary);
        [DllImport("abci")] public static extern void       aiPolyMeshGetSampleSummary(aiSample sample, ref aiMeshSampleSummary summary, Bool forceRefresh);
        [DllImport("abci")] public static extern int        aiPolyMeshGetVertexBufferLength(aiSample sample, int splitIndex);
        [DllImport("abci")] public static extern void       aiPolyMeshFillVertexBuffer(aiSample sample, int splitIndex, ref aiPolyMeshData data);
        [DllImport("abci")] public static extern int        aiPolyMeshPrepareSubmeshes(aiSample sample, ref aiFacesets facesets);
        [DllImport("abci")] public static extern int        aiPolyMeshGetSplitSubmeshCount(aiSample sample, int splitIndex);
        [DllImport("abci")] public static extern Bool       aiPolyMeshGetNextSubmesh(aiSample sample, ref aiSubmeshSummary smi);
        [DllImport("abci")] public static extern void       aiPolyMeshFillSubmeshIndices(aiSample sample, ref aiSubmeshSummary smi, ref aiSubmeshData data);

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
        [DllImport("abci")] public static extern void            aiPropertyGetData(aiProperty prop, aiPropertyData o_data);





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

    #if UNITY_EDITOR

        public class ImportParams
        {
            public bool swapHandedness = true;
            public bool swapFaceWinding = false;
        }

        static string MakeRelativePath(string path)
        {
            Uri pathToAssets = new Uri(Application.streamingAssetsPath + "/");
            return pathToAssets.MakeRelativeUri(new Uri(path)).ToString();
        }
    #endif
        
        public static void UpdateAbcTree(aiContext ctx, AlembicTreeNode node, float time, bool createMissingNodes=false)
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
            
            childTreeNode = new AlembicTreeNode() {linkedGameObj = childGO, stream = treeNode.stream };
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
        
        public static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null)
            {
                c = go.AddComponent<T>();
            }
            return c;
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

        public static T LoadAsset<T>(string name, string type="") where T : class
        {
            string search_string = name;

            if (type.Length > 0)
            {
                search_string += " t:" + type;
            }

            string[] guids = AssetDatabase.FindAssets(search_string);

            if (guids.Length >= 1)
            {
                if (guids.Length > 1)
                {
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        
                        if (path.Contains("AlembicImporter"))
                        {
                            return AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
                        }
                    }

                    Debug.LogWarning("Found several " + (type.Length > 0 ? type : "asset") + " named '" + name + "'. Use first found.");
                }
                
                return AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]), typeof(T)) as T;
            }
            else
            {
                Debug.LogWarning("Could not find " + (type.Length > 0 ? type : "asset") + " '" + name + "'");
                return null;
            }
        }

    #endif

        public static int CeilDiv(int v, int d)
        {
            return v / d + (v % d == 0 ? 0 : 1);
        }
    }
}
