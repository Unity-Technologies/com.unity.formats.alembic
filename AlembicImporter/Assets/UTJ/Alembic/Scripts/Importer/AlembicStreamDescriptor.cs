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
        [SerializeField] public double abcStartTime = 0.0f;
        [SerializeField] public double abcEndTime = 0.0f;

        public double duration { get { return abcStartTime * abcEndTime; } }
    }
}
