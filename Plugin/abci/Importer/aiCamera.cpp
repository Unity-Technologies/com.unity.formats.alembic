#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiCamera.h"


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
    // Note: CameraSample::getFieldOfView() returns the horizontal field of view, we need the verical one
    DebugLog("aiCameraSample::getData()");
    static float sRad2Deg = 180.0f / float(M_PI);
    float focalLength = (float)m_sample.getFocalLength();
    float verticalAperture = (float)m_sample.getVerticalAperture();
    if (m_config.aspectRatio > 0.0f)
    {
        verticalAperture = (float)m_sample.getHorizontalAperture() / m_config.aspectRatio;
    }
    
    outData.nearClippingPlane = (float)m_sample.getNearClippingPlane();
    outData.farClippingPlane = (float)m_sample.getFarClippingPlane();
    outData.fieldOfView = 2.0f * atanf(verticalAperture * 10.0f / (2.0f * focalLength)) * sRad2Deg;

    outData.focusDistance = (float)m_sample.getFocusDistance();
    outData.focalLength = focalLength;
    outData.aperture = verticalAperture;
    outData.aspectRatio = float(m_sample.getHorizontalAperture() / verticalAperture);

    if (m_config.interpolateSamples && m_currentTimeOffset != 0)
    {
        float timeOffset = (float)m_currentTimeOffset;
        float focalLength2 = (float)m_nextSample.getFocalLength();
        float verticalAperture2 = (float)m_nextSample.getVerticalAperture();
        if (m_config.aspectRatio > 0.0f)
        {
            verticalAperture2 = (float)m_nextSample.getHorizontalAperture() / m_config.aspectRatio;
        }
        
        float fov2 = 2.0f * atanf(verticalAperture2 * 10.0f / (2.0f * focalLength2)) * sRad2Deg;
        outData.nearClippingPlane += timeOffset * (float)(m_nextSample.getNearClippingPlane() - m_sample.getNearClippingPlane());
        outData.farClippingPlane += timeOffset * (float)(m_nextSample.getFarClippingPlane() - m_sample.getFarClippingPlane());
        outData.fieldOfView += timeOffset * (fov2 - outData.fieldOfView);

        outData.focusDistance += timeOffset * (float)(m_nextSample.getFocusDistance() - m_sample.getFocusDistance());
        outData.focalLength += timeOffset * (focalLength2 - focalLength);
        outData.aperture += timeOffset * (verticalAperture2 - verticalAperture);
        outData.aspectRatio += timeOffset * (float(m_nextSample.getHorizontalAperture() / verticalAperture2) - outData.aspectRatio);
    }
    if (outData.nearClippingPlane==.0f)
    {
        outData.nearClippingPlane = 0.01f;
    }
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

bool aiCamera::updateInterpolatedValues(const Abc::ISampleSelector& ss, Sample& sample) const
{
    AbcCoreAbstract::chrono_t requestedTime = ss.getRequestedTime();
    Abc::ISampleSelector ssCeil = getSampleSelectorComplement(ss);
    bool dataChanged = requestedTime != sample.m_lastSampleTime;
    sample.m_lastSampleTime = requestedTime;
    if (dataChanged)
    {
        AbcCoreAbstract::chrono_t interval = m_schema.getTimeSampling()->getTimeSamplingType().getTimePerCycle();
        AbcCoreAbstract::chrono_t floor_offset = fmod(requestedTime, interval);
        sample.m_currentTimeOffset = floor_offset / interval;
        if (ss.getRequestedTimeIndexType() == Abc::ISampleSelector::TimeIndexType::kCeilIndex)
            sample.m_currentTimeOffset = 1.0 - sample.m_currentTimeOffset;
        m_schema.get(sample.m_nextSample, ssCeil);
    }
    return dataChanged;
}
