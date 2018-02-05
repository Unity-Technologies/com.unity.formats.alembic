using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{

    [RequireComponent(typeof(ParticleEngine))]
    public class ParticleEngineCapturer : AlembicCustomComponentCapturer
    {
        public bool m_captureVelocities = true;
        aeObject m_abc;

        public override void CreateAbcObject(aeObject parent)
        {
            m_abc = parent.NewPoints(gameObject.name);
        }

        public override void Capture()
        {
            var target = GetComponent<ParticleEngine>();
            var positions = target.positionBuffer;
            if (positions == null) { return; }

            var data = default(aePointsData);
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
            m_abc.WriteSample(ref data);
        }

    }
}
