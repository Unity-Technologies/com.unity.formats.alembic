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

    [DllImport ("AlembicImporter")] public static extern IntPtr     aiCreateContext();
    [DllImport ("AlembicImporter")] public static extern void       aiDestroyContext(IntPtr ctx);
    
    [DllImport ("AlembicImporter")] public static extern bool       aiLoad(IntPtr ctx, string path);
    [DllImport ("AlembicImporter")] public static extern IntPtr     aiGetTopObject(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern void       aiEnumerateChild(IntPtr ctx, IntPtr obj, aiNodeEnumerator e, IntPtr userdata);
    [DllImport ("AlembicImporter")] public static extern void       aiSetCurrentObject(IntPtr ctx, IntPtr obj);
    [DllImport ("AlembicImporter")] public static extern void       aiSetCurrentTime(IntPtr ctx, float time);

    [DllImport ("AlembicImporter")] private static extern IntPtr    aiGetNameS(IntPtr ctx);
    [DllImport ("AlembicImporter")] private static extern IntPtr    aiGetFullNameS(IntPtr ctx);
    public static string aiGetName(IntPtr ctx)      { return Marshal.PtrToStringAnsi(aiGetNameS(ctx)); }
    public static string aiGetFullName(IntPtr ctx)  { return Marshal.PtrToStringAnsi(aiGetFullNameS(ctx)); }

    [DllImport ("AlembicImporter")] public static extern uint       aiGetNumChildren(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern bool       aiHasXForm(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern Vector3    aiGetPosition(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern Vector3    aiGetRotation(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern Vector3    aiGetScale(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern Matrix4x4  aiGetMatrix(IntPtr ctx);

    [DllImport ("AlembicImporter")] public static extern bool       aiHasPolyMesh(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern uint       aiGetIndexCount(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern uint       aiGetVertexCount(IntPtr ctx);
    [DllImport ("AlembicImporter")] public static extern void       aiCopyIndices(IntPtr ctx, IntPtr indices, bool reverse=false);
    [DllImport ("AlembicImporter")] public static extern void       aiCopyVertices(IntPtr ctx, IntPtr vertices);


    [MenuItem ("Assets/Import Alembic")]
    static void Import()
    {
        var path = EditorUtility.OpenFilePanel("", "", "abc");
        var filename = System.IO.Path.GetFileNameWithoutExtension(path);

        IntPtr ctx = aiCreateContext();
        if (!aiLoad(ctx, path))
        {
            Debug.Log("aiLoad(\"" + path + "\") failed");
        }
        else
        {
            GameObject root = new GameObject();
            root.name = filename;
            var abcstream = root.AddComponent<AlembicStream>();
            abcstream.m_path_to_abc = path;

            GCHandle gch = GCHandle.Alloc(root.GetComponent<Transform>());
            aiEnumerateChild(ctx, aiGetTopObject(ctx), ImportEnumerator, GCHandle.ToIntPtr(gch));
        }
        aiDestroyContext(ctx);
    }

    public static void UpdateAbcTree(IntPtr ctx, Transform root, float time)
    {
        aiSetCurrentTime(ctx, time);

        GCHandle gch = GCHandle.Alloc(root);
        aiEnumerateChild(ctx, aiGetTopObject(ctx), ImportEnumerator, GCHandle.ToIntPtr(gch));
    }

    static void ImportEnumerator(IntPtr ctx, IntPtr node, IntPtr parent_addr)
    {
        Transform parent = GCHandle.FromIntPtr(parent_addr).Target as Transform;
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
            UpdateMesh(ctx, trans);
        }

        GCHandle gch = GCHandle.Alloc(trans);
        aiEnumerateChild(ctx, node, ImportEnumerator, GCHandle.ToIntPtr(gch));
    }

    static void UpdateMesh(IntPtr ctx, Transform trans)
    {
        Mesh mesh;
        var mesh_filter = trans.GetComponent<MeshFilter>();
        if (mesh_filter==null)
        {
            mesh = new Mesh();
            mesh_filter = trans.gameObject.AddComponent<MeshFilter>();
            mesh_filter.sharedMesh = mesh;

            var mesh_renderer = trans.gameObject.AddComponent<MeshRenderer>();
            mesh.name = trans.name = aiGetName(ctx);
            mesh_renderer.material = GetDefaultMaterial();
        }
        else
        {
            mesh = mesh_filter.sharedMesh;
        }

        {
            Vector3[] vertices = new Vector3[aiGetVertexCount(ctx)];
            int[] indices = new int[aiGetIndexCount(ctx)];
            aiCopyVertices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(vertices, 0));
            aiCopyIndices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(indices, 0), true);
            mesh.vertices = vertices;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();
        }
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
