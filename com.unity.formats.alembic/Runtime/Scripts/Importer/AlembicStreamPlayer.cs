using System;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.Rendering;

namespace UnityEngine.Formats.Alembic.Importer
{
    /// <summary>
    /// This component allows data streaming from Alembic files. It updates children nodes (Meshes, Transforms, Cameras, etc.) to reflect the Alembic data at the given time.
    /// </summary>
    [ExecuteInEditMode]
    public class AlembicStreamPlayer : MonoBehaviour
    {
        // "m_" prefix is intentionally missing and expose fields as public just to keep asset compatibility...
        AlembicStream abcStream { get; set; }
        [SerializeField]
        AlembicStreamDescriptor streamDescriptor;
        /// <summary>
        /// Gives access to the stream description.
        /// </summary>
        internal AlembicStreamDescriptor StreamDescriptor
        {
            get { return streamDescriptor; }
            set { streamDescriptor = value; }
        }

        [SerializeField]
        float startTime = float.MinValue;
        /// <summary>
        /// Get or set the start timestamp of the streaming time window (scale in seconds). This is clamped to the time range of the Alembic source file.
        /// </summary>
        public float StartTime
        {
            get { return startTime; }
            set
            {
                startTime = value;
                if (StreamDescriptor == null)
                    return;
                startTime = Mathf.Clamp(startTime, StreamDescriptor.mediaStartTime, StreamDescriptor.mediaEndTime);
            }
        }

        [SerializeField]
        float endTime = float.MaxValue;
        /// <summary>
        /// Get or set the end timestamp of the streaming time window (scale in seconds). This is clamped to the time range of the Alembic source file.
        /// </summary>
        public float EndTime
        {
            get { return endTime; }
            set
            {
                endTime = value;
                if (StreamDescriptor == null)
                    return;
                endTime = Mathf.Clamp(endTime, StartTime, StreamDescriptor.mediaEndTime);
            }
        }

        [SerializeField]
        float currentTime;
        /// <summary>
        /// Get or set the current time relative to the Alembic file time range (scale in seconds). This is clamped between 0 and the alembic time duration.
        /// </summary>
        public float CurrentTime
        {
            get { return currentTime; }
            set { currentTime = Mathf.Clamp(value, 0.0f, Duration); }
        }

        /// <summary>
        /// Get the duration of the Alembic file (in seconds).
        /// </summary>
        public float Duration { get { return EndTime - StartTime; } }

        [SerializeField]
        float vertexMotionScale = 1.0f;
        /// <summary>
        /// Get or set the scalar multiplier to the Alembic vertex speed (magnification factor for velocity). Default value is 1.
        /// </summary>
        public float VertexMotionScale
        {
            get { return vertexMotionScale; }
            set { vertexMotionScale = value; }
        }

        /// <summary>
        /// The start timestamp of the Alembic file (scale in seconds).
        /// </summary>
        public float MediaStartTime => StreamDescriptor ? StreamDescriptor.mediaStartTime : 0;
        /// <summary>
        /// The end timestamp of the Alembic file (scale in seconds).
        /// </summary>
        public float MediaEndTime => StreamDescriptor ? StreamDescriptor.mediaEndTime : 0;

        /// <summary>
        /// The duration of the Alembic file (in seconds).
        /// </summary>
        public float MediaDuration => MediaEndTime - MediaStartTime;

        /// <summary>
        /// The path to the Alembic asset. When in a standalone build, the returned path is prepended by the streamingAssets path.
        /// </summary>
        public string PathToAbc => StreamDescriptor != null ? StreamDescriptor.PathToAbc : "";

        /// <summary>
        /// The stream import options. NOTE: these options are shared between all instances of this asset.
        /// </summary>
        public AlembicStreamSettings Settings => StreamDescriptor != null ? StreamDescriptor.Settings : null;

        float lastUpdateTime;
        bool forceUpdate = false;
        bool updateStarted = false;


        /// <summary>
        /// Update the child GameObject's data to the CurrentTime (The regular update happens during the LateUpdate phase).
        /// </summary>
        /// <param name="time">The timestamp to stream from the asset file.</param>
        public void UpdateImmediately(float time)
        {
            CurrentTime = time;
            Update();
            LateUpdate();
        }

        /// <summary>
        /// Loads a different Alembic file.
        /// </summary>
        /// <param name="newPath">Path to the new file.</param>
        /// <returns>True if the load succeeded, false otherwise.</returns>
        internal bool LoadFromFile(string newPath)
        {
            if (StreamDescriptor == null)
            {
                StreamDescriptor = ScriptableObject.CreateInstance<AlembicStreamDescriptor>();
            }

            StreamDescriptor.PathToAbc = newPath;
            return InitializeAfterLoad();
        }

        bool InitializeAfterLoad()
        {
            var ret = LoadStream(true);
            if (!ret)
                return false;
            //abcStream.AbcLoad(true, true);
            double start, end;
            abcStream.GetTimeRange(out start, out end);
            startTime = (float)start;
            endTime = (float)end;

            streamDescriptor.mediaStartTime = (float)start;
            streamDescriptor.mediaEndTime = (float)end;

            var pipelineAsset = GraphicsSettings.renderPipelineAsset;
            var defaultMat = pipelineAsset != null
                ? pipelineAsset.defaultMaterial
                : new Material(Shader.Find("Standard"));

            foreach (var meshRenderer in gameObject.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = new Material[meshRenderer.sharedMaterials.Length];
                meshRenderer.sharedMaterials = Array.ConvertAll(mats, x => defaultMat);
            }

            foreach (var meshFilter in gameObject.GetComponentsInChildren<MeshFilter>())
            {
                meshFilter.sharedMesh.hideFlags |= HideFlags.DontSave;
            }

            return true;
        }

        void ClampTime()
        {
            CurrentTime = Mathf.Clamp(CurrentTime, 0.0f, Duration);
        }

        internal bool  LoadStream(bool createMissingNodes)
        {
            if (StreamDescriptor == null)
                return false;
            abcStream = new AlembicStream(gameObject, StreamDescriptor);
            var ret = abcStream.AbcLoad(createMissingNodes, false);
            forceUpdate = true;
            return ret;
        }

        void Start()
        {
            OnValidate();
        }

        void OnValidate()
        {
            if (StreamDescriptor == null || abcStream == null)
                return;
            if (StreamDescriptor.mediaStartTime == double.MinValue || StreamDescriptor.mediaEndTime == double.MaxValue)
            {
                double start, end;
                abcStream.GetTimeRange(out start, out end);
                StreamDescriptor.mediaStartTime = (float)start;
                StreamDescriptor.mediaEndTime = (float)end;
            }

            StartTime = Mathf.Clamp(StartTime, StreamDescriptor.mediaStartTime, StreamDescriptor.mediaEndTime);
            EndTime = Mathf.Clamp(EndTime, StartTime, StreamDescriptor.mediaEndTime);
            ClampTime();
            forceUpdate = true;
        }

        internal void Update()
        {
            if (abcStream == null || StreamDescriptor == null)
                return;

            ClampTime();
            if (lastUpdateTime != CurrentTime || forceUpdate)
            {
                abcStream.SetVertexMotionScale(VertexMotionScale);

                if (abcStream.AbcUpdateBegin(StartTime + CurrentTime))
                {
                    lastUpdateTime = CurrentTime;
                    forceUpdate = false;
                    updateStarted = true;
                }
                else
                {
                    abcStream.Dispose();
                    abcStream = null;
                    LoadStream(false);
                }
            }
        }

        void LateUpdate()
        {
            // currentTime maybe updated after Update() by other GameObjects
            if (!updateStarted && lastUpdateTime != currentTime)
                Update();

            if (!updateStarted)
                return;
            updateStarted = false;
            abcStream.AbcUpdateEnd();
        }

        void OnEnable()
        {
            if (abcStream == null)
                LoadStream(false);
        }

        void OnDisable()
        {
            if (abcStream != null)
                abcStream.Dispose();
        }

        void OnApplicationQuit()
        {
            NativeMethods.aiCleanup();
        }
    }
}
