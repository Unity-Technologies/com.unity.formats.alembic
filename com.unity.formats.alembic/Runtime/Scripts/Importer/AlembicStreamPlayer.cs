using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    [ExecuteInEditMode]
    internal class AlembicStreamPlayer : MonoBehaviour
    {
        // "m_" prefix is intentionally missing and expose fields as public just to keep asset compatibility...
        public AlembicStream abcStream { get; set; }
        [SerializeField]
        private AlembicStreamDescriptor streamDescriptor;
        public AlembicStreamDescriptor StreamDescriptor
        {
            get { return streamDescriptor; }
            set { streamDescriptor = value; }
        }

        [SerializeField]
        private double startTime = double.MinValue;
        public double StartTime
        {
            get { return startTime; }
            set { startTime = value; }
        }

        [SerializeField]
        private double endTime = double.MaxValue;
        public double EndTime
        {
            get { return endTime; }
            set { endTime = value; }
        }

        [SerializeField]
        private float currentTime;
        public float CurrentTime
        {
            get { return currentTime; }
            set { currentTime = value; }
        }

        [SerializeField]
        private float vertexMotionScale = 1.0f;
        public float VertexMotionScale
        {
            get { return vertexMotionScale; }
            set { vertexMotionScale = value; }
        }

        [SerializeField]
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
            CurrentTime = Mathf.Clamp((float)CurrentTime, 0.0f, (float)duration);
        }

        public void LoadStream(bool createMissingNodes)
        {
            if (StreamDescriptor == null)
                return;
            abcStream = new AlembicStream(gameObject, StreamDescriptor);
            abcStream.AbcLoad(createMissingNodes, false);
            forceUpdate = true;
        }

        #region messages
        void Start()
        {
            OnValidate();
        }

        void OnValidate()
        {
            if (StreamDescriptor == null || abcStream == null)
                return;
            if (StreamDescriptor.abcStartTime == double.MinValue || StreamDescriptor.abcEndTime == double.MaxValue)
                abcStream.GetTimeRange(ref StreamDescriptor.abcStartTime, ref StreamDescriptor.abcEndTime);
            StartTime = Mathf.Clamp((float)StartTime, (float)StreamDescriptor.abcStartTime, (float)StreamDescriptor.abcEndTime);
            EndTime = Mathf.Clamp((float)EndTime, (float)StartTime, (float)StreamDescriptor.abcEndTime);
            ClampTime();
            forceUpdate = true;
        }

        public void Update()
        {
            if (abcStream == null || StreamDescriptor == null)
                return;

            ClampTime();
            if (lastUpdateTime != CurrentTime || forceUpdate)
            {
                abcStream.SetVertexMotionScale(VertexMotionScale);
                abcStream.SetAsyncLoad(AsyncLoad);
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
