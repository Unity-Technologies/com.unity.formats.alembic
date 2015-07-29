#include "pch.h"
#include "AlembicImporter.h"
#include "aiLogger.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiCamera.h"
#include "aiPolyMesh.h"
#include "aiXForm.h"


aiXFormSample::aiXFormSample(aiXForm *schema, float time)
    : super(schema, time)
{
}

void aiXFormSample::updateConfig(const aiConfig &config, bool &topoChanged, bool &dataChanged)
{
    DebugLog("aiXFormSample::updateConfig()");
    
    topoChanged = false;
    dataChanged = (config.swapHandedness != m_config.swapHandedness);
    m_config = config;
}

void aiXFormSample::getData(aiXFormData &outData) const
{
    DebugLog("aiXFormSample::getData()");
    
    abcV3 trans = m_sample.getTranslation();
    abcV3 axis = m_sample.getAxis();
    float angle = m_sample.getAngle() * (aiPI / 180.0f);

    if (m_config.swapHandedness)
    {
        trans.x *= -1.0f;
        axis.x *= -1.0f;
        angle *= -1.0f;
    }

    outData.inherits = m_sample.getInheritsXforms();
    outData.translation = trans;
    outData.scale = abcV3(m_sample.getScale());
    outData.rotation = abcV4(axis.x * std::sin(angle * 0.5f),
                             axis.y * std::sin(angle * 0.5f),
                             axis.z * std::sin(angle * 0.5f),
                             std::cos(angle * 0.5f) );

}



aiXForm::aiXForm(aiObject *obj)
    : super(obj)
{
}

aiXForm::Sample* aiXForm::readSample(float time, bool &topologyChanged)
{
    DebugLog("aiXForm::readSample(t=%f)", time);
    
    Sample *ret = new Sample(this, time);

    m_schema.get(ret->m_sample, MakeSampleSelector(time));

    topologyChanged = false;

    return ret;
}

