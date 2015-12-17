#include "pch.h"
#include "AlembicExporter.h"
#include "aeObject.h"
#include "aeCamera.h"

aeCamera::aeCamera(aeObject *obj)
    : super(obj)
    , m_abcobj(obj->getAbcObject(), "Camera")
    , m_schema(m_abcobj.getSchema())
{
}

void aeCamera::writeSample(const aeCameraSampleData &data)
{
    // todo: compute aperture & focal length
    //static const float sRad2Deg = 180.0f / float(M_PI);
    //static const float sDeg2Rad = float(M_PI) / 180.0f;
    //2.0f * atanf(verticalAperture * 10.0f / (2.0f * focalLength)) * sRad2Deg;
    //1.0f / tan(data.fieldOfView * 0.5f * sDeg2Rad);

    m_sample.setNearClippingPlane(data.nearClippingPlane);
    m_sample.setFarClippingPlane(data.farClippingPlane);

    m_schema.set(m_sample);
    m_sample.reset();
}
