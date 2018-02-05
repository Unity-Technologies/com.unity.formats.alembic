using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{
    public class RandomWait : MonoBehaviour
    {
        public float m_minWait;
        public float m_maxWait;

        void LateUpdate()
        {
            var t = Random.Range(m_minWait, m_maxWait);
            System.Threading.Thread.Sleep((int)t);
        }
    }
}
