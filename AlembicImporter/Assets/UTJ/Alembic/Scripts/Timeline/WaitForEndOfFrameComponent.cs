using System;
using System.Collections;
using UnityEngine;

namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    class WaitForEndOfFrameComponent : MonoBehaviour
    {
        [NonSerialized]
        public AlembicRecorderPlayableBehaviour m_playable;

        public IEnumerator WaitForEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            if(m_playable != null)
                m_playable.OnFrameEnd();
        }

        public void LateUpdate()
        {
            StartCoroutine(WaitForEndOfFrame());
        }
    }
}
