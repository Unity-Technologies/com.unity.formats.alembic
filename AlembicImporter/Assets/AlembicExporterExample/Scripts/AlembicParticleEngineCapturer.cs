using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlembicExporterExample
{

    [RequireComponent(typeof(ParticleEngine))]
    public class AlembicParticleEngineCapturer : AlembicCustomComponentCapturer
    {
        public bool m_captureVelocities = true;
        aeAPI.aeObject m_abc;

        public override void CreateAbcObject(aeAPI.aeObject parent)
        {
            m_abc = aeAPI.aeNewPoints(parent, gameObject.name);
        }

        public override void Capture()
        {
            var target = GetComponent<ParticleEngine>();
            var positions = target.positionBuffer;
            if (positions == null) { return; }

            var data = new aeAPI.aePointsSampleData();
            data.count = positions.Length;
            data.positions = Marshal.UnsafeAddrOfPinnedArrayElement(positions, 0);
            if(m_captureVelocities)
            {
                var velocities = target.velocityBuffer;
                if (velocities != null)
                {
                    data.velocities = Marshal.UnsafeAddrOfPinnedArrayElement(velocities, 0);
                }
            }
            aeAPI.aePointsWriteSample(m_abc, ref data);
        }

    }
}
