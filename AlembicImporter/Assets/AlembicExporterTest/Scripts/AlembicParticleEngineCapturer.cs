using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlembicExporterTest
{

    [RequireComponent(typeof(ParticleEngine))]
    public class AlembicParticleEngineCapturer : AlembicCustomComponentCapturer
    {
        aeAPI.aeObject m_abc;

        public override void CreateAbcObject(aeAPI.aeObject parent)
        {
            m_abc = aeAPI.aeNewPoints(parent, gameObject.name);
        }

        public override void Capture()
        {
            var target = GetComponent<ParticleEngine>();
            var positions = target.GetPositionBuffer();
            if(positions == null) { return; }

            var data = new aeAPI.aePointsSampleData();
            data.positions = Marshal.UnsafeAddrOfPinnedArrayElement(positions, 0);
            data.count = positions.Length;
            aeAPI.aePointsWriteSample(m_abc, ref data);
        }

    }
}
