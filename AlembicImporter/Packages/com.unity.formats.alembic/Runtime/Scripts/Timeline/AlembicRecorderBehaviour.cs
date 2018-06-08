using System;
using UnityEngine;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UTJ.Alembic
{
    public class AlembicRecorderBehaviour : PlayableBehaviour
    {
        #region fields
        AlembicRecorder m_recorder = new AlembicRecorder();
        bool m_ignoreFirstFrame = true;
        int m_prevFrame = 0;
        bool m_firstFrame;
        PlayState m_playState = PlayState.Paused;
        #endregion


        #region properties
        public AlembicRecorderSettings settings
        {
            get { return m_recorder.settings; }
            set { m_recorder.settings = value; }
        }
        public bool ignoreFirstFrame
        {
            get { return m_ignoreFirstFrame; }
            set { m_ignoreFirstFrame = value; }
        }

        #endregion


        #region impl
        void BeginRecording()
        {
            m_firstFrame = true;
            m_prevFrame = -1;
            
            if (m_recorder.BeginRecording())
            {
                var settings = m_recorder.settings;
                if (settings.conf.timeSamplingType == aeTimeSamplingType.Uniform && settings.fixDeltaTime)
                {
                    Time.maximumDeltaTime = (1.0f / settings.conf.frameRate);
                }
            }
        }

        void EndRecording()
        {
            m_recorder.EndRecording();
        }

        void ProcessRecording()
        {
            if (!m_recorder.recording || Time.frameCount == m_prevFrame || m_playState == PlayState.Paused)
                return;

            m_prevFrame = Time.frameCount;
            if (m_ignoreFirstFrame && m_firstFrame)
            {
                m_firstFrame = false;
                return;
            }

            m_recorder.ProcessRecording();
        }
        #endregion


        #region messsages

        public override void OnPlayableCreate(Playable playable)
        {
        }


        public override void OnPlayableDestroy(Playable playable)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#endif
            {
                EndRecording();
            }
        }

        public override void OnGraphStart(Playable playable)
        {
#if UNITY_EDITOR
            if(EditorApplication.isPlaying)
#endif
            {
                BeginRecording();
            }
        }

        public override void OnGraphStop(Playable playable)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#endif
            {
                EndRecording();
            }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#endif
            {
                AlembicWaitForEndOfFrame.Add(this);
            }
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            m_playState = PlayState.Playing;
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            m_playState = PlayState.Paused;
        }

        public void OnFrameEnd()
        {
            ProcessRecording();
        }
        #endregion
    }
}
