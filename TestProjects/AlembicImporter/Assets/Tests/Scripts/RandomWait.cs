using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{
    public class RandomWait : MonoBehaviour
    {
        public float m_minWaitMillisec;
        public float m_maxWaitMillisec;

        void LateUpdate()
        {
            var t = Random.Range(m_minWaitMillisec, m_maxWaitMillisec);
            System.Threading.Thread.Sleep((int)t);
        }
    }
}
