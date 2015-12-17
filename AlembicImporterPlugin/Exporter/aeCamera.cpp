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

}
