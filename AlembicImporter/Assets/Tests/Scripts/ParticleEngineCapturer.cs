using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.Formats.Alembic.Util;

namespace UTJ.Alembic
{

    [CaptureTarget(typeof(ParticleEngine))]
    internal class ParticleEngineCapturer : ComponentCapturer
    {
        ParticleEngine m_target;
        PinnedList<Vector3> m_points;
        PinnedList<Vector3> m_velocities;
        aePointsData m_data;

        public override void Setup(Component c)
        {
            m_target = c as ParticleEngine;
            abcObject = parent.abcObject.NewPoints(m_target.name, timeSamplingIndex);
        }

        public override void Capture()
        {
            if(m_target == null)
            {
                m_data.Visibility = false;
            }
            else
            {
                m_points = m_target.positionBuffer;
                m_velocities = m_target.velocityBuffer;

                m_data.Visibility = m_target.gameObject.activeSelf;
                m_data.Count = m_points.Count;
                m_data.Positions = m_points;
                m_data.Velocities = m_velocities;
            }
            abcObject.WriteSample(ref m_data);
        }
    }
}
