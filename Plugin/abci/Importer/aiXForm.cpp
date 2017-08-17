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
    abcV4 rotFinal;
    Imath::V3d trans;
    decomposeXForm(m_sample.getMatrix(), scale, shear, rot, trans);
    if (m_config.swapHandedness)
    {        trans.x *= -1.0f;
        rotFinal = abcV4(-rot.v[0], rot.v[1], rot.v[2], -rot.r);
    }
    else
    {
        rotFinal = abcV4(rot.v[0], rot.v[1], rot.v[2], rot.r);
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

