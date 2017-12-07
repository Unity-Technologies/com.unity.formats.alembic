using UnityEngine;
using UnityEngine.UI;

namespace UTJ.Alembic
{
    public class AlembicStreamDescriptor : ScriptableObject
    {
        [SerializeField] public string pathToAbc;
        [SerializeField] public AlembicStreamSettings settings = new AlembicStreamSettings();
        [SerializeField] public bool hasVaryingTopology = false;
        [SerializeField] public bool hasAcyclicFramerate = false;
        [SerializeField] public int minFrame = 0;
        [SerializeField] public int maxFrame = 0;
        [SerializeField] public float abcDuration = 0.0f;
        [SerializeField] public float abcStartTime = 0.0f;
        [SerializeField] public int abcFrameCount = 1;

        public float Duration
        {
            get
            {
               return abcFrameCount * FrameLength;
            }
        }

        public float FrameLength
        {
            get
            {
               if (abcFrameCount == 1) return 0;
               return abcDuration / (abcFrameCount-1);
            }
        }
    }
}
