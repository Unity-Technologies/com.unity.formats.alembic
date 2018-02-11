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
    dst = data;
}


aiCamera::aiCamera(aiObject *parent, const abcObject &abc)
    : super(parent, abc)
{
}

aiCamera::Sample* aiCamera::newSample()
{
    return new Sample(this);
}

void aiCamera::readSampleBody(Sample& sample, uint64_t idx)
{
    auto ss = aiIndexToSampleSelector(idx);
    auto ss2 = aiIndexToSampleSelector(idx + 1);

    readVisibility(sample, ss);
    m_schema.get(sample.cam_sp, ss);
    m_schema.get(sample.cam_sp2, ss2);
}

void aiCamera::cookSampleBody(Sample& sample)
{
    auto& config = getConfig();
    auto& sp = sample.cam_sp;
    auto& dst = sample.data;

    dst.visibility = sample.visibility;

    // Note: CameraSample::getFieldOfView() returns the horizontal field of view, we need the verical one
    static float sRad2Deg = 180.0f / float(M_PI);
    float focal_length = (float)sp.getFocalLength();
    float vertical_aperture = (float)sp.getVerticalAperture();
    if (config.aspect_ratio > 0.0f)
        vertical_aperture = (float)sp.getHorizontalAperture() / config.aspect_ratio;

    dst.near_clipping_plane = (float)sp.getNearClippingPlane();
    dst.far_clipping_plane = (float)sp.getFarClippingPlane();
    dst.field_of_view = 2.0f * atanf(vertical_aperture * 10.0f / (2.0f * focal_length)) * sRad2Deg;

    dst.focus_distance = (float)sp.getFocusDistance();
    dst.focal_length = focal_length;
    dst.aperture = vertical_aperture;
    dst.aspect_ratio = float(sp.getHorizontalAperture() / vertical_aperture);

    if (config.interpolate_samples && m_current_time_offset != 0) {
        auto& sp2 = sample.cam_sp2;
        float time_offset = (float)m_current_time_offset;
        float focal_length2 = (float)sp2.getFocalLength();
        float vertical_aperture2 = (float)sp2.getVerticalAperture();
        if (config.aspect_ratio > 0.0f)
            vertical_aperture2 = (float)sp2.getHorizontalAperture() / config.aspect_ratio;

        float fov2 = 2.0f * atanf(vertical_aperture2 * 10.0f / (2.0f * focal_length2)) * sRad2Deg;
        dst.near_clipping_plane += time_offset * (float)(sp2.getNearClippingPlane() - sp.getNearClippingPlane());
        dst.far_clipping_plane += time_offset * (float)(sp2.getFarClippingPlane() - sp.getFarClippingPlane());
        dst.field_of_view += time_offset * (fov2 - dst.field_of_view);

        dst.focus_distance += time_offset * (float)(sp2.getFocusDistance() - sp.getFocusDistance());
        dst.focal_length += time_offset * (focal_length2 - focal_length);
        dst.aperture += time_offset * (vertical_aperture2 - vertical_aperture);
        dst.aspect_ratio += time_offset * (float(sp2.getHorizontalAperture() / vertical_aperture2) - dst.aspect_ratio);
    }

    if (dst.near_clipping_plane == 0.0f)
        dst.near_clipping_plane = 0.01f;
}
