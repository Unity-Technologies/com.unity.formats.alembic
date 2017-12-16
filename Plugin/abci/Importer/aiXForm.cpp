#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiXForm.h"


aiXFormSample::aiXFormSample(aiXForm *schema)
    : super(schema), inherits(true)
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
    decomposeXForm(m_matrix, scale, shear, rot, trans);

    if (m_config.interpolateSamples && m_currentTimeOffset!=0)
    {
        Imath::V3d scale2;
        Imath::Quatd rot2;
        Imath::V3d trans2;
        decomposeXForm(m_nextMatrix, scale2, shear, rot2, trans2);
        scale += (scale2 - scale)* m_currentTimeOffset;
        trans += (trans2 - trans)* m_currentTimeOffset;
        rot = slerpShortestArc(rot, rot2, m_currentTimeOffset);
    }

    auto rotFinal = abcV4(
        static_cast<float>(rot.v[0]),
        static_cast<float>(rot.v[1]),
        static_cast<float>(rot.v[2]),
        static_cast<float>(rot.r)
    );

    if (m_config.swapHandedness)
    {
        trans.x *= -1.0f;
        rotFinal.x = -rotFinal.x;
        rotFinal.w = -rotFinal.w;
    }
    outData.inherits = inherits;
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

aiXForm::Sample* aiXForm::readSample(const uint64_t idx, bool &topologyChanged)
{
    DebugLog("aiXForm::readSample(t=%d)", idx);
    Sample *ret = newSample();

    auto ss = aiIndexToSampleSelector(idx);
    AbcGeom::XformSample matSample;
    m_schema.get(matSample, ss);
    ret->m_matrix = matSample.getMatrix();
    
    ret->inherits = matSample.getInheritsXforms();

    auto ss2 = aiIndexToSampleSelector(idx + 1);
    AbcGeom::XformSample nextMatSample;
    m_schema.get(nextMatSample, ss2 );
    ret->m_nextMatrix = nextMatSample.getMatrix();
    
    return ret;
}