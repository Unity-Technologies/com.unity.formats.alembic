using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


public abstract class AlembicCustomComponentCapturer : MonoBehaviour
{
    public abstract void SetParent(aeAPI.aeObject parent);
    public abstract void Capture();
}



[ExecuteInEditMode]
[AddComponentMenu("Alembic/Recorder")]
public class AlembicExporter : MonoBehaviour
{
    #region impl

    public static void CaptureTransform(aeAPI.aeObject abc, Transform trans)
    {
        aeAPI.aeXFormSampleData data;
        data.inherits = false;
        data.translation = trans.position;
        data.scale = trans.lossyScale;
        trans.rotation.ToAngleAxis(out data.rotationAngle, out data.rotationAxis);
        aeAPI.aeXFormWriteSample(abc, ref data);
    }

    public static void CaptureCamera(aeAPI.aeObject abc, Camera cam)
    {
        var data = new aeAPI.aeCameraSampleData();
        data.nearClippingPlane = cam.nearClipPlane;
        data.farClippingPlane = cam.farClipPlane;
        data.fieldOfView = cam.fieldOfView;
        aeAPI.aeCameraWriteSample(abc, ref data);
    }

    public static void CaptureMesh(aeAPI.aeObject abc, Mesh mesh)
    {
        var data = new aeAPI.aePolyMeshSampleData();
        var indices = mesh.GetIndices(0); // todo: record all submeshes
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var uvs = mesh.uv;
        data.indices = Marshal.UnsafeAddrOfPinnedArrayElement(indices, 0);
        data.positions = Marshal.UnsafeAddrOfPinnedArrayElement(vertices, 0);
        if(normals != null)
        {
            data.normals = Marshal.UnsafeAddrOfPinnedArrayElement(normals, 0);
        }
        if(uvs != null)
        {
            data.uvs = Marshal.UnsafeAddrOfPinnedArrayElement(uvs, 0);
        }
        data.vertexCount = vertices.Length;
        data.indexCount = indices.Length;

        aeAPI.aePolyMeshWriteSample(abc, ref data);
    }


    public abstract class ComponentCapturer
    {
        public abstract void Capture();
    }

    public class TransformCapturer : ComponentCapturer
    {
        Transform m_target;
        aeAPI.aeObject m_abc;

        public TransformCapturer(Transform target, aeAPI.aeObject abc)
        {
            m_target = target;
            m_abc = abc;
        }

        public override void Capture()
        {
            if (m_target != null)
            {
                CaptureTransform(m_abc, m_target);
            }
        }
    }

    public class CameraCapturer : ComponentCapturer
    {
        Camera m_target;
        aeAPI.aeObject m_abc;

        public CameraCapturer(Camera target, aeAPI.aeObject abc)
        {
            m_target = target;
            m_abc = abc;
        }

        public override void Capture()
        {
            if (m_target != null)
            {
                CaptureCamera(m_abc, m_target);
            }
        }
    }

    public class MeshCapturer : ComponentCapturer
    {
        MeshRenderer m_target;
        aeAPI.aeObject m_abc;

        public MeshCapturer(MeshRenderer target, aeAPI.aeObject abc)
        {
            m_target = target;
            m_abc = abc;
        }

        public override void Capture()
        {
            if(m_target != null)
            {
                CaptureMesh(m_abc, m_target.GetComponent<MeshFilter>().sharedMesh);
            }
        }
    }

    public class SkinnedMeshCapturer : ComponentCapturer
    {
        SkinnedMeshRenderer m_target;
        aeAPI.aeObject m_abc;

        public SkinnedMeshCapturer(SkinnedMeshRenderer target, aeAPI.aeObject abc)
        {
            m_target = target;
            m_abc = abc;
        }

        public override void Capture()
        {
            if (m_target != null)
            {
                CaptureMesh(m_abc, m_target.sharedMesh);
            }
        }
    }

    public class CustomCapturerHandler : ComponentCapturer
    {
        AlembicCustomComponentCapturer m_target;

        public CustomCapturerHandler(AlembicCustomComponentCapturer target)
        {
            m_target = target;
        }

        public override void Capture()
        {
            if (m_target != null)
            {
                m_target.Capture();
            }
        }
    }

    #endregion


    public enum Scope
    {
        EntireScene,
        CurrentBranch,
    }

    public string m_path;
    public aeAPI.aeConfig m_conf = aeAPI.aeConfig.default_value;

    public Scope m_scope = Scope.EntireScene;
    public bool m_preserveTreeStructure = false;

    public bool m_captureMeshRenderer = true;
    public bool m_captureSkinnedMeshRenderer = true;
    public bool m_captureParticleRenderer = true;
    public bool m_captureCamera = true;
    public bool m_captureCustomRecorder = true;

    aeAPI.aeContext m_ctx;
    List<ComponentCapturer> m_components = new List<ComponentCapturer>();
    bool m_recording;
    float m_time;
    int m_frameCount;


    public bool isRecording { get { return m_recording; } }
    public float time { get { return m_time; } set { m_time = value; } }
    public float frameCount { get { return m_frameCount; } }


    T[] GetTargets<T>() where T : Component
    {
        if(m_scope == Scope.CurrentBranch)
        {
            return GetComponentsInChildren<T>();
        }
        else
        {
            return FindObjectsOfType<T>();
        }
    }

    public bool BeginRecording()
    {
        if(m_recording) {
            Debug.Log("already recording");
            return false;
        }

        m_ctx = aeAPI.aeCreateContext(ref m_conf);
        if(m_ctx.ptr == IntPtr.Zero) {
            Debug.Log("aeCreateContext() failed");
            return false;
        }
        if(!aeAPI.aeOpenArchive(m_ctx, m_path))
        {
            Debug.Log("aeOpenArchive() failed");
            aeAPI.aeDestroyContext(m_ctx);
            m_ctx = new aeAPI.aeContext();
            return false;
        }


        var top = aeAPI.aeGetTopObject(m_ctx);

        if(m_preserveTreeStructure)
        {
            Debug.LogWarning("AlembicExporter: preserveTreeStructure is not implemented yet...");
            m_preserveTreeStructure = false;
        }

        if (m_captureCamera)
        {
            foreach(var target in GetTargets<Camera>())
            {
                var trans_obj = aeAPI.aeNewXForm(top, target.name + "_trans");
                var trans_rec = new TransformCapturer(target.GetComponent<Transform>(), trans_obj);
                m_components.Add(trans_rec);

                var cam_obj = aeAPI.aeNewCamera(trans_obj, target.name);
                var cam_rec = new CameraCapturer(target, cam_obj);
                m_components.Add(cam_rec);
            }
        }

        if (m_captureMeshRenderer)
        {
            foreach (var target in GetTargets<MeshRenderer>())
            {
                var trans_obj = aeAPI.aeNewXForm(top, target.name + "_trans");
                var trans_rec = new TransformCapturer(target.GetComponent<Transform>(), trans_obj);
                m_components.Add(trans_rec);

                var mesh_obj = aeAPI.aeNewPolyMesh(trans_obj, target.name);
                var mesh_rec = new MeshCapturer(target, mesh_obj);
                m_components.Add(mesh_rec);
            }
        }

        if (m_captureSkinnedMeshRenderer)
        {
            foreach (var target in GetTargets<SkinnedMeshRenderer>())
            {
                var trans_obj = aeAPI.aeNewXForm(top, target.name + "_trans");
                var trans_rec = new TransformCapturer(target.GetComponent<Transform>(), trans_obj);
                m_components.Add(trans_rec);

                var mesh_obj = aeAPI.aeNewPolyMesh(trans_obj, target.name);
                var mesh_rec = new SkinnedMeshCapturer(target, mesh_obj);
                m_components.Add(mesh_rec);
            }
        }

        if (m_captureCustomRecorder)
        {
            foreach (var target in GetTargets<AlembicCustomComponentCapturer>())
            {
                target.SetParent(top);
                var mesh_rec = new CustomCapturerHandler(target);
                m_components.Add(mesh_rec);
            }
        }

        m_recording = true;
        m_time = 0.0f;

        Debug.Log("AlembicExporter: start " + m_path);
        return true;
    }

    public void EndRecording()
    {
        if (!m_recording) { return; }

        m_components.Clear();
        aeAPI.aeDestroyContext(m_ctx);
        m_ctx = new aeAPI.aeContext();
        m_recording = false;
        m_frameCount = 0;

        Debug.Log("AlembicExporter: end: " + m_path);
    }

    public void OneShot()
    {
        if (BeginRecording())
        {
            aeAPI.aeAddTime(m_ctx, 0.0f);
            foreach (var recorder in m_components)
            {
                recorder.Capture();
            }
            EndRecording();
        }
    }

    IEnumerator ProcessRecording()
    {
        yield return new WaitForEndOfFrame();
        if(!m_recording) { yield break; }

        aeAPI.aeAddTime(m_ctx, m_time);
        foreach(var recorder in m_components) {
            recorder.Capture();
        }
        ++m_frameCount;


        // wait maximumDeltaTime if timeSamplingType is uniform
        if (m_conf.timeSamplingType == aeAPI.aeTypeSamplingType.Uniform)
        {
            Time.maximumDeltaTime = m_conf.timePerSample;
            aeAPI.aeWaitMaxDeltaTime();
        }
    }


    void Reset()
    {
        if(m_path == null || m_path == "")
        {
            m_path = gameObject.name + ".abc";
        }
    }

    void Update()
    {
        if(m_recording)
        {
            m_time += Time.deltaTime;
            StartCoroutine(ProcessRecording());
        }
    }

    void OnDisable()
    {
        EndRecording();
    }
}
