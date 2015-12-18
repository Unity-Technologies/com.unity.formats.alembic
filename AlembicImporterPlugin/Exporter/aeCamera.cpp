#include "pch.h"
#include "AlembicExporter.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aeCamera.h"


aeCamera::aeCamera(aeObject *parent, const char *name)
    : super(parent->getContext(), parent, new abcCamera(parent->getAbcObject(), name, parent->getContext()->getTimeSaplingIndex()))
    , m_schema(getAbcObject().getSchema())
{
}

abcCamera& aeCamera::getAbcObject()
{
    return dynamic_cast<abcCamera&>(*m_abc);
}

abcProperties* aeCamera::getAbcProperties()
{
    return &m_schema;
}

void aeCamera::writeSample(const aeCameraSampleData &data)
{
    // todo: compute aperture & focal length
    //static const float sRad2Deg = 180.0f / float(M_PI);
    //static const float sDeg2Rad = float(M_PI) / 180.0f;
    //2.0f * atanf(verticalAperture * 10.0f / (2.0f * focalLength)) * sRad2Deg;
    //1.0f / tan(data.fieldOfView * 0.5f * sDeg2Rad);

    AbcGeom::CameraSample sample;
    sample.setNearClippingPlane(data.nearClippingPlane);
    sample.setFarClippingPlane(data.farClippingPlane);

    m_schema.set(sample);
}
