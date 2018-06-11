using UnityEngine;

namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    public class AlembicStreamPlayer : MonoBehaviour
    {
        // "m_" prefix is intentionally missing and expose fields as public just to keep asset compatibility...
        public AlembicStream abcStream;
        public AlembicStreamDescriptor streamDescriptor;
        public double startTime = double.MinValue;
        public double endTime = double.MaxValue;
        public float currentTime;
        public float vertexMotionScale = 1.0f;
        public bool asyncLoad = true;
        public bool ignoreVisibility = false;
        float lastUpdateTime;
        bool forceUpdate = false;
        bool updateStarted = false;

        public double duration { get { return endTime - startTime; } }


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
            startTime = Mathf.Clamp((float)startTime, (float)streamDescriptor.abcStartTime, (float)streamDescriptor.abcEndTime);
            endTime = Mathf.Clamp((float)endTime, (float)startTime, (float)streamDescriptor.abcEndTime);
            ClampTime();
            forceUpdate = true;
        }

        void Update()
        {
            if (abcStream == null || streamDescriptor == null)
                return;

            ClampTime();
            if (lastUpdateTime != currentTime || forceUpdate)
            {
                abcStream.vertexMotionScale = vertexMotionScale;
                abcStream.asyncLoad = asyncLoad;
                abcStream.ignoreVisibility = ignoreVisibility;
                if (abcStream.AbcUpdateBegin(startTime + currentTime))
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
            AbcAPI.aiCleanup();
        }
        #endregion
    }
}
