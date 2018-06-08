using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    class AlembicWaitForEndOfFrame: MonoBehaviour
    {
        List<AlembicRecorderBehaviour> m_playables = new List<AlembicRecorderBehaviour>();

        static AlembicWaitForEndOfFrame s_instance;
        static AlembicWaitForEndOfFrame GetInstance()
        {
            if (s_instance == null)
            {
                s_instance =  GameObject.FindObjectOfType<AlembicWaitForEndOfFrame>();
                if (s_instance == null)
                {
                    var go = new GameObject();
                    go.name = "AlembicRecorderHelper";
                    s_instance = go.AddComponent<AlembicWaitForEndOfFrame>();
                }
            }
            return s_instance;
        }

        public static void Add(AlembicRecorderBehaviour v)
        {
            GetInstance().m_playables.Add(v);
        }


        IEnumerator WaitForEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            foreach (var e in m_playables)
                e.OnFrameEnd();
            m_playables.Clear();
        }

        void LateUpdate()
        {
            StartCoroutine(WaitForEndOfFrame());
        }
    }
}
