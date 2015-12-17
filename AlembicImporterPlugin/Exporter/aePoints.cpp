#include "pch.h"
#include "AlembicExporter.h"
#include "aeObject.h"
#include "aePoints.h"

aePoints::aePoints(aeObject *obj)
    : super(obj)
    , m_abcobj(obj->getAbcObject(), "Points")
    , m_schema(m_abcobj.getSchema())
{
}

void aePoints::writeSample(const aePointsSampleData &data)
{

}
