using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{

    [CaptureTarget(typeof(ParticleEngine))]
    public class ParticleEngineCapturer : ComponentCapturer
    {
        ParticleEngine m_target;
        aePointsData m_data;

        public override void Setup(AlembicRecorder rec, ComponentCapturer p, Component c)
        {
            base.Setup(rec, p, c);
            m_target = c as ParticleEngine;
            m_abc = parent.abcObject.NewPoints(gameObject.name, rec.GetCurrentTimeSamplingIndex());
        }

        public override void Capture()
        {
            var positions = m_target.positionBuffer;
            if (positions == null) { return; }

            m_data.visibility = true;
            m_data.count = positions.Count;
            m_data.positions = positions;
            m_data.velocities = m_target.velocityBuffer;
            m_abc.WriteSample(ref m_data);
        }
    }
}
