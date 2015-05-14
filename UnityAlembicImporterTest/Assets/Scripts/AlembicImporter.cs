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
    public delegate void aiNodeEnumerator(IntPtr ctx, IntPtr obj, IntPtr userdata);

    public struct aiSplitedMeshInfo
    {
        public int num_faces;
        public int num_indices;
        public int num_vertices;
        public int begin_face;
        public int begin_index;
        public int triangulated_index_count;
    };


    [DllImport ("AlembicImporter")] public static extern IntPtr     aiCreateContext();
    [DllImport ("AlembicImporter")] public static extern void       aiDestroyContext(IntPtr ctx);
    
    [DllImport ("AlembicImporter")] public static extern bool       aiLoad(IntPtr ctx, string path);
    [DllImport ("AlembicImporter")] public static extern IntPtr     aiGetTopObject(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern void       aiEnumerateChild(IntPtr ctx, IntPtr obj, aiNodeEnumerator e, IntPtr userdata);
    [DllImport ("AlembicImporter")] public static extern void       aiSetCurrentObject(IntPtr ctx, IntPtr obj);
    [DllImport ("AlembicImporter")] public static extern void       aiSetCurrentTime(IntPtr ctx, float time);
    [DllImport ("AlembicImporter")] public static extern void       aiEnableTriangulate(IntPtr ctx, bool v);
    [DllImport ("AlembicImporter")] public static extern void       aiEnableReverseIndex(IntPtr ctx, bool v);

    [DllImport ("AlembicImporter")] private static extern IntPtr    aiGetNameS(IntPtr ctx);
    [DllImport ("AlembicImporter")] private static extern IntPtr    aiGetFullNameS(IntPtr ctx);
    public static string aiGetName(IntPtr ctx)      { return Marshal.PtrToStringAnsi(aiGetNameS(ctx)); }
    public static string aiGetFullName(IntPtr ctx)  { return Marshal.PtrToStringAnsi(aiGetFullNameS(ctx)); }

    [DllImport ("AlembicImporter")] public static extern int        aiGetNumChildren(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern bool       aiHasXForm(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern Vector3    aiGetPosition(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern Vector3    aiGetRotation(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern Vector3    aiGetScale(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern Matrix4x4  aiGetMatrix(IntPtr ctx);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasPolyMesh(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern bool       aiIsNormalsIndexed(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern bool       aiIsUVsIndexed(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern int        aiGetIndexCount(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern int        aiGetVertexCount(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern void       aiCopyIndices(IntPtr ctx, IntPtr dst);
    [DllImport ("AlembicImporter")] public static extern void       aiCopyVertices(IntPtr ctx, IntPtr dst);
    [DllImport ("AlembicImporter")] public static extern bool       aiGetSplitedMeshInfo(IntPtr ctx, ref aiSplitedMeshInfo o_smi, ref aiSplitedMeshInfo prev, int max_vertices);
    [DllImport ("AlembicImporter")] public static extern void       aiCopySplitedIndices(IntPtr ctx, IntPtr indices, ref aiSplitedMeshInfo smi);
    [DllImport ("AlembicImporter")] public static extern void       aiCopySplitedVertices(IntPtr ctx, IntPtr vertices, ref aiSplitedMeshInfo smi);


    class ImportContext
    {
        public Transform parent;
    }

#if UNITY_EDITOR
    [MenuItem ("Assets/Import Alembic")]
    static void Import()
    {
        var path = EditorUtility.OpenFilePanel("", "", "abc");
        ImportImpl(path, false);
    }

    [MenuItem("Assets/Import Alembic (reverse faces)")]
    static void ImportR()
    {
        var path = EditorUtility.OpenFilePanel("", "", "abc");
        ImportImpl(path, true);
    }

    static void ImportImpl(string path, bool reverse_faces)
    {
        if (path=="") return;

        IntPtr ctx = aiCreateContext();
        if (!aiLoad(ctx, path))
        {
            Debug.Log("aiLoad(\"" + path + "\") failed");
        }
        else
        {
            GameObject root = new GameObject();
            root.name = System.IO.Path.GetFileNameWithoutExtension(path);
            var abcstream = root.AddComponent<AlembicStream>();
            abcstream.m_path_to_abc = path;
            abcstream.m_reverse_faces = reverse_faces;

            var ic = new ImportContext();
            ic.parent = root.GetComponent<Transform>();

            GCHandle gch = GCHandle.Alloc(ic);
            aiEnableReverseIndex(ctx, reverse_faces);
            aiEnumerateChild(ctx, aiGetTopObject(ctx), ImportEnumerator, GCHandle.ToIntPtr(gch));
        }
        aiDestroyContext(ctx);
    }
#endif

    public static void UpdateAbcTree(IntPtr ctx, Transform root, bool reverse_faces, float time)
    {
        aiSetCurrentTime(ctx, time);

        var ic = new ImportContext();
        ic.parent = root;

        GCHandle gch = GCHandle.Alloc(ic);
        aiEnumerateChild(ctx, aiGetTopObject(ctx), ImportEnumerator, GCHandle.ToIntPtr(gch));
    }

    static void ImportEnumerator(IntPtr ctx, IntPtr node, IntPtr userdata)
    {
        var ic = GCHandle.FromIntPtr(userdata).Target as ImportContext;
        Transform parent = ic.parent;
        bool has_xform = aiHasXForm(ctx);
        bool has_mesh = aiHasPolyMesh(ctx);
        //Debug.Log("Node: " + aiGetFullName(ctx) + " (" + (xf ? "x" : "") + (mesh ? "p" : "") + ")");

        string child_name = aiGetName(ctx);
        var trans = parent.FindChild(child_name);
        if (trans == null)
        {
            GameObject go = new GameObject();
            go.name = aiGetName(ctx);
            trans = go.GetComponent<Transform>();
            trans.parent = parent;
        }

        if (has_xform)
        {
            trans.localPosition = aiGetPosition(ctx);
            trans.localEulerAngles = aiGetRotation(ctx);
            trans.localScale = aiGetScale(ctx);
        }
        else
        {
            trans.localPosition = Vector3.zero;
            trans.localEulerAngles = Vector3.zero;
            trans.localScale = Vector3.one;
        }
        if (has_mesh)
        {
            UpdateAbcMesh(ctx, trans);
        }

        ic.parent = trans;
        aiEnumerateChild(ctx, node, ImportEnumerator, userdata);
        ic.parent = parent;
    }

    public static void UpdateAbcMesh(IntPtr ctx, Transform trans)
    {
        int max_vertices = 65000;

        bool needs_split = aiGetVertexCount(ctx) >= max_vertices;

        Mesh mainmesh = CreateOrGetMesh(ctx, trans);
        if (!needs_split)
        {
            int[] indices = new int[aiGetIndexCount(ctx)];
            Vector3[] vertices = new Vector3[aiGetVertexCount(ctx)];
            aiCopyIndices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(indices, 0));
            aiCopyVertices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(vertices, 0));
            mainmesh.Clear();
            mainmesh.vertices = vertices;
            mainmesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mainmesh.RecalculateNormals();

            for (int i = 0; DisableSubmesh(ctx, trans, i); ++i) { }
        }
        else
        {
            aiSplitedMeshInfo smi_prev = default(aiSplitedMeshInfo);
            aiSplitedMeshInfo smi = default(aiSplitedMeshInfo);

            int nth_submesh = 0;
            bool is_end = aiGetSplitedMeshInfo(ctx, ref smi, ref smi_prev, max_vertices);
            {
                int[] indices = new int[smi.triangulated_index_count];
                Vector3[] vertices = new Vector3[smi.num_vertices];
                aiCopySplitedIndices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(indices, 0), ref smi);
                aiCopySplitedVertices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(vertices, 0), ref smi);
                mainmesh.vertices = vertices;
                mainmesh.SetIndices(indices, MeshTopology.Triangles, 0);
                mainmesh.RecalculateNormals();
            }

            while (!is_end)
            {
                smi_prev = smi;
                smi = default(aiSplitedMeshInfo);
                is_end = aiGetSplitedMeshInfo(ctx, ref smi, ref smi_prev, max_vertices);

                Transform child;
                Mesh submesh;
                CreateOrGetSubmeshObject(ctx, trans, nth_submesh, out child, out submesh);

                int[] indices = new int[smi.triangulated_index_count];
                Vector3[] vertices = new Vector3[smi.num_vertices];
                aiCopySplitedIndices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(indices, 0), ref smi);
                aiCopySplitedVertices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(vertices, 0), ref smi);
                submesh.Clear();
                submesh.vertices = vertices;
                submesh.SetIndices(indices, MeshTopology.Triangles, 0);
                submesh.RecalculateNormals();

                ++nth_submesh;
            }
            while (DisableSubmesh(ctx, trans, nth_submesh)) { ++nth_submesh; }
        }
    }

    static void CreateOrGetSubmeshObject(IntPtr ctx, Transform parent, int nth, out Transform o_trans, out Mesh o_mesh)
    {
        string name = "Submesh_" + nth;
        Transform child = parent.FindChild(name);
        Mesh mesh;
        if (child == null)
        {
            var go = new GameObject();
            go.name = name;
            child = go.GetComponent<Transform>();
            child.parent = parent;
            child.localPosition = Vector3.zero;
            child.localEulerAngles = Vector3.zero;
            child.localScale = Vector3.one;

            mesh = CreateOrGetMesh(ctx, child);
            mesh.name = name;
        }
        else
        {
            mesh = child.GetComponent<MeshFilter>().sharedMesh;
        }
        o_trans = child;
        o_mesh = mesh;
    }

    static bool DisableSubmesh(IntPtr ctx, Transform parent, int nth)
    {
        string name = "Submesh_" + nth;
        Transform child = parent.FindChild(name);
        if (child != null) {
            GameObject.DestroyImmediate(child.gameObject);
            return true;
        }
        return false;
    }

    static Mesh CreateOrGetMesh(IntPtr ctx, Transform trans)
    {
        Mesh mesh;
        var mesh_filter = trans.GetComponent<MeshFilter>();
        if (mesh_filter == null)
        {
            mesh = new Mesh();
            mesh.name = aiGetName(ctx);
            mesh.MarkDynamic();
            mesh_filter = trans.gameObject.AddComponent<MeshFilter>();
            mesh_filter.sharedMesh = mesh;

            var mesh_renderer = trans.gameObject.AddComponent<MeshRenderer>();
            mesh_renderer.material = GetDefaultMaterial();
        }
        else
        {
            mesh = mesh_filter.sharedMesh;
        }
        return mesh;
    }


    static MethodInfo s_GetBuiltinExtraResourcesMethod;

    static Material GetDefaultMaterial()
    {
        if (s_GetBuiltinExtraResourcesMethod == null)
        {
            BindingFlags bfs = BindingFlags.NonPublic | BindingFlags.Static;
            s_GetBuiltinExtraResourcesMethod = typeof(EditorGUIUtility).GetMethod("GetBuiltinExtraResource", bfs);
        }
        return (Material)s_GetBuiltinExtraResourcesMethod.Invoke(null, new object[] { typeof(Material), "Default-Material.mat" });
    }
}
