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

public class AbcAPI
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
    public delegate void aiSampleCallback(IntPtr _this, aiSample sample, bool topologyChanged);

    public struct aiConfig
    {
        [MarshalAs(UnmanagedType.U1)] public bool swapHandedness;
        [MarshalAs(UnmanagedType.U1)] public bool swapFaceWinding;
        [MarshalAs(UnmanagedType.U4)] public aiNormalsMode normalsMode;
        [MarshalAs(UnmanagedType.U4)] public aiTangentsMode tangentsMode;
        [MarshalAs(UnmanagedType.U1)] public bool cacheTangentsSplits;
        public float aspectRatio;
        [MarshalAs(UnmanagedType.U1)] public bool forceUpdate;
        [MarshalAs(UnmanagedType.U1)] public bool useThreads;
        [MarshalAs(UnmanagedType.U4)] public int cacheSamples;
        [MarshalAs(UnmanagedType.U1)] public bool submeshPerUVTile;

        public void SetDefaults()
        {
            swapHandedness = true;
            swapFaceWinding = false;
            normalsMode = aiNormalsMode.ComputeIfMissing;
            tangentsMode = aiTangentsMode.None;
            cacheTangentsSplits = true;
            aspectRatio = -1.0f;
            forceUpdate = false;
            useThreads = true;
            cacheSamples = 0;
            submeshPerUVTile = true;
        }
    }

    public struct aiFacesets
    {
        [MarshalAs(UnmanagedType.U4)]public int count;

        public IntPtr faceCounts;
        public IntPtr faceIndices;
    }

    public struct aiMeshSummary
    {
        [MarshalAs(UnmanagedType.U4)] public aiTopologyVariance topologyVariance;
        public int peakVertexCount;
        public int peakIndexCount;
        public int peakTriangulatedIndexCount;
        public int peakSubmeshCount;
    }

    public struct aiMeshSampleSummary
    {
        [MarshalAs(UnmanagedType.U4)] public int splitCount;
        [MarshalAs(UnmanagedType.U1)] public bool hasNormals;
        [MarshalAs(UnmanagedType.U1)] public bool hasUVs;
        [MarshalAs(UnmanagedType.U1)] public bool hasTangents;
    }

    public struct aiMeshSampleData
    {
        // indices and their counts are available only when get by aiPolyMeshGetData()
        public IntPtr positions;
        public IntPtr velocities;
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

        public Vector3 center;
        public Vector3 size;
    }

    public struct aiSubmeshSummary
    {
        [MarshalAs(UnmanagedType.U4)] public int index;
        [MarshalAs(UnmanagedType.U4)] public int splitIndex;
        [MarshalAs(UnmanagedType.U4)] public int splitSubmeshIndex;
        [MarshalAs(UnmanagedType.U4)] public int facesetIndex;
        [MarshalAs(UnmanagedType.U4)] public int triangleCount;
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
        [MarshalAs(UnmanagedType.U1)] public bool inherits;
    }

    public struct aiCameraData
    {
        public float nearClippingPlane;
        public float farClippingPlane;
        public float fieldOfView;
        public float focusDistance;
        public float focalLength;
    }

    public struct aiContext
    {
        public System.IntPtr ptr;
    }

    public struct aiObject
    {
        public System.IntPtr ptr;
    }

    public struct aiSchema
    {
        public System.IntPtr ptr;
    }

    public struct aiProperty
    {
        public System.IntPtr ptr;
    }

    public struct aiSample
    {
        public System.IntPtr ptr;
    }

    public struct aiPointsSampleData
    {
        public IntPtr positions;
        public IntPtr velocities;
        public IntPtr ids;
        public Vector3 boundsCenter;
        public Vector3 boundsExtents;
        public int count;
    }

    public struct aiPropertyData
    {
        public IntPtr data;
        public int size;
        aiPropertyType type;
    }



    [DllImport ("AlembicImporter")] public static extern void       aiEnableFileLog(bool on, string path);

    [DllImport ("AlembicImporter")] public static extern void       aiCleanup();
    [DllImport ("AlembicImporter")] public static extern aiContext  aiCreateContext(int uid);
    [DllImport ("AlembicImporter")] public static extern void       aiDestroyContext(aiContext ctx);
    
    [DllImport ("AlembicImporter")] public static extern bool       aiLoad(aiContext ctx, string path);
    [DllImport ("AlembicImporter")] public static extern void       aiSetConfig(aiContext ctx, ref aiConfig conf);
    [DllImport ("AlembicImporter")] public static extern float      aiGetStartTime(aiContext ctx);
    [DllImport ("AlembicImporter")] public static extern float      aiGetEndTime(aiContext ctx);
    [DllImport ("AlembicImporter")] public static extern aiObject   aiGetTopObject(aiContext ctx);
    [DllImport ("AlembicImporter")] public static extern void       aiDestroyObject(aiContext ctx, aiObject obj);

    [DllImport ("AlembicImporter")] public static extern void       aiUpdateSamples(aiContext ctx, float time);
    [DllImport ("AlembicImporter")] public static extern void       aiUpdateSamplesBegin(aiContext ctx, float time);
    [DllImport ("AlembicImporter")] public static extern void       aiUpdateSamplesEnd(aiContext ctx);

    [DllImport ("AlembicImporter")] public static extern void       aiEnumerateChild(aiObject obj, aiNodeEnumerator e, IntPtr userData);
    [DllImport ("AlembicImporter")] private static extern IntPtr    aiGetNameS(aiObject obj);
    [DllImport ("AlembicImporter")] private static extern IntPtr    aiGetFullNameS(aiObject obj);
    public static string aiGetName(aiObject obj)      { return Marshal.PtrToStringAnsi(aiGetNameS(obj)); }
    public static string aiGetFullName(aiObject obj)  { return Marshal.PtrToStringAnsi(aiGetFullNameS(obj)); }
    
    [DllImport ("AlembicImporter")] public static extern void       aiSchemaSetSampleCallback(aiSchema schema, aiSampleCallback cb, IntPtr arg);
    [DllImport ("AlembicImporter")] public static extern void       aiSchemaSetConfigCallback(aiSchema schema, aiConfigCallback cb, IntPtr arg);
    [DllImport ("AlembicImporter")] public static extern aiSample   aiSchemaUpdateSample(aiSchema schema, float time);
    [DllImport ("AlembicImporter")] public static extern aiSample   aiSchemaGetSample(aiSchema schema, float time);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasXForm(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern aiSchema   aiGetXForm(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern bool       aiXFormGetData(aiSample sample, ref aiXFormData data);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasPolyMesh(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern aiSchema   aiGetPolyMesh(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshGetSummary(aiSchema schema, ref aiMeshSummary summary);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshGetSampleSummary(aiSample sample, ref aiMeshSampleSummary summary, bool forceRefresh);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetVertexBufferLength(aiSample sample, int splitIndex);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshFillVertexBuffer(aiSample sample, int splitIndex, ref aiMeshSampleData data);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshPrepareSubmeshes(aiSample sample, ref aiFacesets facesets);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetSplitSubmeshCount(aiSample sample, int splitIndex);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshGetNextSubmesh(aiSample sample, ref aiSubmeshSummary smi);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshFillSubmeshIndices(aiSample sample, ref aiSubmeshSummary smi, ref aiSubmeshData data);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasCamera(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern aiSchema   aiGetCamera(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern void       aiCameraGetData(aiSample sample, ref aiCameraData data);

    [DllImport("AlembicImporter")] public static extern bool        aiHasPoints(aiObject obj);
    [DllImport("AlembicImporter")] public static extern aiSchema    aiGetPoints(aiObject obj);
    [DllImport("AlembicImporter")] public static extern int         aiPointsGetPeakVertexCount(aiSchema schema);
    [DllImport("AlembicImporter")] public static extern void        aiPointsCopyData(aiSample sample, ref aiPointsSampleData data);

    [DllImport("AlembicImporter")] public static extern int             aiSchemaGetNumProperties(aiSchema schema);
    [DllImport("AlembicImporter")] public static extern aiProperty      aiSchemaGetPropertyByIndex(aiSchema schema, int i);
    [DllImport("AlembicImporter")] public static extern aiProperty      aiSchemaGetPropertyByName(aiSchema schema, string name);
    [DllImport("AlembicImporter")] public static extern IntPtr          aiPropertyGetNameS(aiProperty prop);
    [DllImport("AlembicImporter")] public static extern aiPropertyType  aiPropertyGetType(aiProperty prop);
    [DllImport("AlembicImporter")] public static extern void            aiPropertyGetData(aiProperty prop, aiPropertyData o_data);


    // currently tex must be ARGBFloat
    [DllImport("AlembicImporter")] public static extern bool        aiPointsCopyPositionsToTexture(ref aiPointsSampleData data, IntPtr tex, int width, int height, aiTextureFormat fmt);
    // currently tex must be RInt
    [DllImport("AlembicImporter")] public static extern bool        aiPointsCopyIDsToTexture(ref aiPointsSampleData data, IntPtr tex, int width, int height, aiTextureFormat fmt);


    class ImportContext
    {
        public AlembicStream abcStream;
        public Transform parent;
        public float time;
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

    [MenuItem ("Assets/Import Alembic")]
    static void Import()
    {
        var path = MakeRelativePath(EditorUtility.OpenFilePanel("Select alembic (.abc) file in StreamingAssets directory", Application.streamingAssetsPath, "abc"));
        ImportParams p = new ImportParams();
        ImportImpl(path, p);
    }

    public static GameObject ImportAbc(string path)
    {
        var relPath = MakeRelativePath(path);
        ImportParams p = new ImportParams();
        return ImportImpl(relPath, p);
    }

    static GameObject ImportImpl(string path, ImportParams p)
    {
        if (path == null || path == "")
        {
            return null;
        }

        string baseName = System.IO.Path.GetFileNameWithoutExtension(path);
        string name = baseName;
        int index = 1;
        
        while (GameObject.Find("/" + name) != null)
        {
            name = baseName + index;
            ++index;
        }

        GameObject root = new GameObject();
        root.name = name;

        var abcStream = root.AddComponent<AlembicStream>();
        abcStream.m_pathToAbc = path;
        abcStream.m_swapHandedness = p.swapHandedness;
        abcStream.m_swapFaceWinding = p.swapFaceWinding;
        abcStream.AbcLoad(true);

        return root;
    }

#endif
    
    public static void UpdateAbcTree(aiContext ctx, Transform root, float time, bool createMissingNodes=false)
    {
        var ic = new ImportContext();
        ic.abcStream = root.GetComponent<AlembicStream>();
        ic.parent = root;
        ic.time = time;
        ic.createMissingNodes = createMissingNodes;
        ic.objectsToDelete = new List<aiObject>();

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
        Transform parent = ic.parent;

        string childName = aiGetName(obj);
        var trans = parent.FindChild(childName);

        if (trans == null)
        {
            if (!ic.createMissingNodes)
            {
                ic.objectsToDelete.Add(obj);
                return;
            }

            GameObject go = new GameObject();
            go.name = childName;
            trans = go.GetComponent<Transform>();
            trans.parent = parent;
            trans.localPosition = Vector3.zero;
            trans.localEulerAngles = Vector3.zero;
            trans.localScale = Vector3.one;
        }

        AlembicElement elem = null;
        aiSchema schema = default(aiSchema);

        if (aiHasXForm(obj))
        {
            elem = GetOrAddComponent<AlembicXForm>(trans.gameObject);
            schema = aiGetXForm(obj);
        }
        else if (aiHasPolyMesh(obj))
        {
            elem = GetOrAddComponent<AlembicMesh>(trans.gameObject);
            schema = aiGetPolyMesh(obj);
        }
        else if (aiHasCamera(obj))
        {
            elem = GetOrAddComponent<AlembicCamera>(trans.gameObject);
            schema = aiGetCamera(obj);
        }
        else if (aiHasPoints(obj))
        {
            elem = GetOrAddComponent<AlembicPoints>(trans.gameObject);
            schema = aiGetPoints(obj);
        }

        if (elem)
        {
            elem.AbcSetup(ic.abcStream, obj, schema);
            aiSchemaUpdateSample(schema, ic.time);
            elem.AbcUpdate();
            
            ic.abcStream.AbcAddElement(elem);
        }

        ic.parent = trans;
        aiEnumerateChild(obj, ImportEnumerator, userData);
        ic.parent = parent;
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


    public static aiTextureFormat GetTextureFormat(RenderTexture v)
    {
        switch (v.format)
        {
            case RenderTextureFormat.ARGB32:    return aiTextureFormat.ARGB32;
            case RenderTextureFormat.RHalf:     return aiTextureFormat.RHalf;
            case RenderTextureFormat.RGHalf:    return aiTextureFormat.RGHalf;
            case RenderTextureFormat.ARGBHalf:  return aiTextureFormat.ARGBHalf;
            case RenderTextureFormat.RFloat:    return aiTextureFormat.RFloat;
            case RenderTextureFormat.RGFloat:   return aiTextureFormat.RGFloat;
            case RenderTextureFormat.ARGBFloat: return aiTextureFormat.ARGBFloat;
            case RenderTextureFormat.RInt:      return aiTextureFormat.RInt;
            case RenderTextureFormat.RGInt:     return aiTextureFormat.RGInt;
            case RenderTextureFormat.ARGBInt:   return aiTextureFormat.ARGBInt;
        }
        return aiTextureFormat.Unknown;
    }

    public static aiTextureFormat GetTextureFormat(Texture2D v)
    {
        switch (v.format)
        {
            case TextureFormat.ARGB32:      return aiTextureFormat.ARGB32;
            case TextureFormat.RHalf:       return aiTextureFormat.RHalf;
            case TextureFormat.RGHalf:      return aiTextureFormat.RGHalf;
            case TextureFormat.RGBAHalf:    return aiTextureFormat.ARGBHalf;
            case TextureFormat.RFloat:      return aiTextureFormat.RFloat;
            case TextureFormat.RGFloat:     return aiTextureFormat.RGFloat;
            case TextureFormat.RGBAFloat:   return aiTextureFormat.ARGBFloat;
        }
        return aiTextureFormat.Unknown;
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
