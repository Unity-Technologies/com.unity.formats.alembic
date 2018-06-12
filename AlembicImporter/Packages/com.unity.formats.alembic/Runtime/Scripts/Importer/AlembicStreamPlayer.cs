using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    [ExecuteInEditMode]
    public class AlembicStreamPlayer : MonoBehaviour
    {
        // "m_" prefix is intentionally missing and expose fields as public just to keep asset compatibility...
        public AlembicStream abcStream { get; set; }
        public AlembicStreamDescriptor streamDescriptor { get; set; }

        private double startTime = double.MinValue;
        public double StartTime
        {
            get { return startTime; }
            set { startTime = value; }
        }

        private double endTime = double.MaxValue;
        public double EndTime
        {
            get { return endTime; }
            set { endTime = value; }
        }

        public float currentTime { get; set; }

        private float vertexMotionScale = 1.0f;
        public float VertexMotionScale
        {
            get { return vertexMotionScale; }
            set { vertexMotionScale = value; }
        }

        private bool asyncLoad = true;
        public bool AsyncLoad
        {
            get { return asyncLoad; }
            set { asyncLoad = value; }
        }
        float lastUpdateTime;
        bool forceUpdate = false;
        bool updateStarted = false;

        public double duration { get { return EndTime - StartTime; } }


        void ClampTime()
        {
            currentTime = Mathf.Clamp((float)currentTime, 0.0f, (float)duration);
        }

        public void LoadStream(bool createMissingNodes)
        {
            if (streamDescriptor == null)
                return;
            abcStream = new AlembicStream(gameObject, streamDescriptor);
            abcStream.AbcLoad(createMissingNodes);
            forceUpdate = true;
        }


        #region messages
        void Start()
        {
            OnValidate();
        }

        void OnValidate()
        {
            if (streamDescriptor == null || abcStream == null)
                return;
            if (streamDescriptor.abcStartTime == double.MinValue || streamDescriptor.abcEndTime == double.MaxValue)
                abcStream.GetTimeRange(ref streamDescriptor.abcStartTime, ref streamDescriptor.abcEndTime);
            StartTime = Mathf.Clamp((float)StartTime, (float)streamDescriptor.abcStartTime, (float)streamDescriptor.abcEndTime);
            EndTime = Mathf.Clamp((float)EndTime, (float)StartTime, (float)streamDescriptor.abcEndTime);
            ClampTime();
            forceUpdate = true;
        }

        public void Update()
        {
            if (abcStream == null || streamDescriptor == null)
                return;

            ClampTime();
            if (lastUpdateTime != currentTime || forceUpdate)
            {
                abcStream.SetVertexMotionScale(VertexMotionScale);
                abcStream.SetAsyncLoad(AsyncLoad);
                if (abcStream.AbcUpdateBegin(StartTime + currentTime))
                {
                    lastUpdateTime = currentTime;
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

        void OnDestroy()
        {
            if (abcStream != null)
                abcStream.Dispose();
        }

        void OnApplicationQuit()
        {
            NativeMethods.aiCleanup();
        }
        #endregion
    }
}
