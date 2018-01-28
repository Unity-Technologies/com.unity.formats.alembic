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

void aiCameraSample::updateConfig(const aiConfig &config, bool &data_changed)
{
    data_changed = (config.aspect_ratio != m_config.aspect_ratio);
    m_config = config;
}

void aiCameraSample::getData(aiCameraData &dst) const
{
    // Note: CameraSample::getFieldOfView() returns the horizontal field of view, we need the verical one
    DebugLog("aiCameraSample::getData()");
    static float sRad2Deg = 180.0f / float(M_PI);
    float focalLength = (float)m_sample.getFocalLength();
    float verticalAperture = (float)m_sample.getVerticalAperture();
    if (m_config.aspect_ratio > 0.0f)
    {
        verticalAperture = (float)m_sample.getHorizontalAperture() / m_config.aspect_ratio;
    }
    
    dst.near_clipping_plane = (float)m_sample.getNearClippingPlane();
    dst.far_clipping_plane = (float)m_sample.getFarClippingPlane();
    dst.field_of_view = 2.0f * atanf(verticalAperture * 10.0f / (2.0f * focalLength)) * sRad2Deg;

    dst.focus_distance = (float)m_sample.getFocusDistance();
    dst.focal_length = focalLength;
    dst.aperture = verticalAperture;
    dst.aspect_ratio = float(m_sample.getHorizontalAperture() / verticalAperture);

    if (m_config.interpolate_samples && m_current_time_offset != 0)
    {
        float timeOffset = (float)m_current_time_offset;
        float focalLength2 = (float)m_next_sample.getFocalLength();
        float verticalAperture2 = (float)m_next_sample.getVerticalAperture();
        if (m_config.aspect_ratio > 0.0f)
        {
            verticalAperture2 = (float)m_next_sample.getHorizontalAperture() / m_config.aspect_ratio;
        }
        
        float fov2 = 2.0f * atanf(verticalAperture2 * 10.0f / (2.0f * focalLength2)) * sRad2Deg;
        dst.near_clipping_plane += timeOffset * (float)(m_next_sample.getNearClippingPlane() - m_sample.getNearClippingPlane());
        dst.far_clipping_plane += timeOffset * (float)(m_next_sample.getFarClippingPlane() - m_sample.getFarClippingPlane());
        dst.field_of_view += timeOffset * (fov2 - dst.field_of_view);

        dst.focus_distance += timeOffset * (float)(m_next_sample.getFocusDistance() - m_sample.getFocusDistance());
        dst.focal_length += timeOffset * (focalLength2 - focalLength);
        dst.aperture += timeOffset * (verticalAperture2 - verticalAperture);
        dst.aspect_ratio += timeOffset * (float(m_next_sample.getHorizontalAperture() / verticalAperture2) - dst.aspect_ratio);
    }
    if (dst.near_clipping_plane==.0f)
    {
        dst.near_clipping_plane = 0.01f;
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

aiCamera::Sample* aiCamera::readSample(const uint64_t idx)
{
    auto ss = aiIndexToSampleSelector(idx);
    auto ss2 = aiIndexToSampleSelector(idx + 1);
    DebugLog("aiCamera::readSample(t=%d)", idx);
    
    Sample *ret = newSample();
    
    m_schema.get(ret->m_sample, ss);
    m_schema.get(ret->m_next_sample, ss2);

    return ret;
}