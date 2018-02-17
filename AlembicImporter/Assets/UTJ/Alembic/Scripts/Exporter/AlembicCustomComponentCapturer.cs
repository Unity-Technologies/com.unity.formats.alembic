using UnityEngine;


namespace UTJ.Alembic
{
    public abstract class AlembicCustomComponentCapturer : MonoBehaviour
    {
        public AlembicRecorder recorder;
        public AlembicRecorder.CustomCapturerHandler handler;

        public abstract void CreateAbcObject(aeObject parent);
        public abstract void Capture();
        public abstract void WriteSample();
    }
}
