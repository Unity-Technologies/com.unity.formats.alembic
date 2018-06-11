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
        [SerializeField] public double abcStartTime = double.MinValue;
        [SerializeField] public double abcEndTime = double.MaxValue;

        public double duration { get { return abcEndTime - abcStartTime; } }
    }
}
