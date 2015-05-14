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
        public int face_count;
        public int index_count;
        public int vertex_count;
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
        const int max_vertices = 65000;
        bool needs_split = aiGetVertexCount(ctx) >= max_vertices;

        AlembicMesh abcmesh = trans.GetComponent<AlembicMesh>();
        if(abcmesh==null)
        {
            abcmesh = trans.gameObject.AddComponent<AlembicMesh>();
            var entry = new AlembicMesh.Entry {
                host = trans.gameObject,
                mesh = AddMeshComponents(ctx, trans),
                vertex_cache = new Vector3[0],
                index_cache = new int[0],
            };
            abcmesh.m_meshes.Add(entry);
        }

        if (!needs_split)
        {
            AlembicMesh.Entry entry = abcmesh.m_meshes[0];
            Array.Resize(ref entry.index_cache, aiGetIndexCount(ctx));
            Array.Resize(ref entry.vertex_cache, aiGetVertexCount(ctx));
            aiCopyIndices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(entry.index_cache, 0));
            aiCopyVertices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(entry.vertex_cache, 0));
            entry.mesh.Clear();
            entry.mesh.vertices = entry.vertex_cache;
            entry.mesh.SetIndices(entry.index_cache, MeshTopology.Triangles, 0);
            entry.mesh.RecalculateNormals(); // 

            for (int i = 1; i < abcmesh.m_meshes.Count; ++i)
            {
                abcmesh.m_meshes[i].host.SetActive(false);
            }
        }
        else
        {
            aiSplitedMeshInfo smi_prev = default(aiSplitedMeshInfo);
            aiSplitedMeshInfo smi = default(aiSplitedMeshInfo);

            bool is_end = aiGetSplitedMeshInfo(ctx, ref smi, ref smi_prev, max_vertices);
            {
                AlembicMesh.Entry entry = abcmesh.m_meshes[0];
                Array.Resize(ref entry.index_cache, smi.triangulated_index_count);
                Array.Resize(ref entry.vertex_cache, smi.vertex_count);
                aiCopySplitedIndices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(entry.index_cache, 0), ref smi);
                aiCopySplitedVertices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(entry.vertex_cache, 0), ref smi);
                entry.mesh.Clear();
                entry.mesh.vertices = entry.vertex_cache;
                entry.mesh.SetIndices(entry.index_cache, MeshTopology.Triangles, 0);
                entry.mesh.RecalculateNormals();
            }

            int nth_submesh = 0;
            while (!is_end)
            {
                ++nth_submesh;
                smi_prev = smi;
                smi = default(aiSplitedMeshInfo);
                is_end = aiGetSplitedMeshInfo(ctx, ref smi, ref smi_prev, max_vertices);

                AlembicMesh.Entry entry;
                if(nth_submesh < abcmesh.m_meshes.Count)
                {
                    entry = abcmesh.m_meshes[nth_submesh];
                    entry.host.SetActive(true);
                }
                else
                {
                    string name = "Submesh_" + nth_submesh;

                    GameObject go = new GameObject();
                    Transform child = go.GetComponent<Transform>();
                    go.name = name;
                    child.parent = trans;
                    child.localPosition = Vector3.zero;
                    child.localEulerAngles = Vector3.zero;
                    child.localScale = Vector3.one;
                    Mesh mesh = AddMeshComponents(ctx, child);
                    mesh.name = name;

                    entry = new AlembicMesh.Entry
                    {
                        host = go,
                        mesh = mesh,
                        vertex_cache = new Vector3[0],
                        index_cache = new int[0],
                    };
                    abcmesh.m_meshes.Add(entry);
                }

                Array.Resize(ref entry.index_cache, smi.triangulated_index_count);
                Array.Resize(ref entry.vertex_cache, smi.vertex_count);
                aiCopySplitedIndices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(entry.index_cache, 0), ref smi);
                aiCopySplitedVertices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(entry.vertex_cache, 0), ref smi);
                entry.mesh.Clear();
                entry.mesh.vertices = entry.vertex_cache;
                entry.mesh.SetIndices(entry.index_cache, MeshTopology.Triangles, 0);
                entry.mesh.RecalculateNormals();
            }
            for (int i = nth_submesh + 1; i < abcmesh.m_meshes.Count; ++i)
            {
                abcmesh.m_meshes[i].host.SetActive(false);
            }
        }
    }

    static Mesh AddMeshComponents(IntPtr ctx, Transform trans)
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
#if UNITY_EDITOR
            mesh_renderer.material = GetDefaultMaterial();
#endif
        }
        else
        {
            mesh = mesh_filter.sharedMesh;
        }
        return mesh;
    }


#if UNITY_EDITOR
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
#endif
}
