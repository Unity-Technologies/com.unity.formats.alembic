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
    o_params.near_clipping_plane = m_sample.getNearClippingPlane();
    o_params.far_clipping_plane = m_sample.getFarClippingPlane();
    o_params.field_of_view = m_sample.getFieldOfView();
    o_params.focus_distance = m_sample.getFocusDistance() * 0.1f; // centimeter to meter
    o_params.focal_length = m_sample.getFocalLength() * 0.01f; // milimeter to meter
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
