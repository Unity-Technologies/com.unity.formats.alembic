using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlembicExporterExample
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

            var time_sampling_type = (AbcAPI.aeTypeSamplingType)m_dropdown_simesampling.value;
            var frame_rate = int.Parse(m_input_fps.text);

            m_exporters = FindObjectsOfType<AlembicExporter>();
            foreach(var e in m_exporters)
            {
                e.m_maxCaptureFrame = 0;
                e.m_conf.timeSamplingType = time_sampling_type;
                e.m_conf.frameRate = frame_rate;
                e.BeginCapture();
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
                e.EndCapture();
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
