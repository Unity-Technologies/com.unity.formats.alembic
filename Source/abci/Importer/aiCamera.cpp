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

void aiCameraSample::getData(CameraData &dst) const
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

    const double lensSizeFactor =  10; // Lens size is in cm

    dst.visibility = sample.visibility;
    dst.focal_length = (float)sp.getFocalLength() ;
    dst.sensor_size[0] = (float)sp.getHorizontalAperture() * lensSizeFactor;
    dst.sensor_size[1] = (float)sp.getVerticalAperture() * lensSizeFactor ;
    dst.lens_shift[0] = (float)sp.getHorizontalFilmOffset();
    dst.lens_shift[1] = (float)sp.getVerticalFilmOffset();
    dst.near_far[0] = (float)sp.getNearClippingPlane() * config.scale_factor;
    dst.near_far[1] = (float)sp.getFarClippingPlane() * config.scale_factor;


    if (config.interpolate_samples && m_current_time_offset != 0) {
        auto& sp2 = sample.cam_sp2;
        float time_offset = (float)m_current_time_offset;

        dst.focal_length += time_offset * ((float)sp2.getFocalLength() - dst.focal_length);
        dst.sensor_size[0] += time_offset * (float)(sp2.getHorizontalAperture() * lensSizeFactor - dst.sensor_size[0]);
        dst.sensor_size[1] += time_offset * (float)(sp2.getVerticalAperture() * lensSizeFactor - dst.sensor_size[1]);
        dst.lens_shift[0] += time_offset * (float)(sp2.getHorizontalFilmOffset() - dst.lens_shift[0]);
        dst.lens_shift[1] += time_offset * (float)(sp2.getVerticalFilmOffset() - dst.lens_shift[1]);
        dst.near_far[0] += time_offset * (float)(sp2.getNearClippingPlane() * config.scale_factor - dst.near_far[0]);
        dst.near_far[1] += time_offset * (float)(sp2.getFarClippingPlane() * config.scale_factor - dst.near_far[1]);
    }

    if (dst.near_far[0] == 0.0f)
        dst.near_far[0] = 0.01f;
}
