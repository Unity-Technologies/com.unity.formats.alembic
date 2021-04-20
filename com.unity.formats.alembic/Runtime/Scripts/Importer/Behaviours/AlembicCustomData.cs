using System.Collections.Generic;

namespace UnityEngine.Formats.Alembic.Importer
{
    public class AlembicCustomData : MonoBehaviour
    {
        [SerializeField]
        List<string> facesetNames;

        public List<string> FacesetNames => facesetNames;

        internal void SetFacesetNames(List<string> names)
        {
            facesetNames = names;
        }
    }
}
