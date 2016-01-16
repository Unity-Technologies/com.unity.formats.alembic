#include "pch.h"
#include "AlembicImporter.h"
#include "aiLogger.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiCamera.h"
#include "aiPolyMesh.h"
#include "aiXForm.h"


aiCameraSample::aiCameraSample(aiCamera *schema)
    : super(schema)
{
}

void aiCameraSample::updateConfig(const aiConfig &config, bool &topoChanged, bool &dataChanged)
{
    DebugLog("aiCameraSample::updateConfig()");
    
    topoChanged = false;
    dataChanged = (config.aspectRatio != m_config.aspectRatio);
    
    m_config = config;
}

void aiCameraSample::getData(aiCameraData &outData)
{
    DebugLog("aiCameraSample::getData()");
    
    // Note: CameraSample::getFieldOfView() returns the horizontal field of view, we need the verical one
    static float sRad2Deg = 180.0f / float(M_PI);

    float verticalAperture = (float) m_sample.getVerticalAperture();
    float focalLength = (float) m_sample.getFocalLength();

    if (m_config.aspectRatio > 0.0f)
    {
        verticalAperture = (float) m_sample.getHorizontalAperture() / m_config.aspectRatio;
    }

    outData.nearClippingPlane = (float) m_sample.getNearClippingPlane();
    outData.farClippingPlane = (float) m_sample.getFarClippingPlane();
    // CameraSample::getFieldOfView() returns the horizontal field of view, we need the verical one
    outData.fieldOfView = 2.0f * atanf(verticalAperture * 10.0f / (2.0f * focalLength)) * sRad2Deg;
    outData.focusDistance = (float) m_sample.getFocusDistance() * 0.1f; // centimeter to meter
    outData.focalLength = focalLength * 0.01f; // milimeter to meter
}


aiCamera::aiCamera(aiObject *obj)
    : super(obj)
{
}

aiCamera::Sample* aiCamera::newSample()
{
    Sample *sample = getSample();
    
    if (!sample)
    {
        sample = new Sample(this);
    }
    
    return sample;
}

aiCamera::Sample* aiCamera::readSample(const abcSampleSelector& ss, bool &topologyChanged)
{
    DebugLog("aiCamera::readSample(t=%f)", time);
    
    Sample *ret = newSample();
    
    m_schema.get(ret->m_sample, ss);

    topologyChanged = false;

    return ret;
}
