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

    public struct aiSubmeshInfo
    {
        public int index;
        public int split_index;
        public int split_submesh_index;
        public int triangle_count;
        public int faceset_index;
    }

    public struct aiFacesets
    {
        public int count;
        public IntPtr face_counts;
        public IntPtr face_indices;
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

#if UNITY_STANDALONE_WIN
    [DllImport ("AddLibraryPath")] public static extern void        AddLibraryPath();
#endif

    [DllImport ("AlembicImporter")] public static extern aiContext  aiCreateContext();
    [DllImport ("AlembicImporter")] public static extern void       aiDestroyContext(aiContext ctx);
    
    [DllImport ("AlembicImporter")] public static extern bool       aiLoad(aiContext ctx, string path);
    [DllImport ("AlembicImporter")] public static extern float      aiGetStartTime(aiContext ctx);
    [DllImport ("AlembicImporter")] public static extern float      aiGetEndTime(aiContext ctx);
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
    [DllImport ("AlembicImporter")] public static extern aiTopologyVariance aiPolyMeshGetTopologyVariance(aiObject obj);    
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

    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetSplitCount(aiObject obj, bool force_refresh);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetVertexBufferLength(aiObject obj, int split);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshFillVertexBuffer(aiObject obj, int split, IntPtr positions, IntPtr normals, IntPtr uvs);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshPrepareSubmeshes(aiObject obj, ref aiFacesets facesets);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetSplitSubmeshCount(aiObject obj, int split);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshGetNextSubmesh(aiObject obj, ref aiSubmeshInfo o_smi);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshFillSubmeshIndices(aiObject obj, IntPtr indices, ref aiSubmeshInfo smi);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasCamera(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern void       aiCameraGetParams(aiObject obj, ref aiCameraParams o_params);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasLight(aiObject obj);


    class ImportContext
    {
        public Transform parent;
        public float time;
        public bool reverse_x;
        public bool reverse_faces;
        public bool ignore_missing_nodes;
    }

#if UNITY_EDITOR
    [MenuItem ("Assets/Import Alembic")]
    static void Import()
    {
        var path = MakeRelativePath(EditorUtility.OpenFilePanel("Select alembic (.abc) file in StreamingAssets directory", Application.streamingAssetsPath, "abc"));
        ImportImpl(path, true, false);
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
            abcstream.m_start_time = aiGetStartTime(ctx);
            abcstream.m_end_time = aiGetEndTime(ctx);
            abcstream.m_time_offset = -abcstream.m_start_time;
            abcstream.m_time_scale = 1.0f;
            abcstream.m_preserve_start_time = true;
            abcstream.m_reverse_x = reverse_x;
            abcstream.m_reverse_faces = reverse_faces;

            var ic = new ImportContext();
            ic.parent = root.GetComponent<Transform>();
            ic.reverse_x = reverse_x;
            ic.reverse_faces = reverse_faces;
            ic.ignore_missing_nodes = false;

            GCHandle gch = GCHandle.Alloc(ic);
            aiEnumerateChild(aiGetTopObject(ctx), ImportEnumerator, GCHandle.ToIntPtr(gch));
        }
        aiDestroyContext(ctx);
    }
#endif

    public static void UpdateAbcTree(aiContext ctx, Transform root, bool reverse_x, bool reverse_faces, float time, bool ignore_missing_nodes)
    {
        var ic = new ImportContext();
        ic.parent = root;
        ic.time = time;
        ic.reverse_x = reverse_x;
        ic.reverse_faces = reverse_faces;
        ic.ignore_missing_nodes = ignore_missing_nodes;

        GCHandle gch = GCHandle.Alloc(ic);
        aiEnumerateChild(aiGetTopObject(ctx), ImportEnumerator, GCHandle.ToIntPtr(gch));
    }

    static void ImportEnumerator(aiObject obj, IntPtr userdata)
    {
        var ic = GCHandle.FromIntPtr(userdata).Target as ImportContext;
        Transform parent = ic.parent;
        //Debug.Log("Node: " + aiGetFullName(ctx) + " (" + (xf ? "x" : "") + (mesh ? "p" : "") + ")");

        string child_name = aiGetName(obj);
        var trans = parent.FindChild(child_name);

        if (trans == null)
        {
            if (ic.ignore_missing_nodes)
            {
                return;
            }
            else
            {
                GameObject go = new GameObject();
                go.name = child_name;
                trans = go.GetComponent<Transform>();
                trans.parent = parent;
            }
        }

        aiSetCurrentTime(obj, ic.time);
        aiEnableReverseX(obj, ic.reverse_x);
        aiEnableReverseIndex(obj, ic.reverse_faces);
        
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
        AlembicMesh abcmesh = trans.GetComponent<AlembicMesh>();
        
        if (abcmesh == null)
        {
            abcmesh = trans.gameObject.AddComponent<AlembicMesh>();
        }

        aiTopologyVariance topo_variance = aiPolyMeshGetTopologyVariance(abc);
        bool has_normals = aiPolyMeshHasNormals(abc);
        bool has_uvs = aiPolyMeshHasUVs(abc);
        bool needs_index_update = (abcmesh.m_submeshes.Count == 0 || topo_variance == aiTopologyVariance.Heterogeneous);

        AlembicMaterial abcmaterials = trans.GetComponent<AlembicMaterial>();
        if (abcmaterials != null)
        {
            needs_index_update = needs_index_update || abcmaterials.HasFacesetsChanged();
            abcmesh.has_facesets = (abcmaterials.GetFacesetsCount() > 0);
        }
        else if (abcmesh.has_facesets)
        {
            needs_index_update = true;
            abcmesh.has_facesets = false;
        }

        if (!needs_index_update && topo_variance == aiTopologyVariance.Constant)
        {
            //Debug.Log("Nothing to update for \"" + trans.name + "\"");
            return;
        }

        AlembicMesh.Split split;

        int nsplits = aiPolyMeshGetSplitCount(abc, needs_index_update);
        
        // create necessary splits
        if (nsplits > 1)
        {
            for (int s=0; s<nsplits; ++s)
            {
                bool init_split = false;

                if (s < abcmesh.m_splits.Count)
                {
                    split = abcmesh.m_splits[s];

                    // Mesh with heterogeneous topology may start with a single split
                    //   but grow bigger later
                    if (s == 0 && split.host == trans.gameObject)
                    {
                        split.mesh.Clear();

                        Transform split0 = trans.FindChild("Split_0");

                        if (split0 != null)
                        {
                            split.host = split0.gameObject;
                            split.mesh = split0.gameObject.GetComponent<MeshFilter>().sharedMesh;
                        }
                        else
                        {
                            init_split = true;
                        }
                    }
                }
                else
                {
                    split = new AlembicMesh.Split
                    {
                        position_cache = new Vector3[0],
                        normal_cache = new Vector3[0],
                        uv_cache = new Vector2[0],
                        mesh = null,
                        host = null
                    };

                    abcmesh.m_splits.Add(split);

                    init_split = true;
                }

                if (init_split)
                {
                    GameObject go = new GameObject();
                    go.name = "Split_" + s;

                    Transform got = go.GetComponent<Transform>();
                    got.parent = trans;
                    got.localPosition = Vector3.zero;
                    got.localEulerAngles = Vector3.zero;
                    got.localScale = Vector3.one;

                    MeshRenderer renderer = trans.gameObject.GetComponent<MeshRenderer>();

                    Mesh mesh = AddMeshComponents(abc, got, (renderer != null ? renderer.sharedMaterial : null));
                    mesh.name = go.name;

                    split.mesh = mesh;
                    split.host = go;
                }

                split.host.SetActive(true);
            }
        }
        else
        {
            Mesh mesh = AddMeshComponents(abc, trans, null);

            if (abcmesh.m_splits.Count == 0)
            {
                split = new AlembicMesh.Split
                {
                    position_cache = new Vector3[0],
                    normal_cache = new Vector3[0],
                    uv_cache = new Vector2[0],
                    mesh = mesh,
                    host = trans.gameObject
                };

                abcmesh.m_splits.Add(split);
            }
            else
            {
                split = abcmesh.m_splits[0];

                if (split.host != trans.gameObject)
                {
                    // Shrinking back from multi objects to a single one: deactivate sub object
                    split.host.SetActive(false);
                }

                split.mesh = mesh;
                split.host = trans.gameObject;
            }

            split.host.SetActive(true);
        }

        // deactivate unused splits (sub objects)
        for (int s=nsplits; s<abcmesh.m_splits.Count; ++s)
        {
            abcmesh.m_splits[s].host.SetActive(false);
        }

        // read splits vertices
        for (int s=0; s<nsplits; ++s)
        {
            split = abcmesh.m_splits[s];

            int nvertices = aiPolyMeshGetVertexBufferLength(abc, s);

            Array.Resize(ref split.position_cache, nvertices);
            Array.Resize(ref split.normal_cache, (has_normals ? nvertices : 0));

            if (needs_index_update)
            {
                split.mesh.Clear();

                Array.Resize(ref split.uv_cache, (has_uvs ? nvertices : 0));
            }

            aiPolyMeshFillVertexBuffer(abc, s,
                                       Marshal.UnsafeAddrOfPinnedArrayElement(split.position_cache, 0),
                                       (has_normals ? Marshal.UnsafeAddrOfPinnedArrayElement(split.normal_cache, 0) : (IntPtr)0),
                                       ((needs_index_update && has_uvs) ? Marshal.UnsafeAddrOfPinnedArrayElement(split.uv_cache, 0) : (IntPtr)0));

            split.mesh.vertices = split.position_cache;
            split.mesh.normals = split.normal_cache;

            if (needs_index_update)
            {
                split.mesh.uv = split.uv_cache;
            }
        }

        if (needs_index_update)
        {
            aiSubmeshInfo smi = default(aiSubmeshInfo);
            aiFacesets facesets = default(aiFacesets);

            if (abcmaterials != null)
            {
                abcmaterials.GetFacesets(ref facesets);
            }

            aiPolyMeshPrepareSubmeshes(abc, ref facesets);

            // Setup materials and submeshes for each split
            for (int s=0; s<nsplits; ++s)
            {
                int ssm = aiPolyMeshGetSplitSubmeshCount(abc, s);

                split = abcmesh.m_splits[s];

                split.mesh.subMeshCount = ssm;

                MeshRenderer renderer = split.host.GetComponent<MeshRenderer>();
                    
                int nmat = renderer.sharedMaterials.Length;

                if (nmat != ssm)
                {
                    Material[] materials = new Material[ssm];
                    
                    for (int i=0; i<nmat; ++i)
                    {
                        materials[i] = renderer.sharedMaterials[i];
                    }

                    for (int i=nmat; i<ssm; ++i)
                    {
                        materials[i] = UnityEngine.Object.Instantiate(GetDefaultMaterial());
                        materials[i].name = "Material_" + Convert.ToString(i);
                    }

                    renderer.sharedMaterials = materials;
                }
            }

            // Setup submeshes
            while (aiPolyMeshGetNextSubmesh(abc, ref smi))
            {
                AlembicMesh.Submesh submesh;

                if (smi.split_index >= abcmesh.m_splits.Count)
                {
                    Debug.Log("Invalid split index");
                    continue;
                }

                split = abcmesh.m_splits[smi.split_index];

                if (smi.index < abcmesh.m_submeshes.Count)
                {
                    submesh = abcmesh.m_submeshes[smi.index];

                    submesh.faceset_index = smi.faceset_index;
                    submesh.split_index = smi.split_index;
                }
                else
                {
                    submesh = new AlembicMesh.Submesh
                    {
                        index_cache = new int[0],
                        faceset_index = smi.faceset_index,
                        split_index = smi.split_index
                    };

                    abcmesh.m_submeshes.Add(submesh);
                }

                // update indices
                Array.Resize(ref submesh.index_cache, smi.triangle_count * 3);
                aiPolyMeshFillSubmeshIndices(abc, Marshal.UnsafeAddrOfPinnedArrayElement(submesh.index_cache, 0), ref smi);
                
                split.mesh.SetIndices(submesh.index_cache, MeshTopology.Triangles, smi.split_submesh_index);
            }

            if (abcmaterials != null)
            {
                abcmaterials.AknowledgeFacesetsChanges();
            }
        }

        if (!has_normals)
        {
            for (int s=0; s<nsplits; ++s)
            {
                abcmesh.m_splits[s].mesh.RecalculateNormals();
            }
        }
    }

    static Mesh AddMeshComponents(aiObject abc, Transform trans, Material material=null)
    {
        Mesh mesh;
        
        MeshFilter mesh_filter = trans.gameObject.GetComponent<MeshFilter>();
        
        if (mesh_filter == null || mesh_filter.sharedMesh == null)
        {
            mesh = new Mesh();
            mesh.name = aiGetName(abc);
            mesh.MarkDynamic();

            if (mesh_filter == null)
            {
                mesh_filter = trans.gameObject.AddComponent<MeshFilter>();
            }

            mesh_filter.sharedMesh = mesh;

            MeshRenderer renderer = trans.gameObject.GetComponent<MeshRenderer>();
                
            if (renderer == null)
            {
                renderer = trans.gameObject.AddComponent<MeshRenderer>();
            }
            
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
            else
            {
                renderer.sharedMaterial = UnityEngine.Object.Instantiate(GetDefaultMaterial());
                renderer.sharedMaterial.name = "Material_0";
            }
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
