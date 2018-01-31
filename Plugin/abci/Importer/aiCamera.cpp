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
    dst = m_data;
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

void aiCamera::cookSample(Sample& sample)
{
    auto& config = getConfig();
    auto& cs = sample.m_sample;
    auto& cs_next = sample.m_next_sample;
    auto& dst = sample.m_data;

    // Note: CameraSample::getFieldOfView() returns the horizontal field of view, we need the verical one
    static float sRad2Deg = 180.0f / float(M_PI);
    float focal_length = (float)cs.getFocalLength();
    float vertical_aperture = (float)cs.getVerticalAperture();
    if (config.aspect_ratio > 0.0f)
        vertical_aperture = (float)cs.getHorizontalAperture() / config.aspect_ratio;

    dst.near_clipping_plane = (float)cs.getNearClippingPlane();
    dst.far_clipping_plane = (float)cs.getFarClippingPlane();
    dst.field_of_view = 2.0f * atanf(vertical_aperture * 10.0f / (2.0f * focal_length)) * sRad2Deg;

    dst.focus_distance = (float)cs.getFocusDistance();
    dst.focal_length = focal_length;
    dst.aperture = vertical_aperture;
    dst.aspect_ratio = float(cs.getHorizontalAperture() / vertical_aperture);

    if (config.interpolate_samples && m_current_time_offset != 0) {
        float time_offset = (float)m_current_time_offset;
        float focal_length2 = (float)cs_next.getFocalLength();
        float vertical_aperture2 = (float)cs_next.getVerticalAperture();
        if (config.aspect_ratio > 0.0f)
            vertical_aperture2 = (float)cs_next.getHorizontalAperture() / config.aspect_ratio;

        float fov2 = 2.0f * atanf(vertical_aperture2 * 10.0f / (2.0f * focal_length2)) * sRad2Deg;
        dst.near_clipping_plane += time_offset * (float)(cs_next.getNearClippingPlane() - cs.getNearClippingPlane());
        dst.far_clipping_plane += time_offset * (float)(cs_next.getFarClippingPlane() - cs.getFarClippingPlane());
        dst.field_of_view += time_offset * (fov2 - dst.field_of_view);

        dst.focus_distance += time_offset * (float)(cs_next.getFocusDistance() - cs.getFocusDistance());
        dst.focal_length += time_offset * (focal_length2 - focal_length);
        dst.aperture += time_offset * (vertical_aperture2 - vertical_aperture);
        dst.aspect_ratio += time_offset * (float(cs_next.getHorizontalAperture() / vertical_aperture2) - dst.aspect_ratio);
    }

    if (dst.near_clipping_plane == 0.0f)
        dst.near_clipping_plane = 0.01f;
}
