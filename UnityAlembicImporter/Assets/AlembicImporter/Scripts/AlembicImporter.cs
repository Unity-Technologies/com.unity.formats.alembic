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

    public struct aiCameraParams
    {
        public float near_clipping_plane;
        public float far_clipping_plane;
        public float field_of_view;
        public float focal_distance;
        public float focal_length;
    }


    [DllImport ("AlembicImporter")] public static extern IntPtr     aiCreateContext();
    [DllImport ("AlembicImporter")] public static extern void       aiDestroyContext(IntPtr ctx);
    
    [DllImport ("AlembicImporter")] public static extern bool       aiLoad(IntPtr ctx, string path);
    [DllImport ("AlembicImporter")] public static extern IntPtr     aiGetTopObject(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern void       aiEnumerateChild(IntPtr ctx, IntPtr obj, aiNodeEnumerator e, IntPtr userdata);
    [DllImport ("AlembicImporter")] public static extern void       aiSetCurrentObject(IntPtr ctx, IntPtr obj);
    [DllImport ("AlembicImporter")] public static extern void       aiSetCurrentTime(IntPtr ctx, float time);
    [DllImport ("AlembicImporter")] public static extern void       aiEnableTriangulate(IntPtr ctx, bool v);
    [DllImport ("AlembicImporter")] public static extern void       aiEnableReverseIndex(IntPtr ctx, bool v);

    [DllImport ("AlembicImporter")] public static extern int        aiGetNumChildren(IntPtr ctx);
    [DllImport ("AlembicImporter")] private static extern IntPtr    aiGetNameS(IntPtr ctx);
    [DllImport ("AlembicImporter")] private static extern IntPtr    aiGetFullNameS(IntPtr ctx);
    public static string aiGetName(IntPtr ctx)      { return Marshal.PtrToStringAnsi(aiGetNameS(ctx)); }
    public static string aiGetFullName(IntPtr ctx)  { return Marshal.PtrToStringAnsi(aiGetFullNameS(ctx)); }

    [DllImport ("AlembicImporter")] public static extern bool       aiHasXForm(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern bool       aiXFormGetInherits(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern Vector3    aiXFormGetPosition(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern Vector3    aiXFormGetAxis(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern float      aiXFormGetAngle(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern Vector3    aiXFormGetScale(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern Matrix4x4  aiXFormGetMatrix(IntPtr ctx);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasPolyMesh(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshIsTopologyConstant(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshIsTopologyConstantTriangles(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetIndexCount(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetVertexCount(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopyIndices(IntPtr ctx, IntPtr dst);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopyVertices(IntPtr ctx, IntPtr dst);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshGetSplitedMeshInfo(IntPtr ctx, ref aiSplitedMeshInfo o_smi, ref aiSplitedMeshInfo prev, int max_vertices);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopySplitedIndices(IntPtr ctx, IntPtr indices, ref aiSplitedMeshInfo smi);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopySplitedVertices(IntPtr ctx, IntPtr vertices, ref aiSplitedMeshInfo smi);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasCamera(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern void       aiCameraGetParams(IntPtr ctx, ref aiCameraParams o_params);


    class ImportContext
    {
        public Transform parent;
    }

#if UNITY_EDITOR
    [MenuItem ("Assets/Import Alembic")]
    static void Import()
    {
        var path = MakeRelativePath(EditorUtility.OpenFilePanel("Select alembic (.abc) file in StreamingAssets directory", Application.streamingAssetsPath, "abc"));
        ImportImpl(path, false);
    }

    [MenuItem("Assets/Import Alembic (reverse faces)")]
    static void ImportR()
    {
        var path = MakeRelativePath(EditorUtility.OpenFilePanel("Select alembic (.abc) file in StreamingAssets directory", Application.streamingAssetsPath, "abc"));
        ImportImpl(path, true);
    }

    static string MakeRelativePath(string path)
    {
        Uri path_to_assets = new Uri(Application.streamingAssetsPath + "/");
        return path_to_assets.MakeRelativeUri(new Uri(path)).ToString();
    }

    static void ImportImpl(string path, bool reverse_faces)
    {
        if (path=="") return;

        IntPtr ctx = aiCreateContext();
        if (!aiLoad(ctx, Application.streamingAssetsPath + "/" + path))
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

        if (aiHasXForm(ctx))
        {
            Vector3 pos = aiXFormGetPosition(ctx);
            Quaternion rot = Quaternion.AngleAxis(aiXFormGetAngle(ctx), aiXFormGetAxis(ctx));
            Vector3 scale = aiXFormGetScale(ctx);
            if (aiXFormGetInherits(ctx))
            {
                trans.localPosition = pos;
                trans.localRotation = rot;
                trans.localScale = scale;
            }
            else
            {
                trans.position = pos;
                trans.rotation = rot;
                trans.localScale = scale;
            }
        }
        else
        {
            trans.localPosition = Vector3.zero;
            trans.localEulerAngles = Vector3.zero;
            trans.localScale = Vector3.one;
        }
        if (aiHasPolyMesh(ctx))
        {
            UpdateAbcMesh(ctx, trans);
        }
        if(aiHasCamera(ctx))
        {
            trans.parent.forward = -trans.parent.forward;
            UpdateAbcCamera(ctx, trans);
        }

        ic.parent = trans;
        aiEnumerateChild(ctx, node, ImportEnumerator, userdata);
        ic.parent = parent;
    }

    public static void UpdateAbcMesh(IntPtr ctx, Transform trans)
    {
        const int max_vertices = 65000;
        bool needs_split = aiPolyMeshGetVertexCount(ctx) >= max_vertices;

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
#if UNITY_EDITOR
            trans.GetComponent<MeshRenderer>().sharedMaterial = GetDefaultMaterial();
#endif
        }
        Material material = trans.GetComponent<MeshRenderer>().sharedMaterial;

        if (!needs_split)
        {
            AlembicMesh.Entry entry = abcmesh.m_meshes[0];
            Array.Resize(ref entry.index_cache, aiPolyMeshGetIndexCount(ctx));
            Array.Resize(ref entry.vertex_cache, aiPolyMeshGetVertexCount(ctx));
            aiPolyMeshCopyIndices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(entry.index_cache, 0));
            aiPolyMeshCopyVertices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(entry.vertex_cache, 0));
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

            bool is_end = aiPolyMeshGetSplitedMeshInfo(ctx, ref smi, ref smi_prev, max_vertices);
            {
                AlembicMesh.Entry entry = abcmesh.m_meshes[0];
                Array.Resize(ref entry.index_cache, smi.triangulated_index_count);
                Array.Resize(ref entry.vertex_cache, smi.vertex_count);
                aiPolyMeshCopySplitedIndices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(entry.index_cache, 0), ref smi);
                aiPolyMeshCopySplitedVertices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(entry.vertex_cache, 0), ref smi);
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
                is_end = aiPolyMeshGetSplitedMeshInfo(ctx, ref smi, ref smi_prev, max_vertices);

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
                    child.GetComponent<MeshRenderer>().sharedMaterial = material;

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
                aiPolyMeshCopySplitedIndices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(entry.index_cache, 0), ref smi);
                aiPolyMeshCopySplitedVertices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(entry.vertex_cache, 0), ref smi);
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
        }
        else
        {
            mesh = mesh_filter.sharedMesh;
        }
        return mesh;
    }

    static void UpdateAbcCamera(IntPtr ctx, Transform trans)
    {
        var cam = trans.GetComponent<Camera>();
        if(cam == null)
        {
            cam = trans.gameObject.AddComponent<Camera>();
        }
        aiCameraParams cp = default(aiCameraParams);
        aiCameraGetParams(ctx, ref cp);
        cam.fieldOfView = cp.field_of_view;
        cam.nearClipPlane = cp.near_clipping_plane;
        cam.farClipPlane = cp.far_clipping_plane;
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
