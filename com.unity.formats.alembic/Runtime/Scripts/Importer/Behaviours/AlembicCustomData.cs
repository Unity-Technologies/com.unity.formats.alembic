using System.Collections.Generic;

namespace UnityEngine.Formats.Alembic.Importer
{
    public class AlembicCustomData : MonoBehaviour
    {
        [SerializeField]
        List<string> facesetName;

        internal void SetFacesetNames(List<string> names)
        {
            facesetName = names;
        }
    }
}
