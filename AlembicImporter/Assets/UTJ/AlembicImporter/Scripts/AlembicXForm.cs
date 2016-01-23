using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UTJ
{
    [ExecuteInEditMode]
    public class AlembicXForm : AlembicElement
    {
        AbcAPI.aiXFormData m_abcData;

        // No config overrides on AlembicXForm

        public override void AbcSampleUpdated(AbcAPI.aiSample sample, bool topologyChanged)
        {
            AbcAPI.aiXFormGetData(sample, ref m_abcData);

            AbcDirty();
        }

        public override void AbcUpdate()
        {
            if (AbcIsDirty())
            {
                if (m_abcData.inherits)
                {
                    m_trans.localPosition = m_abcData.translation;
                    m_trans.localRotation = m_abcData.rotation;
                    m_trans.localScale = m_abcData.scale;
                }
                else
                {
                    m_trans.position = m_abcData.translation;
                    m_trans.rotation = m_abcData.rotation;
                    m_trans.localScale = m_abcData.scale;
                }

                AbcClean();
            }
        }
    }
}
