using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AlembicImporter
{
    public delegate void aiNodeEnumerator(aiObject obj, IntPtr userdata);

    public struct aiSplitedMeshInfo
    {
        public int face_count;
        public int index_count;
        public int vertex_count;
        public int begin_face;
        public int begin_index;
        public int triangulated_index_count;
    }

    public struct aiTextureMeshData
    {
        // in
        public int tex_width;

        // out
        public int index_count;
        public int vertex_count;
        public int is_normal_indexed;
        public int is_uv_indexed;
        int pad;
        public IntPtr tex_indices;
        public IntPtr tex_vertices;
        public IntPtr tex_normals;
        public IntPtr tex_uvs;
        public IntPtr tex_velocities;
    }


    public struct aiMeshData
    {
        public int index_count;
        public bool is_normal_indexed;
        public bool is_uv_indexed;
        public IntPtr tex_indices;
        public IntPtr tex_vertices;
        public IntPtr tex_normals;
        public IntPtr tex_uvs;
    }

    public struct aiCameraParams
    {
        public float near_clipping_plane;
        public float far_clipping_plane;
        public float field_of_view;
        public float focus_distance;
        public float focal_length;
    }

    public struct aiContext
    {
        public System.IntPtr ptr;
    }

    public struct aiObject
    {
        public System.IntPtr ptr;
    }

    public enum aiTopologyVariance
    {
        Constant,
        Homogeneous,
        Heterogeneous
    }

//#if UNITY_STANDALONE_WIN
//    [DllImport ("AddLibraryPath")] public static extern void        AddLibraryPath();
//#endif

    [DllImport ("AlembicImporter")] public static extern aiContext  aiCreateContext();
    [DllImport ("AlembicImporter")] public static extern void       aiDestroyContext(aiContext ctx);
    
    [DllImport ("AlembicImporter")] public static extern bool       aiLoad(aiContext ctx, string path);
    [DllImport ("AlembicImporter")] public static extern void       aiDebugDump(aiContext ctx);
    [DllImport ("AlembicImporter")] public static extern float      aiGetStartTime(aiContext ctx);
    [DllImport ("AlembicImporter")] public static extern float      aiGetEndTime(aiContext ctx);
    [DllImport ("AlembicImporter")] public static extern aiObject   aiGetTopObject(aiContext ctx);
    [DllImport ("AlembicImporter")] public static extern void       aiUpdateSamples(aiContext ctx, float time);
    [DllImport ("AlembicImporter")] public static extern void       aiUpdateSamplesBegin(aiContext ctx, float time);
    [DllImport ("AlembicImporter")] public static extern void       aiUpdateSamplesEnd(aiContext ctx);

    [DllImport ("AlembicImporter")] public static extern void       aiEnumerateChild(aiObject obj, aiNodeEnumerator e, IntPtr userdata);
    [DllImport ("AlembicImporter")] public static extern void       aiEnableReverseX(aiObject obj, bool v);
    [DllImport ("AlembicImporter")] public static extern void       aiEnableTriangulate(aiObject obj, bool v);
    [DllImport ("AlembicImporter")] public static extern void       aiEnableReverseIndex(aiObject obj, bool v);

    [DllImport ("AlembicImporter")] public static extern int        aiGetNumChildren(aiObject obj);
    [DllImport ("AlembicImporter")] private static extern IntPtr    aiGetNameS(aiObject obj);
    [DllImport ("AlembicImporter")] private static extern IntPtr    aiGetFullNameS(aiObject obj);
    public static string aiGetName(aiObject obj)      { return Marshal.PtrToStringAnsi(aiGetNameS(obj)); }
    public static string aiGetFullName(aiObject obj)  { return Marshal.PtrToStringAnsi(aiGetFullNameS(obj)); }

    [DllImport ("AlembicImporter")] public static extern bool       aiHasXForm(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern bool       aiXFormGetInherits(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern Vector3    aiXFormGetPosition(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern Vector3    aiXFormGetAxis(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern float      aiXFormGetAngle(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern Vector3    aiXFormGetScale(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern Matrix4x4  aiXFormGetMatrix(aiObject obj);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasPolyMesh(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern aiTopologyVariance aiPolyMeshGetTopologyVariance(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshIsTopologyConstantTriangles(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshHasNormals(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshHasUVs(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshHasVelocities(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshIsNormalIndexed(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshIsUVIndexed(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetIndexCount(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetVertexCount(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetPeakIndexCount(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetPeakVertexCount(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopyIndices(aiObject obj, IntPtr dst);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopyVertices(aiObject obj, IntPtr dst);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopyNormals(aiObject obj, IntPtr dst);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopyUVs(aiObject obj, IntPtr dst);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshGetSplitedMeshInfo(aiObject obj, ref aiSplitedMeshInfo o_smi, ref aiSplitedMeshInfo prev, int max_vertices);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopySplitedIndices(aiObject obj, IntPtr indices, ref aiSplitedMeshInfo smi);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopySplitedVertices(aiObject obj, IntPtr vertices, ref aiSplitedMeshInfo smi);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopySplitedNormals(aiObject obj, IntPtr normals, ref aiSplitedMeshInfo smi);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopySplitedUVs(aiObject obj, IntPtr uvs, ref aiSplitedMeshInfo smi);

    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshSetDstMeshTextures(aiObject obj, ref aiTextureMeshData dst);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopyToTexture(aiObject obj, ref aiTextureMeshData dst);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshBeginCopyToTexture(aiObject obj, ref aiTextureMeshData dst);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshEndCopyDataToTexture(aiObject obj, ref aiTextureMeshData dst);


    [DllImport ("AlembicImporter")] public static extern bool       aiHasCamera(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern void       aiCameraGetParams(aiObject obj, ref aiCameraParams o_params);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasLight(aiObject obj);


    class ImportContext
    {
        public AlembicStream abcstream;
        public Transform parent;
        public float time;
        public bool reverse_x;
        public bool reverse_faces;
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

    [MenuItem("Assets/Alembic/Import (texture mesh)")]
    static void ImportT()
    {
        var path = MakeRelativePath(EditorUtility.OpenFilePanel("Select alembic (.abc) file in StreamingAssets directory", Application.streamingAssetsPath, "abc"));
        ImportParams p = new ImportParams();
        p.data_type = AlembicStream.MeshDataType.Texture;
        ImportImpl(path, p);
    }

    [MenuItem("Assets/Alembic/Import (texture mesh, reverse faces)")]
    static void ImportTR()
    {
        var path = MakeRelativePath(EditorUtility.OpenFilePanel("Select alembic (.abc) file in StreamingAssets directory", Application.streamingAssetsPath, "abc"));
        ImportParams p = new ImportParams();
        p.data_type = AlembicStream.MeshDataType.Texture;
        p.reverse_faces = true;
        ImportImpl(path, p);
    }

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
        abcstream.m_reverse_x = p.reverse_x;
        abcstream.m_reverse_faces = p.reverse_faces;
        abcstream.m_data_type = p.data_type;
        abcstream.Awake();
        abcstream.DebugDump();
    }
#endif

    public static void UpdateAbcTree(aiContext ctx, Transform root, bool reverse_x, bool reverse_faces, float time)
    {
        var ic = new ImportContext();
        ic.abcstream = root.GetComponent<AlembicStream>();
        ic.parent = root;
        ic.time = time;
        ic.reverse_x = reverse_x;
        ic.reverse_faces = reverse_faces;

        GCHandle gch = GCHandle.Alloc(ic);
        aiEnumerateChild(aiGetTopObject(ctx), ImportEnumerator, GCHandle.ToIntPtr(gch));
    }

    static void ImportEnumerator(aiObject obj, IntPtr userdata)
    {
        var ic = GCHandle.FromIntPtr(userdata).Target as ImportContext;
        Transform parent = ic.parent;
        //Debug.Log("Node: " + aiGetFullName(ctx) + " (" + (xf ? "x" : "") + (mesh ? "p" : "") + ")");

        aiEnableReverseX(obj, ic.reverse_x);
        aiEnableReverseIndex(obj, ic.reverse_faces);

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
        if (aiHasXForm(obj))
        {
            elem = GetOrAddComponent<AlembicXForm>(trans.gameObject);
        }
        if (aiHasPolyMesh(obj))
        {
            elem = GetOrAddComponent<AlembicMesh>(trans.gameObject);
        }
        if (aiHasCamera(obj))
        {
            elem = GetOrAddComponent<AlembicCamera>(trans.gameObject);
        }
        if (aiHasLight(obj))
        {
            elem = GetOrAddComponent<AlembicLight>(trans.gameObject);
        }

        if (elem)
        {
            elem.AbcSetup(ic.abcstream, obj);
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
    [MenuItem ("Assets/Create Index Only Mesh")]
    public static void AddIndexOnlyMeshToAssetDatabase()
    {
        AssetDatabase.CreateAsset(CreateIndexOnlyMesh(64998), "Assets/IndexOnlyMesh.asset");
    }
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
