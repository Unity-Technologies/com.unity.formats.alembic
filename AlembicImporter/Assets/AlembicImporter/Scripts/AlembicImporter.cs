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
        public float targetAspect;
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

    public enum aiAspectRatioMode
    {
        CurrentResolution = 0,
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

    [DllImport ("AlembicImporter")] public static extern void       aiEnableFileLog(bool on, string path);

    [DllImport ("AlembicImporter")] public static extern aiContext  aiCreateContext(int uid);
    [DllImport ("AlembicImporter")] public static extern void       aiDestroyContext(aiContext ctx);
    
    [DllImport ("AlembicImporter")] public static extern bool       aiLoad(aiContext ctx, string path);
    [DllImport ("AlembicImporter")] public static extern float      aiGetStartTime(aiContext ctx);
    [DllImport ("AlembicImporter")] public static extern float      aiGetEndTime(aiContext ctx);
    [DllImport ("AlembicImporter")] public static extern aiObject   aiGetTopObject(aiContext ctx);
    [DllImport ("AlembicImporter")] public static extern void       aiEnumerateChild(aiObject obj, aiNodeEnumerator e, IntPtr userdata);
    [DllImport ("AlembicImporter")] public static extern void       aiSetCurrentTime(aiObject obj, float time);
    [DllImport ("AlembicImporter")] public static extern void       aiEnableTriangulate(aiObject obj, bool v);
    
    [DllImport ("AlembicImporter")] public static extern void       aiSwapHandedness(aiObject obj, bool v);
    [DllImport ("AlembicImporter")] public static extern bool       aiIsHandednessSwapped(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern void       aiSwapFaceWinding(aiObject obj, bool v);
    [DllImport ("AlembicImporter")] public static extern bool       aiIsFaceWindingSwapped(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern void       aiSetNormalsMode(aiObject obj, int m);
    [DllImport ("AlembicImporter")] public static extern int        aiGetNormalsMode(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern void       aiSetTangentsMode(aiObject obj, int m);
    [DllImport ("AlembicImporter")] public static extern int        aiGetTangentsMode(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern void       aiCacheTangentsSplits(aiObject obj, bool v);
    [DllImport ("AlembicImporter")] public static extern bool       aiAreTangentsSplitsCached(aiObject obj);

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
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetSplitCount(aiObject obj, bool forceRefresh);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetVertexBufferLength(aiObject obj, int split);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshFillVertexBuffer(aiObject obj, int split, IntPtr positions, IntPtr normals, IntPtr uvs, IntPtr tangents);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshPrepareSubmeshes(aiObject obj, ref aiFacesets facesets);
    [DllImport ("AlembicImporter")] public static extern int        aiPolyMeshGetSplitSubmeshCount(aiObject obj, int split);
    [DllImport ("AlembicImporter")] public static extern bool       aiPolyMeshGetNextSubmesh(aiObject obj, ref aiSubmeshInfo smi);
    [DllImport ("AlembicImporter")] public static extern void       aiPolyMeshFillSubmeshIndices(aiObject obj, IntPtr indices, ref aiSubmeshInfo smi);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasCamera(aiObject obj);
    [DllImport ("AlembicImporter")] public static extern void       aiCameraGetParams(aiObject obj, ref aiCameraParams o_params);


    class ImportContext
    {
        public Transform parent;
        public float time;
        public bool swapHandedness;
        public bool swapFaceWinding;
        public aiNormalsMode normalsMode;
        public aiTangentsMode tangentsMode;
        public float aspectRatio;
        public bool ignoreMissingNodes;
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

    static void ImportImpl(string path, bool swapHandedness, bool swapFaceWinding)
    {
        if (path == "")
        {
            return;
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

        aiContext ctx = aiCreateContext(root.GetInstanceID());

        if (!aiLoad(ctx, Application.streamingAssetsPath + "/" + path))
        {
            Debug.Log("aiLoad(\"" + path + "\") failed");
            GameObject.DestroyImmediate(root);
            aiDestroyContext(ctx);
        }
        else
        {
            var abcstream = root.AddComponent<AlembicStream>();
            abcstream.m_pathToAbc = path;
            abcstream.m_startTime = aiGetStartTime(ctx);
            abcstream.m_endTime = aiGetEndTime(ctx);
            abcstream.m_timeOffset = -abcstream.m_startTime;
            abcstream.m_timeScale = 1.0f;
            abcstream.m_preserveStartTime = true;
            abcstream.m_swapHandedness = swapHandedness;
            abcstream.m_swapFaceWinding = swapFaceWinding;
            abcstream.m_normalsMode = aiNormalsMode.ComputeIfMissing;
            abcstream.m_tangentsMode = aiTangentsMode.None;
            abcstream.m_aspectRatioMode = aiAspectRatioMode.CurrentResolution;

            abcstream.Init();

            var ic = new ImportContext();
            ic.parent = root.GetComponent<Transform>();
            ic.swapHandedness = swapHandedness;
            ic.swapFaceWinding = swapFaceWinding;
            ic.normalsMode = aiNormalsMode.ComputeIfMissing;
            ic.tangentsMode = aiTangentsMode.None;
            ic.aspectRatio = GetAspectRatio(abcstream.m_aspectRatioMode);
            ic.ignoreMissingNodes = false;

            GCHandle gch = GCHandle.Alloc(ic);

            aiObject top = aiGetTopObject(ctx);

            if (top.ptr != (IntPtr)0)
            {
                aiEnumerateChild(top, ImportEnumerator, GCHandle.ToIntPtr(gch));
            }
        }
    }
#endif
    
    public static void UpdateAbcTree(aiContext ctx, Transform root, float time,
                                     bool swapHandedness, bool swapFaceWinding,
                                     aiNormalsMode normalsMode, aiTangentsMode tangentsMode,
                                     float aspectRatio, bool ignoreMissingNodes)
    {
        var ic = new ImportContext();
        ic.parent = root;
        ic.time = time;
        ic.swapHandedness = swapHandedness;
        ic.swapFaceWinding = swapFaceWinding;
        ic.normalsMode = normalsMode;
        ic.tangentsMode = tangentsMode;
        ic.aspectRatio = aspectRatio;
        ic.ignoreMissingNodes = ignoreMissingNodes;

        GCHandle gch = GCHandle.Alloc(ic);

        aiObject top = aiGetTopObject(ctx);

        if (top.ptr != (IntPtr)0)
        {
            aiEnumerateChild(top, ImportEnumerator, GCHandle.ToIntPtr(gch));
        }
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

        bool swapFaceWinding = ic.swapFaceWinding;
        int normalsMode = (int) ic.normalsMode;
        int tangentsMode = (int) ic.tangentsMode;
        bool cacheTangentsSplits = true;

        AlembicMesh abcMesh = trans.GetComponent<AlembicMesh>();
        if (abcMesh != null)
        {
            if (abcMesh.m_faceWinding != aiFaceWindingOverride.InheritStreamSetting)
            {
                swapFaceWinding = (abcMesh.m_faceWinding == aiFaceWindingOverride.Swap);
            }
            if (abcMesh.m_normalsMode != aiNormalsModeOverride.InheritStreamSetting)
            {
                normalsMode = (int) abcMesh.m_normalsMode;
            }
            if (abcMesh.m_tangentsMode != aiTangentsModeOverride.InheritStreamSetting)
            {
                tangentsMode = (int) abcMesh.m_tangentsMode;
            }
            cacheTangentsSplits = abcMesh.m_cacheTangentsSplits;
        }

        // Check if polygon winding has change in order to force topology update
        bool forceTopology = (aiIsFaceWindingSwapped(obj) != swapFaceWinding);
        // Check if we need force a vertex buffer update
        bool forceVertices = (aiGetNormalsMode(obj) != normalsMode ||
                              aiGetTangentsMode(obj) != tangentsMode ||
                              aiIsHandednessSwapped(obj) != ic.swapHandedness);

        aiSwapHandedness(obj, ic.swapHandedness);
        aiSwapFaceWinding(obj, swapFaceWinding);
        aiSetNormalsMode(obj, normalsMode);
        aiSetTangentsMode(obj, tangentsMode);
        aiCacheTangentsSplits(obj, cacheTangentsSplits);

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
            UpdateAbcMesh(obj, trans, forceTopology, forceVertices);
        }

        if (aiHasCamera(obj))
        {
            trans.parent.forward = -trans.parent.forward;
            UpdateAbcCamera(obj, trans, ic.aspectRatio);
        }

        ic.parent = trans;
        aiEnumerateChild(obj, ImportEnumerator, userdata);
        ic.parent = parent;
    }
    
    public static void UpdateAbcMesh(aiObject abc, Transform trans, bool forceTopologyUpdate=false, bool forceVerticesUpdate=false)
    {
        AlembicMesh abcMesh = trans.GetComponent<AlembicMesh>();
        
        if (abcMesh == null)
        {
            abcMesh = trans.gameObject.AddComponent<AlembicMesh>();
        }

        aiTopologyVariance topoVariance = aiPolyMeshGetTopologyVariance(abc);
        bool hasNormals = aiPolyMeshHasNormals(abc);
        bool hasUVs = aiPolyMeshHasUVs(abc);
        bool hasTangents = (hasUVs && aiGetTangentsMode(abc) != (int) aiTangentsMode.None);
        bool updateTopology = (abcMesh.m_submeshes.Count == 0 ||
                               topoVariance == aiTopologyVariance.Heterogeneous ||
                               forceTopologyUpdate);

        AlembicMaterial abcMaterials = trans.GetComponent<AlembicMaterial>();
        if (abcMaterials != null)
        {
            updateTopology = updateTopology || abcMaterials.HasFacesetsChanged();
            abcMesh.hasFacesets = (abcMaterials.GetFacesetsCount() > 0);
        }
        else if (abcMesh.hasFacesets)
        {
            updateTopology = true;
            abcMesh.hasFacesets = false;
        }

        if (!updateTopology && topoVariance == aiTopologyVariance.Constant && !forceVerticesUpdate)
        {
            //Debug.Log("Nothing to update for \"" + trans.name + "\"");
            return;
        }

        AlembicMesh.Split split;

        int numSplits = aiPolyMeshGetSplitCount(abc, updateTopology);
        
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
                        tangentCache = new Vector4[0],
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
                    tangentCache = new Vector4[0],
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
            Array.Resize(ref split.tangentCache, (hasTangents ? numVertices : 0));
            
            if (updateTopology)
            {
                split.mesh.Clear();

                Array.Resize(ref split.uvCache, (hasUVs ? numVertices : 0));
            }

            aiPolyMeshFillVertexBuffer(abc, s,
                                       Marshal.UnsafeAddrOfPinnedArrayElement(split.positionCache, 0),
                                       (hasNormals ? Marshal.UnsafeAddrOfPinnedArrayElement(split.normalCache, 0) : (IntPtr)0),
                                       ((updateTopology && hasUVs) ? Marshal.UnsafeAddrOfPinnedArrayElement(split.uvCache, 0) : (IntPtr)0),
                                       (hasTangents ? Marshal.UnsafeAddrOfPinnedArrayElement(split.tangentCache, 0) : (IntPtr)0));

            split.mesh.vertices = split.positionCache;
            split.mesh.normals = split.normalCache;
            split.mesh.tangents = split.tangentCache;

            if (updateTopology)
            {
                split.mesh.uv = split.uvCache;
            }
        }

        if (updateTopology)
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

        // Do not call RecalculateNormals if we have tangents computed
        if (!hasNormals && !hasTangents)
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

    static void UpdateAbcCamera(aiObject abc, Transform trans, float aspectRatio)
    {
        var cam = trans.GetComponent<Camera>();
        
        if (cam == null)
        {
            cam = trans.gameObject.AddComponent<Camera>();
        }

        aiCameraParams cp = default(aiCameraParams);

        cp.targetAspect = aspectRatio;

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
