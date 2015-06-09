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

#if UNITY_STANDALONE_WIN
    [DllImport ("AddLibraryPath")] public static extern void        AddLibraryPath();
#endif

    [DllImport ("AlembicImporter")] public static extern aiContext  aiCreateContext();
    [DllImport ("AlembicImporter")] public static extern void       aiDestroyContext(aiContext ctx);
    
    [DllImport ("AlembicImporter")] public static extern bool       aiLoad(aiContext ctx, string path);
    [DllImport ("AlembicImporter")] public static extern aiObject   aiGetTopObject(aiContext ctx);
    [DllImport ("AlembicImporter")] public static extern void       aiEnumerateChild(aiObject obj, aiNodeEnumerator e, IntPtr userdata);
    [DllImport ("AlembicImporter")] public static extern void       aiSetCurrentTime(aiObject obj, float time);
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
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshIsTopologyConstant(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshIsTopologyConstantTriangles(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshHasNormals(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshHasUVs(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetIndexCount(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetVertexCount(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopyIndices(aiObject obj, IntPtr dst);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopyVertices(aiObject obj, IntPtr dst);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopyNormals(aiObject obj, IntPtr dst);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopyUVs(aiObject obj, IntPtr dst);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshGetSplitedMeshInfo(aiObject obj, ref aiSplitedMeshInfo o_smi, ref aiSplitedMeshInfo prev, int max_vertices);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopySplitedIndices(aiObject obj, IntPtr indices, ref aiSplitedMeshInfo smi);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopySplitedVertices(aiObject obj, IntPtr vertices, ref aiSplitedMeshInfo smi);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopySplitedNormals(aiObject obj, IntPtr normals, ref aiSplitedMeshInfo smi);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopySplitedUVs(aiObject obj, IntPtr uvs, ref aiSplitedMeshInfo smi);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasCamera(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern void       aiCameraGetParams(aiObject obj, ref aiCameraParams o_params);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasLight(aiObject obj);


    class ImportContext
    {
        public Transform parent;
        public float time;
        public bool reverse_x;
        public bool reverse_faces;
    }

#if UNITY_EDITOR
    [MenuItem ("Assets/Import Alembic")]
    static void Import()
    {
        var path = MakeRelativePath(EditorUtility.OpenFilePanel("Select alembic (.abc) file in StreamingAssets directory", Application.streamingAssetsPath, "abc"));
        ImportImpl(path, true, false);
    }

    [MenuItem("Assets/Import Alembic (reverse faces)")]
    static void ImportR()
    {
        var path = MakeRelativePath(EditorUtility.OpenFilePanel("Select alembic (.abc) file in StreamingAssets directory", Application.streamingAssetsPath, "abc"));
        ImportImpl(path, true, true);
    }

    static string MakeRelativePath(string path)
    {
        Uri path_to_assets = new Uri(Application.streamingAssetsPath + "/");
        return path_to_assets.MakeRelativeUri(new Uri(path)).ToString();
    }

    static void ImportImpl(string path, bool reverse_x, bool reverse_faces)
    {
        if (path=="") return;

#if UNITY_STANDALONE_WIN
        AlembicImporter.AddLibraryPath();
#endif
        aiContext ctx = aiCreateContext();
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
            abcstream.m_reverse_x = reverse_x;
            abcstream.m_reverse_faces = reverse_faces;

            var ic = new ImportContext();
            ic.parent = root.GetComponent<Transform>();
            ic.reverse_x = reverse_x;
            ic.reverse_faces = reverse_faces;

            GCHandle gch = GCHandle.Alloc(ic);
            aiEnumerateChild(aiGetTopObject(ctx), ImportEnumerator, GCHandle.ToIntPtr(gch));
        }
        aiDestroyContext(ctx);
    }
#endif

    public static void UpdateAbcTree(aiContext ctx, Transform root, bool reverse_x, bool reverse_faces, float time)
    {
        var ic = new ImportContext();
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

        aiSetCurrentTime(obj, ic.time);
        aiEnableReverseX(obj, ic.reverse_x);
        aiEnableReverseIndex(obj, ic.reverse_faces);
        string child_name = aiGetName(obj);
        var trans = parent.FindChild(child_name);
        if (trans == null)
        {
            GameObject go = new GameObject();
            go.name = child_name;
            trans = go.GetComponent<Transform>();
            trans.parent = parent;
        }

        if (aiHasXForm(obj))
        {
            Vector3 pos = aiXFormGetPosition(obj);
            Quaternion rot = Quaternion.AngleAxis(aiXFormGetAngle(obj), aiXFormGetAxis(obj));
            Vector3 scale = aiXFormGetScale(obj);
            if (aiXFormGetInherits(obj))
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
        if (aiHasPolyMesh(obj))
        {
            UpdateAbcMesh(obj, trans);
        }
        if (aiHasCamera(obj))
        {
            trans.parent.forward = -trans.parent.forward;
            UpdateAbcCamera(obj, trans);
        }
        if (aiHasLight(obj))
        {
            UpdateAbcLight(obj, trans);
        }

        ic.parent = trans;
        aiEnumerateChild(obj, ImportEnumerator, userdata);
        ic.parent = parent;
    }


    public static void UpdateAbcMesh(aiObject abc, Transform trans)
    {
        const int max_vertices = 65000;

        AlembicMesh abcmesh = trans.GetComponent<AlembicMesh>();
        if(abcmesh==null)
        {
            abcmesh = trans.gameObject.AddComponent<AlembicMesh>();
            var entry = new AlembicMesh.Entry {
                host = trans.gameObject,
                mesh = AddMeshComponents(abc, trans),
                vertex_cache = new Vector3[0],
                uv_cache = new Vector2[0],
                index_cache = new int[0],
            };
            abcmesh.m_meshes.Add(entry);
#if UNITY_EDITOR
            trans.GetComponent<MeshRenderer>().sharedMaterial = GetDefaultMaterial();
#endif
        }
        Material material = trans.GetComponent<MeshRenderer>().sharedMaterial;

        /*
        {
            AlembicMesh.Entry entry = abcmesh.m_meshes[0];
            Array.Resize(ref entry.index_cache, aiPolyMeshGetIndexCount(ctx));
            Array.Resize(ref entry.vertex_cache, aiPolyMeshGetVertexCount(ctx));
            aiPolyMeshCopyIndices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(entry.index_cache, 0));
            aiPolyMeshCopyVertices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(entry.vertex_cache, 0));
            entry.mesh.Clear();
            entry.mesh.vertices = entry.vertex_cache;
            if(aiPolyMeshHasNormals(ctx))
            {
                aiPolyMeshCopyNormals(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(entry.vertex_cache, 0));
                entry.mesh.normals = entry.vertex_cache;
            }
            if (aiPolyMeshHasUVs(ctx))
            {
                Array.Resize(ref entry.uv_cache, aiPolyMeshGetVertexCount(ctx));
                aiPolyMeshCopyUVs(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(entry.uv_cache, 0));
                entry.mesh.uv = entry.uv_cache;
            }
            entry.mesh.SetIndices(entry.index_cache, MeshTopology.Triangles, 0);
            //entry.mesh.RecalculateNormals(); // 

            for (int i = 1; i < abcmesh.m_meshes.Count; ++i)
            {
                abcmesh.m_meshes[i].host.SetActive(false);
            }
        }
         */

        aiSplitedMeshInfo smi_prev = default(aiSplitedMeshInfo);
        aiSplitedMeshInfo smi = default(aiSplitedMeshInfo);

        int nth_submesh = 0;
        for (; ; )
        {
            smi_prev = smi;
            smi = default(aiSplitedMeshInfo);
            bool is_end = aiPolyMeshGetSplitedMeshInfo(abc, ref smi, ref smi_prev, max_vertices);

            AlembicMesh.Entry entry;
            if (nth_submesh < abcmesh.m_meshes.Count)
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
                Mesh mesh = AddMeshComponents(abc, child);
                mesh.name = name;
                child.GetComponent<MeshRenderer>().sharedMaterial = material;

                entry = new AlembicMesh.Entry
                {
                    host = go,
                    mesh = mesh,
                    vertex_cache = new Vector3[0],
                    uv_cache = new Vector2[0],
                    index_cache = new int[0],
                };
                abcmesh.m_meshes.Add(entry);
            }

            bool needs_index_update = entry.mesh.vertexCount == 0 || !aiPolyMeshIsTopologyConstant(abc);
            if (needs_index_update)
            {
                entry.mesh.Clear();
            }

            // update positions
            {
                Array.Resize(ref entry.vertex_cache, smi.vertex_count);
                aiPolyMeshCopySplitedVertices(abc, Marshal.UnsafeAddrOfPinnedArrayElement(entry.vertex_cache, 0), ref smi);
                entry.mesh.vertices = entry.vertex_cache;
            }

            // update normals
            if (aiPolyMeshHasNormals(abc))
            {
                // normals can reuse entry.vertex_cache
                aiPolyMeshCopySplitedNormals(abc, Marshal.UnsafeAddrOfPinnedArrayElement(entry.vertex_cache, 0), ref smi);
                entry.mesh.normals = entry.vertex_cache;
            }

            if (needs_index_update)
            {
                // update uvs
                if (aiPolyMeshHasUVs(abc))
                {
                    Array.Resize(ref entry.uv_cache, smi.vertex_count);
                    aiPolyMeshCopySplitedUVs(abc, Marshal.UnsafeAddrOfPinnedArrayElement(entry.uv_cache, 0), ref smi);
                    entry.mesh.uv = entry.uv_cache;
                }

                // update indices
                Array.Resize(ref entry.index_cache, smi.triangulated_index_count);
                aiPolyMeshCopySplitedIndices(abc, Marshal.UnsafeAddrOfPinnedArrayElement(entry.index_cache, 0), ref smi);
                entry.mesh.SetIndices(entry.index_cache, MeshTopology.Triangles, 0);
            }

            // recalculate normals
            if (!aiPolyMeshHasNormals(abc))
            {
                entry.mesh.RecalculateNormals();
            }

            ++nth_submesh;
            if (is_end) { break; }
        }

        for (int i = nth_submesh + 1; i < abcmesh.m_meshes.Count; ++i)
        {
            abcmesh.m_meshes[i].host.SetActive(false);
        }
    }


    static Mesh AddMeshComponents(aiObject abc, Transform trans)
    {
        Mesh mesh;
        var mesh_filter = trans.GetComponent<MeshFilter>();
        if (mesh_filter == null)
        {
            mesh = new Mesh();
            mesh.name = aiGetName(abc);
            mesh.MarkDynamic();
            mesh_filter = trans.gameObject.AddComponent<MeshFilter>();
            mesh_filter.sharedMesh = mesh;

            trans.gameObject.AddComponent<MeshRenderer>();
        }
        else
        {
            mesh = mesh_filter.sharedMesh;
        }
        return mesh;
    }


    static void UpdateAbcCamera(aiObject abc, Transform trans)
    {
        var cam = trans.GetComponent<Camera>();
        if(cam == null)
        {
            cam = trans.gameObject.AddComponent<Camera>();
        }
        aiCameraParams cp = default(aiCameraParams);
        aiCameraGetParams(abc, ref cp);
        cam.fieldOfView = cp.field_of_view;
        cam.nearClipPlane = cp.near_clipping_plane;
        cam.farClipPlane = cp.far_clipping_plane;

        /*
        var dof = trans.GetComponent<UnityStandardAssets.ImageEffects.DepthOfField>();
        if(dof != null)
        {
            dof.focalLength = cp.focus_distance;
            dof.focalSize = cp.focal_length;
        }
         */
    }

    static void UpdateAbcLight(aiObject abc, Transform trans)
    {
        var light = trans.GetComponent<Light>();
        if (light == null)
        {
            light = trans.gameObject.AddComponent<Light>();
        }
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
