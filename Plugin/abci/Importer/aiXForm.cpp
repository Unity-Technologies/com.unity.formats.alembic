#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiXForm.h"


aiXFormSample::aiXFormSample(aiXForm *schema)
    : super(schema)
{
}

void aiXFormSample::updateConfig(const aiConfig &config, bool &topoChanged, bool &dataChanged)
{
    DebugLog("aiXFormSample::updateConfig()");
    
    topoChanged = false;
    dataChanged = (config.swapHandedness != m_config.swapHandedness);
    m_config = config;
}

void aiXFormSample::decomposeXForm(const Imath::M44d &mat,Imath::V3d &scale,Imath::V3d &shear,Imath::Quatd &rotation,Imath::V3d &translation) const
{
    Imath::M44d mat_remainder(mat);

    // Extract Scale, Shear
    Imath::extractAndRemoveScalingAndShear(mat_remainder, scale, shear);

    // Extract translation
    translation.x = mat_remainder[3][0];
    translation.y = mat_remainder[3][1];
    translation.z = mat_remainder[3][2];

    // Extract rotation
    rotation = extractQuat(mat_remainder);
}

void aiXFormSample::getData(aiXFormData &outData) const
{
    DebugLog("aiXFormSample::getData()");

    Imath::V3d scale;
    Imath::V3d shear;
    Imath::Quatd rot;
    Imath::V3d trans;
    decomposeXForm(m_sample.getMatrix(), scale, shear, rot, trans);

    if (m_config.interpolateSamples && m_currentTimeOffset!=0)
    {
        Imath::V3d scale2;
        Imath::Quatd rot2;
        Imath::V3d trans2;
        decomposeXForm(m_nextSample.getMatrix(), scale2, shear, rot2, trans2);
        scale += (scale2 - scale)* m_currentTimeOffset;
        trans += (trans2 - trans)* m_currentTimeOffset;
        rot = slerpShortestArc(rot, rot2, m_currentTimeOffset);
    }

    abcV4 rotFinal = abcV4(rot.v[0], rot.v[1], rot.v[2], rot.r);

    if (m_config.swapHandedness)
    {        trans.x *= -1.0f;
        rotFinal.x = -rotFinal.x;
        rotFinal.w = -rotFinal.w;
    }

    outData.inherits = m_sample.getInheritsXforms();
    outData.translation = trans;
    outData.rotation = rotFinal;
    outData.scale = scale;
}

aiXForm::aiXForm(aiObject *obj)
    : super(obj)
{
}

aiXForm::Sample* aiXForm::newSample()
{
    Sample *sample = getSample();
    
    if (!sample)
    {
        sample = new Sample(this);
    }
    
    return sample;
}

aiXForm::Sample* aiXForm::readSample(const abcSampleSelector& ss, bool &topologyChanged)
{
    DebugLog("aiXForm::readSample(t=%f)", time);
    
    Sample *ret = newSample();

    m_schema.get(ret->m_sample, ss);

    topologyChanged = false;

    return ret;
}

bool aiXForm::updateInterpolatedValues(const AbcCoreAbstract::chrono_t requestedTime, Sample& sample) const
{
    Abc::ISampleSelector ssCeil = abcSampleSelector(requestedTime, Abc::ISampleSelector::kCeilIndex);
    bool dataChanged = requestedTime != sample.m_lastSampleTime;
    sample.m_lastSampleTime = requestedTime;
    if (dataChanged)
    {
        AbcCoreAbstract::chrono_t interval = m_schema.getTimeSampling()->getTimeSamplingType().getTimePerCycle();
        AbcCoreAbstract::chrono_t floor_offset = fmod(requestedTime, interval);
        sample.m_currentTimeOffset = floor_offset / interval;
        m_schema.get(sample.m_nextSample, ssCeil);
    }
    return dataChanged;
}

