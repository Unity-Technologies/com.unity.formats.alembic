using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AbcAPI
{
    public delegate void aiNodeEnumerator(aiObject obj, IntPtr userdata);
    public delegate void aiSampleCallback(IntPtr _this, aiSample sample);

    public struct aiImportConfig
    {
        public byte triangulate;
        public byte revert_x;
        public byte revert_faces;

        public void SetDefaults()
        {
            triangulate = 1;
            revert_x = 1;
            revert_faces = 0;
        }
    };

    public enum aiTopologyVariance
    {
        Constant,
        Homogeneous,
        Heterogeneous
    }

    public struct aiPolyMeshSchemaSummary
    {
        public aiTopologyVariance topology_variance;
        public uint peak_index_count;
        public uint peak_vertex_count;
        public byte has_normals;
        public byte has_uvs;
        public byte has_velocities;
        public byte is_normals_indexed;
        public byte is_uvs_indexed;
    };

    public struct aiPolyMeshSampleSummary
    {
        public uint index_count;
        public uint vertex_count;
        public byte has_normals;
        public byte has_uvs;
        public byte has_velocities;
        public byte is_normals_indexed;
        public byte is_uvs_indexed;
    };

    public struct aiSplitedMeshInfo
    {
        public uint face_count;
        public uint index_count;
        public uint vertex_count;
        public uint begin_face;
        public uint begin_index;
        public uint triangulated_index_count;

        public IntPtr dst_indices;
        public IntPtr dst_vertices;
        public IntPtr dst_normals;
        public IntPtr dst_uvs;
        public IntPtr dst_velocities;
    }

    public struct aiTextureMeshData
    {
        // in
        public uint tex_width;

        // out
        public uint index_count;
        public uint vertex_count;
        public uint is_normal_indexed;
        public uint is_uv_indexed;
        int pad;
        public IntPtr tex_indices;
        public IntPtr tex_vertices;
        public IntPtr tex_normals;
        public IntPtr tex_uvs;
        public IntPtr tex_velocities;
    }

    public struct aiXFormData
    {
        public Vector3 translation;
        public Quaternion rotation;
        public Vector3 scale;
        public byte inherits;
    };

    public struct aiCameraData
    {
        public float near_clipping_plane;
        public float far_clipping_plane;
        public float field_of_view;
        public float focus_distance;
        public float focal_length;
    }

    public struct aiContext { public System.IntPtr ptr; }
    public struct aiObject { public System.IntPtr ptr; }
    public struct aiSchema { public System.IntPtr ptr; }
    public struct aiSample { public System.IntPtr ptr; }

//#if UNITY_STANDALONE_WIN
//    [DllImport ("AddLibraryPath")] public static extern void        AddLibraryPath();
//#endif

    [DllImport ("AlembicImporter")] public static extern void       aiCleanup();
    [DllImport ("AlembicImporter")] public static extern aiContext  aiCreateContext();
    [DllImport ("AlembicImporter")] public static extern void       aiDestroyContext(aiContext ctx);
    
    [DllImport ("AlembicImporter")] public static extern bool       aiLoad(aiContext ctx, string path);
    [DllImport ("AlembicImporter")] public static extern void       aiSetImportConfig(aiContext ctx, ref aiImportConfig conf);
    [DllImport ("AlembicImporter")] public static extern void       aiDebugDump(aiContext ctx);
    [DllImport ("AlembicImporter")] public static extern float      aiGetStartTime(aiContext ctx);
    [DllImport ("AlembicImporter")] public static extern float      aiGetEndTime(aiContext ctx);
    [DllImport ("AlembicImporter")] public static extern aiObject   aiGetTopObject(aiContext ctx);
    [DllImport ("AlembicImporter")] public static extern void       aiUpdateSamples(aiContext ctx, float time);
    [DllImport ("AlembicImporter")] public static extern void       aiUpdateSamplesBegin(aiContext ctx, float time);
    [DllImport ("AlembicImporter")] public static extern void       aiUpdateSamplesEnd(aiContext ctx);
    [DllImport ("AlembicImporter")] public static extern void       aiSetTimeRangeToKeepSamples(aiContext ctx, float time, float keep_range);
    [DllImport ("AlembicImporter")] public static extern int        aiErasePastSamples(aiContext ctx, float time, float keep_range);

    [DllImport ("AlembicImporter")] public static extern void       aiEnumerateChild(aiObject obj, aiNodeEnumerator e, IntPtr userdata);
    [DllImport ("AlembicImporter")] private static extern IntPtr    aiGetNameS(aiObject obj);
    [DllImport ("AlembicImporter")] private static extern IntPtr    aiGetFullNameS(aiObject obj);
    public static string aiGetName(aiObject obj)      { return Marshal.PtrToStringAnsi(aiGetNameS(obj)); }
    public static string aiGetFullName(aiObject obj)  { return Marshal.PtrToStringAnsi(aiGetFullNameS(obj)); }

    [DllImport ("AlembicImporter")] public static extern void       aiSchemaSetCallback(aiSchema schema, aiSampleCallback cb, IntPtr arg);
    [DllImport ("AlembicImporter")] public static extern aiSample   aiSchemaUpdateSample(aiSchema schema, float time);
    [DllImport ("AlembicImporter")] public static extern aiSample   aiSchemaGetSample(aiSchema schema, float time);
    [DllImport ("AlembicImporter")] public static extern float      aiSampleGetTime(aiSample sample);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasXForm(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern aiSchema   aiGetXForm(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern bool       aiXFormGetData(aiSample sample, ref aiXFormData data);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasPolyMesh(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern aiSchema   aiGetPolyMesh(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshGetSchemaSummary(aiSchema schema, ref aiPolyMeshSchemaSummary summary);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshGetSampleSummary(aiSample sample, ref aiPolyMeshSampleSummary summary);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshGetSplitedMeshInfo(aiSample sample, ref aiSplitedMeshInfo o_smi, ref aiSplitedMeshInfo prev, int max_vertices);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshCopySplitedMesh(aiSample sample, ref aiSplitedMeshInfo o_smi);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopyToTexture(aiSample sample, ref aiTextureMeshData dst);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasCamera(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern aiSchema   aiGetCamera(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern void       aiCameraGetData(aiSample sample, ref aiCameraData o_params);


    class ImportContext
    {
        public AlembicStream abcstream;
        public Transform parent;
        public float time;
        public aiImportConfig iconf;

        public ImportContext()
        {
            iconf.SetDefaults();
        }
    }

#if UNITY_EDITOR

    public class ImportParams
    {
        public bool reverse_x = true;
        public bool reverse_faces = false;
        public AlembicStream.MeshDataType data_type = AlembicStream.MeshDataType.Mesh;
    }

    [MenuItem ("Assets/Alembic/Import")]
    static void Import()
    {
        var path = MakeRelativePath(EditorUtility.OpenFilePanel("Select alembic (.abc) file in StreamingAssets directory", Application.streamingAssetsPath, "abc"));
        ImportParams p = new ImportParams();
        ImportImpl(path, p);
    }

    [MenuItem("Assets/Alembic/Import (reverse faces)")]
    static void ImportR()
    {
        var path = MakeRelativePath(EditorUtility.OpenFilePanel("Select alembic (.abc) file in StreamingAssets directory", Application.streamingAssetsPath, "abc"));
        ImportParams p = new ImportParams();
        p.reverse_faces = true;
        ImportImpl(path, p);
    }

    //[MenuItem("Assets/Alembic/Import (texture mesh)")]
    //static void ImportT()
    //{
    //    var path = MakeRelativePath(EditorUtility.OpenFilePanel("Select alembic (.abc) file in StreamingAssets directory", Application.streamingAssetsPath, "abc"));
    //    ImportParams p = new ImportParams();
    //    p.data_type = AlembicStream.MeshDataType.Texture;
    //    ImportImpl(path, p);
    //}

    //[MenuItem("Assets/Alembic/Import (texture mesh, reverse faces)")]
    //static void ImportTR()
    //{
    //    var path = MakeRelativePath(EditorUtility.OpenFilePanel("Select alembic (.abc) file in StreamingAssets directory", Application.streamingAssetsPath, "abc"));
    //    ImportParams p = new ImportParams();
    //    p.data_type = AlembicStream.MeshDataType.Texture;
    //    p.reverse_faces = true;
    //    ImportImpl(path, p);
    //}

    static string MakeRelativePath(string path)
    {
        Uri path_to_assets = new Uri(Application.streamingAssetsPath + "/");
        return path_to_assets.MakeRelativeUri(new Uri(path)).ToString();
    }

    static void ImportImpl(string path, ImportParams p)
    {
        GameObject root = new GameObject();
        root.name = System.IO.Path.GetFileNameWithoutExtension(path);
        var abcstream = root.AddComponent<AlembicStream>();
        abcstream.m_path_to_abc = path;
        abcstream.m_revert_x = p.reverse_x;
        abcstream.m_revert_faces = p.reverse_faces;
        abcstream.m_data_type = p.data_type;
        abcstream.AbcLoad();
        abcstream.DebugDump();
    }
#endif

    public static void UpdateAbcTree(aiContext ctx, Transform root, bool revert_x, bool revert_faces, float time)
    {
        var ic = new ImportContext();
        ic.abcstream = root.GetComponent<AlembicStream>();
        ic.parent = root;
        ic.time = time;
        ic.iconf.revert_x = (byte)(revert_x ? 1 : 0);
        ic.iconf.revert_faces = (byte)(revert_faces ? 1 : 0);

        GCHandle gch = GCHandle.Alloc(ic);
        aiEnumerateChild(aiGetTopObject(ctx), ImportEnumerator, GCHandle.ToIntPtr(gch));
    }

    static void ImportEnumerator(aiObject obj, IntPtr userdata)
    {
        var ic = GCHandle.FromIntPtr(userdata).Target as ImportContext;
        Transform parent = ic.parent;

        string child_name = aiGetName(obj);
        var trans = parent.FindChild(child_name);
        bool created = false;
        if (trans == null)
        {
            created = true;
            GameObject go = new GameObject();
            go.name = child_name;
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
        if (aiHasPolyMesh(obj))
        {
            elem = GetOrAddComponent<AlembicMesh>(trans.gameObject);
            schema = aiGetPolyMesh(obj);
        }
        if (aiHasCamera(obj))
        {
            elem = GetOrAddComponent<AlembicCamera>(trans.gameObject);
            schema = aiGetCamera(obj);
        }

        if (elem)
        {
            elem.AbcSetup(ic.abcstream, obj, schema);
            aiSchemaUpdateSample(schema, 0.0f);
            elem.AbcUpdate();
            if (created)
            {
                ic.abcstream.AddElement(elem);
            }
        }

        ic.parent = trans;
        aiEnumerateChild(obj, ImportEnumerator, userdata);
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
}

class AlembicUtils
{
#if UNITY_EDITOR
    //[MenuItem ("Assets/Alembic/Create Index Only Mesh")]
    //public static void AddIndexOnlyMeshToAssetDatabase()
    //{
    //    AssetDatabase.CreateAsset(CreateIndexOnlyMesh(64998), "Assets/IndexOnlyMesh.asset");
    //}
#endif

    public static Mesh CreateIndexOnlyMesh(int num_indices)
    {
        Vector3[] vertices = new Vector3[num_indices];
        Vector2[] uv = new Vector2[num_indices];
        int[] indices = new int[num_indices];
        for (int i = 0; i < num_indices; ++i)
        {
            vertices[i].x = i;
            uv[i].x = i;
            indices[i] = i;
        }

        Mesh ret = new Mesh();
        ret.vertices = vertices;
        ret.triangles = indices;
        return ret;
    }

    public static int ceildiv(int v, int d)
    {
        return v/d + (v%d==0 ? 0 : 1);
    }
}
