#include "pch.h"
#include "AlembicImporter.h"
#include "aiSchema.h"
#include "aiCamera.h"
#include "aiPolyMesh.h"
#include "aiXForm.h"
#include "aiObject.h"
#include "aiContext.h"

aiCamera::aiCamera()
    : super()
{
}

aiCamera::aiCamera(aiObject *obj)
    : super(obj)
{
}

aiCamera::~aiCamera()
{
}

void aiCamera::updateSample()
{
    Abc::ISampleSelector ss(m_obj->getCurrentTime());

    if (!m_schema.valid())
    {
        AbcGeom::ICamera cam(m_obj->getAbcObject(), Abc::kWrapExisting);
        m_schema = cam.getSchema();
    }
    else if (m_schema.isConstant())
    {
        return;
    }

    m_schema.get(m_sample, ss);
}

void aiCamera::getParams(aiCameraParams &params)
{
    static float sRad2Deg = 180.0f / float(M_PI);

    float verticalAperture = (float) m_sample.getVerticalAperture();
    float focalLength = (float) m_sample.getFocalLength();

    if (params.targetAspect > 0.0f)
    {
        verticalAperture = (float) m_sample.getHorizontalAperture() / params.targetAspect;
    }

    params.nearClippingPlane = (float) m_sample.getNearClippingPlane();
    params.farClippingPlane = (float) m_sample.getFarClippingPlane();
    // CameraSample::getFieldOfView() returns the horizontal field of view, we need the verical one
    params.fieldOfView = 2.0f * atanf(verticalAperture * 10.0f / (2.0f * focalLength)) * sRad2Deg;
    params.focusDistance = (float) m_sample.getFocusDistance() * 0.1f; // centimeter to meter
    params.focalLength = focalLength * 0.01f; // milimeter to meter
}
