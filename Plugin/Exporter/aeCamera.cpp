#include "pch.h"
#include "AlembicExporter.h"
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

void aeCamera::writeSample(const aeCameraData &data_)
{
    auto data = data_;

    const float Rad2Deg = 180.0f / float(M_PI);
    const float Deg2Rad = float(M_PI) / 180;

    if (data.focalLength == 0.0f) {
        // compute focalLength by fieldOfView and aperture
        // deformation:
        //  fieldOfView = atan((aperture * 10.0f) / (2.0f * focalLength)) * Rad2Deg * 2.0f;
        //  fieldOfView * Deg2Rad / 2.0f = atan((aperture * 10.0f) / (2.0f * focalLength));
        //  tan(fieldOfView * Deg2Rad / 2.0f) = (aperture * 10.0f) / (2.0f * focalLength);
        //  tan(fieldOfView * Deg2Rad / 2.0f) * (2.0f * focalLength) = (aperture * 10.0f);
        //  (2.0f * focalLength) = (aperture * 10.0f) / tan(fieldOfView * Deg2Rad / 2.0f);
        //  focalLength = (aperture * 10.0f) / tan(fieldOfView * Deg2Rad / 2.0f) / 2.0f;

        data.focalLength = (data.aperture * 10.0f) / std::tan(data.fieldOfView * Deg2Rad / 2.0f) / 2.0f;
    }

    AbcGeom::CameraSample sample;
    sample.setNearClippingPlane(data.nearClippingPlane);
    sample.setFarClippingPlane(data.farClippingPlane);
    sample.setFocalLength(data.focalLength);
    sample.setFocusDistance(data.focusDistance);
    sample.setVerticalAperture(data.aperture);
    sample.setHorizontalAperture(data.aperture * data.aspectRatio);
    m_schema.set(sample);
}
