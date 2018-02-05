using UnityEngine;


namespace UTJ.Alembic
{
    public abstract class AlembicCustomComponentCapturer : MonoBehaviour
    {
        public abstract void CreateAbcObject(aeObject parent);
        public abstract void Capture();
    }
}
