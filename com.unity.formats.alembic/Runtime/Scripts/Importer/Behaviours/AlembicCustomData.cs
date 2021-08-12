using System.Collections.Generic;

namespace UnityEngine.Formats.Alembic.Importer
{
    /// <summary>
    /// Class that stores ancillary data for the current Alembic Node.
    /// </summary>
    public class AlembicCustomData : MonoBehaviour
    {
        [SerializeField]
        List<string> faceSetNames;

        /// <summary>
        /// Retrieves the set of Face Set names stored in the current Alembic node.
        /// </summary>
        public List<string> FaceSetNames => faceSetNames;

        internal void SetFacesetNames(List<string> names)
        {
            faceSetNames = names;
            for (var i = 0; i < faceSetNames.Count; i++)
            {
                faceSetNames[i] = faceSetNames[i].TrimEnd('\0');
            }
        }
    }
}
