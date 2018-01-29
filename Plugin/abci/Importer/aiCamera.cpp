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

void aiCameraSample::getData(aiCameraData &dst) const
{
    auto& config = getConfig();

    // Note: CameraSample::getFieldOfView() returns the horizontal field of view, we need the verical one
    DebugLog("aiCameraSample::getData()");
    static float sRad2Deg = 180.0f / float(M_PI);
    float focal_length = (float)m_sample.getFocalLength();
    float vertical_aperture = (float)m_sample.getVerticalAperture();
    if (config.aspect_ratio > 0.0f)
        vertical_aperture = (float)m_sample.getHorizontalAperture() / config.aspect_ratio;
    
    dst.near_clipping_plane = (float)m_sample.getNearClippingPlane();
    dst.far_clipping_plane = (float)m_sample.getFarClippingPlane();
    dst.field_of_view = 2.0f * atanf(vertical_aperture * 10.0f / (2.0f * focal_length)) * sRad2Deg;

    dst.focus_distance = (float)m_sample.getFocusDistance();
    dst.focal_length = focal_length;
    dst.aperture = vertical_aperture;
    dst.aspect_ratio = float(m_sample.getHorizontalAperture() / vertical_aperture);

    if (config.interpolate_samples && m_current_time_offset != 0) {
        float timeOffset = (float)m_current_time_offset;
        float focalLength2 = (float)m_next_sample.getFocalLength();
        float verticalAperture2 = (float)m_next_sample.getVerticalAperture();
        if (config.aspect_ratio > 0.0f)
            verticalAperture2 = (float)m_next_sample.getHorizontalAperture() / config.aspect_ratio;

        float fov2 = 2.0f * atanf(verticalAperture2 * 10.0f / (2.0f * focalLength2)) * sRad2Deg;
        dst.near_clipping_plane += timeOffset * (float)(m_next_sample.getNearClippingPlane() - m_sample.getNearClippingPlane());
        dst.far_clipping_plane += timeOffset * (float)(m_next_sample.getFarClippingPlane() - m_sample.getFarClippingPlane());
        dst.field_of_view += timeOffset * (fov2 - dst.field_of_view);

        dst.focus_distance += timeOffset * (float)(m_next_sample.getFocusDistance() - m_sample.getFocusDistance());
        dst.focal_length += timeOffset * (focalLength2 - focal_length);
        dst.aperture += timeOffset * (verticalAperture2 - vertical_aperture);
        dst.aspect_ratio += timeOffset * (float(m_next_sample.getHorizontalAperture() / verticalAperture2) - dst.aspect_ratio);
    }

    if (dst.near_clipping_plane == 0.0f)
        dst.near_clipping_plane = 0.01f;
}


aiCamera::aiCamera(aiObject *obj)
    : super(obj)
{
}

aiCamera::Sample* aiCamera::newSample()
{
    return new Sample(this);
}

void aiCamera::readSample(Sample& sample, uint64_t idx)
{
    auto ss = aiIndexToSampleSelector(idx);
    auto ss2 = aiIndexToSampleSelector(idx + 1);

    m_schema.get(sample.m_sample, ss);
    m_schema.get(sample.m_next_sample, ss2);
}