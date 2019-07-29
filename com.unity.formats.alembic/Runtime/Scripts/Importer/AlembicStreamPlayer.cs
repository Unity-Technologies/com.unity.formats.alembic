using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEngine.Formats.Alembic.Importer
{
    [ExecuteInEditMode]
    public class AlembicStreamPlayer : MonoBehaviour
    {
        // "m_" prefix is intentionally missing and expose fields as public just to keep asset compatibility...
        AlembicStream abcStream { get; set; }
        [SerializeField]
        AlembicStreamDescriptor streamDescriptor;
        internal AlembicStreamDescriptor StreamDescriptor
        {
            get { return streamDescriptor; }
            set { streamDescriptor = value; }
        }

        [SerializeField]
        float startTime = float.MinValue;
        public float StartTime
        {
            get { return startTime; }
            set
            {
                startTime = value;
                if (StreamDescriptor == null)
                    return;
                startTime = Mathf.Clamp(startTime, (float)StreamDescriptor.abcStartTime, (float)StreamDescriptor.abcEndTime);
            }
        }

        [SerializeField]
        float endTime = float.MaxValue;
        public float EndTime
        {
            get { return endTime; }
            set
            {
                endTime = value;
                if (StreamDescriptor == null)
                    return;
                endTime = Mathf.Clamp(endTime, StartTime, (float)StreamDescriptor.abcEndTime);
            }
        }

        [SerializeField]
        float currentTime;
        public float CurrentTime
        {
            get { return currentTime; }
            set { currentTime = Mathf.Clamp(value, 0.0f, Duration); }
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
        bool AsyncLoad
        {
            get { return asyncLoad; }
            set { asyncLoad = value; }
        }
        float lastUpdateTime;
        bool forceUpdate = false;
        bool updateStarted = false;

        public float Duration { get { return EndTime - StartTime; } }


        void ClampTime()
        {
            CurrentTime = Mathf.Clamp(CurrentTime, 0.0f, Duration);
        }

        internal void LoadStream(bool createMissingNodes)
        {
            if (StreamDescriptor == null)
                return;
            abcStream = new AlembicStream(gameObject, StreamDescriptor);
            abcStream.AbcLoad(createMissingNodes, false);
            forceUpdate = true;
        }
        
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
            StartTime = Mathf.Clamp(StartTime, (float)StreamDescriptor.abcStartTime, (float)StreamDescriptor.abcEndTime);
            EndTime = Mathf.Clamp(EndTime, StartTime, (float)StreamDescriptor.abcEndTime);
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
                abcStream.SetAsyncLoad(AsyncLoad );
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
    }
}
