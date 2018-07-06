using UnityEngine;
using UnityEngine.Formats.Alembic.Exporter;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.UI;

namespace UTJ.Alembic
{
    [ExecuteInEditMode]
    public class AlembicExporterGUI : MonoBehaviour
    {
        public Image m_background;
        public Color m_color_default;
        public Color m_color_capturering;
        public Button m_button_toggle_capture;
        public Button m_button_one_shot;
        public Dropdown m_dropdown_simesampling;
        public InputField m_input_fps;
        public GameObject m_fps_control;

        bool m_recording;
        AlembicExporter[] m_exporters;


        public void ToggleCapture()
        {
            if(!m_recording)
            {
                BeginCapture();
            }
            else
            {
                EndCapture();
            }
        }

        public void BeginCapture()
        {
            if(m_recording) { return; }

            // in the enum acyclic is 2, and uniform is 1
            var time_sampling_type = (aeTimeSamplingType)(m_dropdown_simesampling.value == 1? 2 : 0);
            var frame_rate = int.Parse(m_input_fps.text);

            m_exporters = FindObjectsOfType<AlembicExporter>();
            foreach(var e in m_exporters)
            {
                e.maxCaptureFrame = 0;
                var settings = e.recorder.settings;
                settings.conf.TimeSamplingType = time_sampling_type;
                settings.conf.FrameRate = frame_rate;
                e.BeginRecording();
            }

            m_recording = true;
            m_background.color = m_color_capturering;
            m_button_one_shot.gameObject.SetActive(false);
            m_button_toggle_capture.GetComponentInChildren<Text>().text = "End Capture";
        }

        public void EndCapture()
        {
            if(!m_recording) { return; }

            foreach (var e in m_exporters)
            {
                e.EndRecording();
            }

            m_recording = false;
            m_background.color = m_color_default;
            m_button_one_shot.gameObject.SetActive(true);
            m_button_toggle_capture.GetComponentInChildren<Text>().text = "Begin Capture";
        }

        public void OneShot()
        {
            m_exporters = FindObjectsOfType<AlembicExporter>();
            foreach (var e in m_exporters)
            {
                e.OneShot();
            }
        }

        public void OnChangeTimeSampling()
        {
            m_fps_control.SetActive(m_dropdown_simesampling.value == 0);
        }
    }
}