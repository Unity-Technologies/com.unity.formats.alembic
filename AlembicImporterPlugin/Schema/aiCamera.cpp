#include "pch.h"
#include "AlembicImporter.h"
#include "aiSchema.h"
#include "aiObject.h"
#include "aiContext.h"
#include "aiCamera.h"



aiCameraSample::aiCameraSample(aiCamera *schema, float time)
    : super(schema, time)
{
}

void aiCameraSample::getParams(aiCameraData &o_params)
{
    // Note: CameraSample::getFieldOfView() returns the horizontal field of view, we need the verical one
    static float sRad2Deg = 180.0f / float(M_PI);

    float vertical_aperture = (float) m_sample.getVerticalAperture();
    float focal_length = (float) m_sample.getFocalLength();

    o_params.near_clipping_plane = (float) m_sample.getNearClippingPlane();
    o_params.far_clipping_plane = (float) m_sample.getFarClippingPlane();
    o_params.field_of_view = 2.0f * atanf(vertical_aperture * 10.0f / (2.0f * focal_length)) * sRad2Deg;
    o_params.focus_distance = (float) m_sample.getFocusDistance() * 0.1f; // centimeter to meter
    o_params.focal_length = focal_length * 0.01f; // milimeter to meter
}


aiCamera::aiCamera(aiObject *obj)
    : super(obj)
{
    AbcGeom::ICamera cam(obj->getAbcObject(), Abc::kWrapExisting);
    m_schema = cam.getSchema();
}

aiCamera::Sample* aiCamera::readSample(float time)
{
    Sample *ret = new Sample(this, time);
    m_schema.get(ret->m_sample, makeSampleSelector(time));
    return ret;
}

void aiCamera::debugDump() const
{
}
