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



[ExecuteInEditMode]
[AddComponentMenu("Alembic/Exporter")]
public class AlembicExporter : MonoBehaviour
{
    #region impl

    public static IntPtr GetArrayPtr(Array v)
    {
        return Marshal.UnsafeAddrOfPinnedArrayElement(v, 0);
    }

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
        var indices = mesh.triangles;
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var uvs = mesh.uv;

        data.indices = GetArrayPtr(indices);
        data.positions = GetArrayPtr(vertices);
        if(normals != null) { data.normals = GetArrayPtr(normals); }
        if(uvs != null)     { data.uvs = GetArrayPtr(uvs); }
        data.vertexCount = vertices.Length;
        data.indexCount = indices.Length;

        aeAPI.aePolyMeshWriteSample(abc, ref data);
    }



    public abstract class ComponentCapturer
    {
        protected aeAPI.aeObject m_abc;
        public abstract void Capture();

        public aeAPI.aeObject abc { get { return m_abc; } }
    }

    public class TransformCapturer : ComponentCapturer
    {
        Transform m_target;

        public TransformCapturer(Transform target, aeAPI.aeObject abc)
        {
            m_target = target;
            m_abc = abc;
        }

        public override void Capture()
        {
            if (m_target == null) { return; }

            CaptureTransform(m_abc, m_target);
        }
    }

    public class CameraCapturer : ComponentCapturer
    {
        Camera m_target;

        public CameraCapturer(Camera target, aeAPI.aeObject abc)
        {
            m_target = target;
            m_abc = abc;
        }

        public override void Capture()
        {
            if (m_target == null) { return; }

            CaptureCamera(m_abc, m_target);
        }
    }

    public class MeshCapturer : ComponentCapturer
    {
        MeshRenderer m_target;

        public MeshCapturer(MeshRenderer target, aeAPI.aeObject abc)
        {
            m_target = target;
            m_abc = abc;
        }

        public override void Capture()
        {
            if (m_target == null) { return; }

            CaptureMesh(m_abc, m_target.GetComponent<MeshFilter>().sharedMesh);
        }
    }

    public class SkinnedMeshCapturer : ComponentCapturer
    {
        SkinnedMeshRenderer m_target;
        Mesh m_mesh;

        public SkinnedMeshCapturer(SkinnedMeshRenderer target, aeAPI.aeObject abc)
        {
            m_target = target;
            m_abc = abc;
        }

        public override void Capture()
        {
            if (m_target == null) { return; }

            if (m_mesh == null) { m_mesh = new Mesh(); }
            m_target.BakeMesh(m_mesh);
            CaptureMesh(m_abc, m_mesh);
        }
    }

    public class ParticleCapturer : ComponentCapturer
    {
        aeAPI.aeProperty m_prop_rotatrions;
        ParticleSystem m_target;
        ParticleSystem.Particle[] m_buf_particles;
        Vector3[] m_buf_positions;
        Vector3[] m_buf_rotations;

        public ParticleCapturer(ParticleSystem target, aeAPI.aeObject abc)
        {
            m_target = target;
            m_abc = abc;
            m_prop_rotatrions = aeAPI.aeNewProperty(m_abc, "rotation", aeAPI.aePropertyType.Vec3Array);
        }

        public override void Capture()
        {
            if (m_target == null) { return; }

            // create buffer
            int count_max = m_target.maxParticles;
            if (m_buf_particles == null)
            {
                m_buf_particles = new ParticleSystem.Particle[count_max];
                m_buf_positions = new Vector3[count_max];
                m_buf_rotations = new Vector3[count_max];
            }
            else if (m_buf_particles.Length != count_max)
            {
                Array.Resize(ref m_buf_particles, count_max);
                Array.Resize(ref m_buf_positions, count_max);
                Array.Resize(ref m_buf_rotations, count_max);
            }

            // copy particle positions & rotations to buffer
            int count = m_target.GetParticles(m_buf_particles);
            for (int i = 0; i < count; ++i)
            {
                m_buf_positions[i] = m_buf_particles[i].position;
            }
            for (int i = 0; i < count; ++i)
            {
                m_buf_rotations[i] = m_buf_particles[i].rotation3D;
            }

            // write!
            var data = new aeAPI.aePointsSampleData();
            data.positions = GetArrayPtr(m_buf_positions);
            data.count = count;
            aeAPI.aePointsWriteSample(m_abc, ref data);
            aeAPI.aePropertyWriteSample(m_prop_rotatrions, GetArrayPtr(m_buf_rotations), count);
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
            if (m_target == null) { return; }

            m_target.Capture();
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
    public bool m_captureParticleSystem = true;
    public bool m_captureCamera = true;
    public bool m_enableCustomCapturer = true;
    public bool m_ignoreDisabled = true;

    aeAPI.aeContext m_ctx;
    List<ComponentCapturer> m_capturers = new List<ComponentCapturer>();
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


    public TransformCapturer CreateTransformCapturer(GameObject target, aeAPI.aeObject parent)
    {
        var abc = aeAPI.aeNewXForm(parent, target.name + " (" + target.GetHashCode().ToString("X8") + ")");
        var cap = new TransformCapturer(target.GetComponent<Transform>(), abc);
        m_capturers.Add(cap);
        return cap;
    }



    public bool BeginCapture()
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


        // create component captures

        // Camera
        if (m_captureCamera)
        {
            foreach(var target in GetTargets<Camera>())
            {
                if (m_ignoreDisabled && (!target.gameObject.activeInHierarchy || !target.enabled)) { continue; }
                var trans = CreateTransformCapturer(target.gameObject, top);

                var cam_abc = aeAPI.aeNewCamera(trans.abc, target.name);
                var cam_cap = new CameraCapturer(target, cam_abc);
                m_capturers.Add(cam_cap);
            }
        }

        // MeshRenderer
        if (m_captureMeshRenderer)
        {
            foreach (var target in GetTargets<MeshRenderer>())
            {
                if (m_ignoreDisabled && (!target.gameObject.activeInHierarchy || !target.enabled)) { continue; }
                var trans = CreateTransformCapturer(target.gameObject, top);

                var mesh_abc = aeAPI.aeNewPolyMesh(trans.abc, target.name);
                var mesh_cap = new MeshCapturer(target, mesh_abc);
                m_capturers.Add(mesh_cap);
            }
        }

        // SkinnedMeshRenderer
        if (m_captureSkinnedMeshRenderer)
        {
            foreach (var target in GetTargets<SkinnedMeshRenderer>())
            {
                if (m_ignoreDisabled && (!target.gameObject.activeInHierarchy || !target.enabled)) { continue; }
                var trans = CreateTransformCapturer(target.gameObject, top);

                var mesh_abc = aeAPI.aeNewPolyMesh(trans.abc, target.name);
                var mesh_cap = new SkinnedMeshCapturer(target, mesh_abc);
                m_capturers.Add(mesh_cap);
            }
        }

        // ParticleSystem
        if (m_captureParticleSystem)
        {
            foreach (var target in GetTargets<ParticleSystem>())
            {
                if (m_ignoreDisabled && (!target.gameObject.activeInHierarchy)) { continue; }
                var trans = CreateTransformCapturer(target.gameObject, top);

                var particle_abc = aeAPI.aeNewPoints(trans.abc, target.name);
                var particle_cap = new ParticleCapturer(target, particle_abc);
                m_capturers.Add(particle_cap);
            }
        }

        // handle custom capturers (AlembicCustomComponentCapturer)
        if (m_enableCustomCapturer)
        {
            foreach (var target in GetTargets<AlembicCustomComponentCapturer>())
            {
                if (m_ignoreDisabled && (!target.gameObject.activeInHierarchy || !target.enabled)) { continue; }
                var trans = CreateTransformCapturer(target.gameObject, top);

                target.SetParent(trans.abc);
                var capturer = new CustomCapturerHandler(target);
                m_capturers.Add(capturer);
            }
        }


        m_recording = true;
        m_time = 0.0f;

        Debug.Log("AlembicExporter: start " + m_path);
        return true;
    }

    public void EndCapture()
    {
        if (!m_recording) { return; }

        m_capturers.Clear();
        aeAPI.aeDestroyContext(m_ctx);
        m_ctx = new aeAPI.aeContext();
        m_recording = false;
        m_frameCount = 0;

        Debug.Log("AlembicExporter: end: " + m_path);
    }

    public void OneShot()
    {
        if (BeginCapture())
        {
            aeAPI.aeAddTime(m_ctx, 0.0f);
            foreach (var recorder in m_capturers)
            {
                recorder.Capture();
            }
            EndCapture();
        }
    }

    IEnumerator ProcessRecording()
    {
        yield return new WaitForEndOfFrame();
        if(!m_recording) { yield break; }

        aeAPI.aeAddTime(m_ctx, m_time);
        foreach(var recorder in m_capturers) {
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
        EndCapture();
    }
}
