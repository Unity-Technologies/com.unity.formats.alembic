#include "pch.h"
#include "aeInternal.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aeCamera.h"


aeCamera::aeCamera(aeObject *parent, const char *name, uint32_t tsi)
    : super(parent->getContext(), parent, new abcCamera(parent->getAbcObject(), name, tsi), tsi)
    , m_schema(getAbcObject().getSchema())
{
}

abcCamera& aeCamera::getAbcObject()
{
    return dynamic_cast<abcCamera&>(*m_abc);
}

abcProperties aeCamera::getAbcProperties()
{
    return m_schema.getUserProperties();
}

size_t aeCamera::getNumSamples()
{
    return m_schema.getNumSamples();
}

void aeCamera::setFromPrevious()
{
    m_schema.setFromPrevious();
}

void aeCamera::writeSample(const CameraData &data)
{
    m_data_local = data;
    m_ctx->addAsync([this]() { writeSampleBody(); });
}

void aeCamera::writeSampleBody()
{
    auto& data = m_data_local;
    writeVisibility(data.visibility);

   AbcGeom::CameraSample sample;
   sample.setFocalLength(data.focal_length);
   sample.setHorizontalAperture(data.sensor_size[0]);
   sample.setVerticalAperture(data.sensor_size[1]);
   sample.setHorizontalFilmOffset(data.sensor_size[0]);
   sample.setVerticalFilmOffset(data.sensor_size[1]);
   sample.setNearClippingPlane(data.near_far[0]);
   sample.setFarClippingPlane(data.near_far[1]);

    m_schema.set(sample);
}
