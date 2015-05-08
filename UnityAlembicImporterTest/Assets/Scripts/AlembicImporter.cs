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
    public delegate void uaiNodeEnumerator(int ctx, IntPtr obj);

    [DllImport ("UnityAlembicImporter")] public static extern int       uaiCreateContext();
    [DllImport ("UnityAlembicImporter")] public static extern void      uaiDestroyContext(int ctx);
    
    [DllImport ("UnityAlembicImporter")] public static extern bool      uaiLoad(int ctx, string path);
    [DllImport ("UnityAlembicImporter")] public static extern IntPtr    uaiGetTopObject(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern void      uaiEnumerateChild(int ctx, IntPtr obj, uaiNodeEnumerator e);
    [DllImport ("UnityAlembicImporter")] public static extern void      uaiSetCurrentObject(int ctx, IntPtr obj);
    
    [DllImport ("UnityAlembicImporter")] public static extern IntPtr    uaiGetNameS(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern IntPtr    uaiGetFullNameS(int ctx);
    public static string uaiGetName(int ctx)    { return Marshal.PtrToStringAnsi(uaiGetNameS(ctx)); }
    public static string uaiGetFullName(int ctx){ return Marshal.PtrToStringAnsi(uaiGetFullNameS(ctx)); }

    [DllImport ("UnityAlembicImporter")] public static extern uint      uaiGetNumChildren(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern bool      uaiHasXForm(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern bool      uaiHasPolyMesh(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern Vector3   uaiGetPosition(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern Vector3   uaiGetRotation(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern Vector3   uaiGetScale(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern Matrix4x4 uaiGetMatrix(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern uint      uaiGetVertexCount(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern uint      uaiGetIndexCount(int ctx);
    [DllImport ("UnityAlembicImporter")] public static extern void      uaiCopyVertices(int ctx, IntPtr vertices);
    [DllImport ("UnityAlembicImporter")] public static extern void      uaiCopyIndices(int ctx, IntPtr indices, bool reverse=false);


    static Transform s_parent;

    [MenuItem ("AlembicImporter/Import")]
    static void Test()
    {
        var path = EditorUtility.OpenFilePanel("", "", "abc");

        int ctx = uaiCreateContext();
        if (!uaiLoad(ctx, path))
        {
            Debug.Log("uaiLoad(\"" + path + "\") failed");
        }
        else
        {
            uaiEnumerateChild(ctx, uaiGetTopObject(ctx), TestCallback);
        }
        uaiDestroyContext(ctx);
    }

    static void TestCallback(int ctx, IntPtr node)
    {
        bool xf = uaiHasXForm(ctx);
        bool mesh = uaiHasPolyMesh(ctx);
        Debug.Log("Node: " + uaiGetFullName(ctx) + " (" + (xf ? "x" : "") + (mesh ? "p" : "") + ")");

        Transform prev_parent = s_parent;
        GameObject go = new GameObject();
        go.name = uaiGetName(ctx);
        var trans = go.GetComponent<Transform>();
        trans.parent = s_parent;
        s_parent = trans;
        if (xf)
        {
            trans.localPosition = uaiGetPosition(ctx);
            trans.localEulerAngles = uaiGetRotation(ctx);
            trans.localScale = uaiGetScale(ctx);
        }
        else
        {
            trans.localPosition = Vector3.zero;
            trans.localEulerAngles = Vector3.zero;
            trans.localScale = Vector3.one;
        }
        if (mesh)
        {
            GenerateMesh(ctx, go);
        }

        uaiEnumerateChild(ctx, node, TestCallback);

        s_parent = prev_parent;
    }

    static void GenerateMesh(int ctx, GameObject go)
    {
        Mesh mesh = new Mesh();
        {
            Vector3[] vertices = new Vector3[uaiGetVertexCount(ctx)];
            int[] indices = new int[uaiGetIndexCount(ctx)];
            uaiCopyVertices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(vertices, 0));
            uaiCopyIndices(ctx, Marshal.UnsafeAddrOfPinnedArrayElement(indices, 0), true);
            mesh.vertices = vertices;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        }

        mesh.name = go.name = uaiGetName(ctx);
        var mesh_filter = go.AddComponent<MeshFilter>();
        var mesh_renderer = go.AddComponent<MeshRenderer>();
        mesh_filter.mesh = mesh;
        mesh_renderer.material = GetDefaultMaterial();
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
