using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{

    [RequireComponent(typeof(ParticleEngine))]
    public class ParticleEngineCapturer : AlembicCustomComponentCapturer
    {
        public bool m_captureVelocities = true;
        aeObject m_abc;
        aePointsData m_data;

        public override void CreateAbcObject(aeObject parent)
        {
            m_abc = parent.NewPoints(gameObject.name, recorder.GetCurrentTimeSamplingIndex());
        }

        public override void Capture()
        {
            var target = GetComponent<ParticleEngine>();
            var positions = target.positionBuffer;
            if (positions == null) { return; }

            m_data.visibility = true;
            m_data.count = positions.Length;
            m_data.positions = Marshal.UnsafeAddrOfPinnedArrayElement(positions, 0);
            if(m_captureVelocities)
            {
                var velocities = target.velocityBuffer;
                if (velocities != null)
                {
                    m_data.velocities = Marshal.UnsafeAddrOfPinnedArrayElement(velocities, 0);
                }
            }
        }

        public override void WriteSample()
        {
            m_abc.WriteSample(ref m_data);
        }
    }
}
