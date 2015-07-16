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
        public int faceCount;
        public int indexCount;
        public int vertexCount;
        public int beginFace;
        public int beginIndex;
        public int triangulatedIndexCount;
    }

    public struct aiSubmeshInfo
    {
        public int index;
        public int splitIndex;
        public int splitSubmeshIndex;
        public int triangleCount;
        public int facesetIndex;
    }

    public struct aiFacesets
    {
        public int count;
        public IntPtr faceCounts;
        public IntPtr faceIndices;
    }

    public struct aiMeshData
    {
        public int indexCount;
        public bool isNormalIndexed;
        public bool isUvIndexed;
        public IntPtr texIndices;
        public IntPtr texVertices;
        public IntPtr texNormals;
        public IntPtr texUvs;
    }

    public struct aiCameraParams
    {
        public float nearClippingPlane;
        public float faceClippingPlane;
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

    public enum aiTopologyVariance
    {
        Constant,
        Homogeneous,
        Heterogeneous
    }

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
    [DllImport ("AlembicImporter")] public static extern void       aiForceSmoothNormals(aiObject obj, bool v);
    [DllImport ("AlembicImporter")] public static extern bool       aiGetReverseX(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern bool       aiGetReverseIndex(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern bool       aiGetForceSmoothNormals(aiObject obj);

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
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshGetSplitedMeshInfo(aiObject obj, ref aiSplitedMeshInfo smi, ref aiSplitedMeshInfo prev, int maxVertices);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopySplitedIndices(aiObject obj, IntPtr indices, ref aiSplitedMeshInfo smi);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopySplitedVertices(aiObject obj, IntPtr vertices, ref aiSplitedMeshInfo smi);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopySplitedNormals(aiObject obj, IntPtr normals, ref aiSplitedMeshInfo smi);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshCopySplitedUVs(aiObject obj, IntPtr uvs, ref aiSplitedMeshInfo smi);

    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetSplitCount(aiObject obj, bool force_refresh);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetVertexBufferLength(aiObject obj, int split);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshFillVertexBuffer(aiObject obj, int split, IntPtr positions, IntPtr normals, IntPtr uvs);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshPrepareSubmeshes(aiObject obj, ref aiFacesets facesets);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetSplitSubmeshCount(aiObject obj, int split);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshGetNextSubmesh(aiObject obj, ref aiSubmeshInfo smi);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshFillSubmeshIndices(aiObject obj, IntPtr indices, ref aiSubmeshInfo smi);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasCamera(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern void       aiCameraGetParams(aiObject obj, ref aiCameraParams o_params);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasLight(aiObject obj);


    class ImportContext
    {
        public Transform parent;
        public float time;
        public bool reverseX;
        public bool reverseFaces;
        public bool forceSmoothNormals;
        public bool ignoreMissingNodes;
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
        Uri pathToAssets = new Uri(Application.streamingAssetsPath + "/");
        return pathToAssets.MakeRelativeUri(new Uri(path)).ToString();
    }

    static void ImportImpl(string path, bool reverseX, bool reverseFaces)
    {
        if (path == "")
        {
            return;
        }

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
            abcstream.m_pathToAbc = path;
            abcstream.m_startTime = aiGetStartTime(ctx);
            abcstream.m_endTime = aiGetEndTime(ctx);
            abcstream.m_timeOffset = -abcstream.m_startTime;
            abcstream.m_timeScale = 1.0f;
            abcstream.m_preserveStartTime = true;
            abcstream.m_reverseX = reverseX;
            abcstream.m_reverseFaces = reverseFaces;

            var ic = new ImportContext();
            ic.parent = root.GetComponent<Transform>();
            ic.reverseX = reverseX;
            ic.reverseFaces = reverseFaces;
            ic.forceSmoothNormals = false;
            ic.ignoreMissingNodes = false;

            GCHandle gch = GCHandle.Alloc(ic);
            aiEnumerateChild(aiGetTopObject(ctx), ImportEnumerator, GCHandle.ToIntPtr(gch));
        }
        aiDestroyContext(ctx);
    }
#endif

    public static void UpdateAbcTree(aiContext ctx, Transform root, float time, bool reverseX, bool reverseFaces, bool forceSmoothNormals, bool ignoreMissingNodes)
    {
        var ic = new ImportContext();
        ic.parent = root;
        ic.time = time;
        ic.reverseX = reverseX;
        ic.reverseFaces = reverseFaces;
        ic.forceSmoothNormals = forceSmoothNormals;
        ic.ignoreMissingNodes = ignoreMissingNodes;

        GCHandle gch = GCHandle.Alloc(ic);
        aiEnumerateChild(aiGetTopObject(ctx), ImportEnumerator, GCHandle.ToIntPtr(gch));
    }

    static void ImportEnumerator(aiObject obj, IntPtr userdata)
    {
        var ic = GCHandle.FromIntPtr(userdata).Target as ImportContext;
        Transform parent = ic.parent;
        //Debug.Log("Node: " + aiGetFullName(ctx) + " (" + (xf ? "x" : "") + (mesh ? "p" : "") + ")");

        string childName = aiGetName(obj);
        var trans = parent.FindChild(childName);

        if (trans == null)
        {
            if (ic.ignoreMissingNodes)
            {
                return;
            }
            else
            {
                GameObject go = new GameObject();
                go.name = childName;
                trans = go.GetComponent<Transform>();
                trans.parent = parent;
            }
        }


        bool reverseFaces = ic.reverseFaces;
        bool forceSmoothNormals = ic.forceSmoothNormals;

        AlembicMesh abcMesh = trans.GetComponent<AlembicMesh>();
        if (abcMesh != null)
        {
            // AlembicMesh level flags have higher priority
            reverseFaces = reverseFaces || abcMesh.m_reverseFaces;
            forceSmoothNormals = forceSmoothNormals || abcMesh.m_forceSmoothNormals;
        }

        // Check if polygon winding has change in order to force topology update
        bool windingChanged = (aiGetReverseIndex(obj) != reverseFaces);

        aiEnableReverseX(obj, ic.reverseX);
        aiEnableReverseIndex(obj, reverseFaces);
        aiForceSmoothNormals(obj, forceSmoothNormals);

        aiSetCurrentTime(obj, ic.time);
        
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
            UpdateAbcMesh(obj, trans, windingChanged);
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
    
    public static void UpdateAbcMesh(aiObject abc, Transform trans, bool forceIndexUpdate=false)
    {
        AlembicMesh abcMesh = trans.GetComponent<AlembicMesh>();
        
        if (abcMesh == null)
        {
            abcMesh = trans.gameObject.AddComponent<AlembicMesh>();
        }

        aiTopologyVariance topoVariance = aiPolyMeshGetTopologyVariance(abc);
        bool hasNormals = aiPolyMeshHasNormals(abc);
        bool hasUVs = aiPolyMeshHasUVs(abc);
        bool needsIndexUpdate = (abcMesh.m_submeshes.Count == 0 || topoVariance == aiTopologyVariance.Heterogeneous || forceIndexUpdate);

        AlembicMaterial abcMaterials = trans.GetComponent<AlembicMaterial>();
        if (abcMaterials != null)
        {
            needsIndexUpdate = needsIndexUpdate || abcMaterials.HasFacesetsChanged();
            abcMesh.hasFacesets = (abcMaterials.GetFacesetsCount() > 0);
        }
        else if (abcMesh.hasFacesets)
        {
            needsIndexUpdate = true;
            abcMesh.hasFacesets = false;
        }

        if (!needsIndexUpdate && topoVariance == aiTopologyVariance.Constant)
        {
            //Debug.Log("Nothing to update for \"" + trans.name + "\"");
            return;
        }

        AlembicMesh.Split split;

        int numSplits = aiPolyMeshGetSplitCount(abc, needsIndexUpdate);
        
        // create necessary splits
        if (numSplits > 1)
        {
            for (int s=0; s<numSplits; ++s)
            {
                bool initSplit = false;

                if (s < abcMesh.m_splits.Count)
                {
                    split = abcMesh.m_splits[s];

                    // Mesh with heterogeneous topology may start with a single split
                    //   but grow bigger later
                    if (s == 0 && split.host == trans.gameObject)
                    {
                        split.mesh.Clear();

                        Transform split0 = trans.FindChild(trans.gameObject.name + "_split_0");

                        if (split0 != null)
                        {
                            split.host = split0.gameObject;
                            split.mesh = split0.gameObject.GetComponent<MeshFilter>().sharedMesh;
                        }
                        else
                        {
                            initSplit = true;
                        }
                    }
                }
                else
                {
                    split = new AlembicMesh.Split
                    {
                        positionCache = new Vector3[0],
                        normalCache = new Vector3[0],
                        uvCache = new Vector2[0],
                        mesh = null,
                        host = null
                    };

                    abcMesh.m_splits.Add(split);

                    initSplit = true;
                }

                if (initSplit)
                {
                    GameObject go = new GameObject();
                    go.name = trans.gameObject.name + "_split_" + s;

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

            if (abcMesh.m_splits.Count == 0)
            {
                split = new AlembicMesh.Split
                {
                    positionCache = new Vector3[0],
                    normalCache = new Vector3[0],
                    uvCache = new Vector2[0],
                    mesh = mesh,
                    host = trans.gameObject
                };

                abcMesh.m_splits.Add(split);
            }
            else
            {
                split = abcMesh.m_splits[0];

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
        for (int s=numSplits; s<abcMesh.m_splits.Count; ++s)
        {
            abcMesh.m_splits[s].host.SetActive(false);
        }

        // read splits vertices
        for (int s=0; s<numSplits; ++s)
        {
            split = abcMesh.m_splits[s];

            int numVertices = aiPolyMeshGetVertexBufferLength(abc, s);

            Array.Resize(ref split.positionCache, numVertices);
            Array.Resize(ref split.normalCache, (hasNormals ? numVertices : 0));

            if (needsIndexUpdate)
            {
                split.mesh.Clear();

                Array.Resize(ref split.uvCache, (hasUVs ? numVertices : 0));
            }

            aiPolyMeshFillVertexBuffer(abc, s,
                                       Marshal.UnsafeAddrOfPinnedArrayElement(split.positionCache, 0),
                                       (hasNormals ? Marshal.UnsafeAddrOfPinnedArrayElement(split.normalCache, 0) : (IntPtr)0),
                                       ((needsIndexUpdate && hasUVs) ? Marshal.UnsafeAddrOfPinnedArrayElement(split.uvCache, 0) : (IntPtr)0));

            split.mesh.vertices = split.positionCache;
            split.mesh.normals = split.normalCache;

            if (needsIndexUpdate)
            {
                split.mesh.uv = split.uvCache;
            }
        }

        if (needsIndexUpdate)
        {
            aiSubmeshInfo smi = default(aiSubmeshInfo);
            aiFacesets facesets = default(aiFacesets);

            if (abcMaterials != null)
            {
                abcMaterials.GetFacesets(ref facesets);
            }

            aiPolyMeshPrepareSubmeshes(abc, ref facesets);

            // Setup materials and submeshes for each split
            for (int s=0; s<numSplits; ++s)
            {
                int ssm = aiPolyMeshGetSplitSubmeshCount(abc, s);

                split = abcMesh.m_splits[s];

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

                if (smi.splitIndex >= abcMesh.m_splits.Count)
                {
                    Debug.Log("Invalid split index");
                    continue;
                }

                split = abcMesh.m_splits[smi.splitIndex];

                if (smi.index < abcMesh.m_submeshes.Count)
                {
                    submesh = abcMesh.m_submeshes[smi.index];

                    submesh.facesetIndex = smi.facesetIndex;
                    submesh.splitIndex = smi.splitIndex;
                }
                else
                {
                    submesh = new AlembicMesh.Submesh
                    {
                        indexCache = new int[0],
                        facesetIndex = smi.facesetIndex,
                        splitIndex = smi.splitIndex
                    };

                    abcMesh.m_submeshes.Add(submesh);
                }

                // update indices
                Array.Resize(ref submesh.indexCache, smi.triangleCount * 3);
                aiPolyMeshFillSubmeshIndices(abc, Marshal.UnsafeAddrOfPinnedArrayElement(submesh.indexCache, 0), ref smi);
                
                split.mesh.SetIndices(submesh.indexCache, MeshTopology.Triangles, smi.splitSubmeshIndex);
            }

            if (abcMaterials != null)
            {
                abcMaterials.AknowledgeFacesetsChanges();
            }
        }

        if (!hasNormals)
        {
            for (int s=0; s<numSplits; ++s)
            {
                abcMesh.m_splits[s].mesh.RecalculateNormals();
            }
        }
    }

    static Mesh AddMeshComponents(aiObject abc, Transform trans, Material material=null)
    {
        Mesh mesh;
        
        MeshFilter meshFilter = trans.gameObject.GetComponent<MeshFilter>();
        
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            mesh = new Mesh();
            mesh.name = aiGetName(abc);
            mesh.MarkDynamic();

            if (meshFilter == null)
            {
                meshFilter = trans.gameObject.AddComponent<MeshFilter>();
            }

            meshFilter.sharedMesh = mesh;

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
            mesh = meshFilter.sharedMesh;
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
        cam.fieldOfView = cp.fieldOfView;
        cam.nearClipPlane = cp.nearClippingPlane;
        cam.farClipPlane = cp.faceClippingPlane;

        /*
        var dof = trans.GetComponent<UnityStandardAssets.ImageEffects.DepthOfField>();
        if(dof != null)
        {
            dof.focalLength = cp.focusDistance;
            dof.focalSize = cp.focalLength;
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
