using System.Collections.Generic;
using UnityEngine;


namespace UTJ.Alembic
{
    public class TestCreateAndDelete : MonoBehaviour
    {
        List<GameObject> m_gos = new List<GameObject>();
        int m_frame;
        readonly int maxSimultaneousObjects = 8;

        void Update()
        {
            if(m_frame % 10 == 1)
            {
                int gi = m_frame / 10;
                var position = new Vector3(1.0f * (gi / 2), 1.0f * (gi % 2), 0.0f);

                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = position;
                go.transform.SetParent(transform, false);
                m_gos.Add(go);

                if(m_gos.Count > maxSimultaneousObjects)
                {
                    for (int i = 0; i < m_gos.Count - maxSimultaneousObjects; ++i)
                        Destroy(m_gos[i]);
                    m_gos.RemoveRange(0, m_gos.Count - maxSimultaneousObjects);
                }
            }

            ++m_frame;
        }
    }
}